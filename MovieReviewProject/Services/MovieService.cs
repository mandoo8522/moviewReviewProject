using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Linq;

namespace MovieReviewProject.Services
{
    public class MovieService
    {
        private readonly string apiKey = "1061b7ca6df4d6dfd81812948f2a718b";
        private readonly string baseUrl = "https://api.themoviedb.org/3";
        private readonly HttpClient _httpClient;

        public MovieService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        //인기 영화 불러오기 
        public async Task<ObservableCollection<MovieModel>> GetPopularMoviesAsync()
        {
            string url = $"{baseUrl}/movie/popular?api_key={apiKey}&language=ko-KR&page=1";
            return await GetMoviesFromUrlAsync(url);
        }

        //검색으로 영화 불러오기 
        public async Task<ObservableCollection<MovieModel>> GetMoviesBySearch(string query)
        {
            string searchUrl = $"{baseUrl}/search/movie?api_key={apiKey}&language=ko-KR&query={Uri.EscapeDataString(query)}&page=1";
            return await GetMoviesFromUrlAsync(searchUrl);
        }

        // TMDB ID로 단일 영화 정보 조회
        public async Task<MovieModel?> GetMovieByTmdbIdAsync(int tmdbId)
        {
            // 이미 불러온 인기 영화 목록에서 먼저 검색
            var popular = await GetPopularMoviesAsync();
            var found = popular.FirstOrDefault(m => m.TmdbId == tmdbId);
            if (found != null)
                return found;

            // 검색 결과에 없는 경우 TMDB 상세 조회 엔드포인트 사용
            string url = $"{baseUrl}/movie/{tmdbId}?api_key={apiKey}&language=ko-KR";
            HttpResponseMessage response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return null;

            string json = await response.Content.ReadAsStringAsync();
            JObject data = JObject.Parse(json);

            string posterPath = data["poster_path"]?.ToString();
            string fullPosterUrl = !string.IsNullOrEmpty(posterPath)
                ? $"https://image.tmdb.org/t/p/w500{posterPath}"
                : "https://via.placeholder.com/500x750?text=No+Image";

            JArray genreItems = (JArray)data["genres"];
            string genreText = genreItems.Count > 0
                ? string.Join(", ", genreItems.Select(g => g["name"]?.ToString()))
                : "장르 없음";

            // 대표 리뷰도 TMDB 리뷰 엔드포인트에서 가져오기
            string reviewText = await GetMovieReview(tmdbId);

            return new MovieModel
            {
                TmdbId = tmdbId,
                Title = data["title"]?.ToString() ?? "제목 없음",
                Overview = data["overview"]?.ToString() ?? "",
                PosterPath = fullPosterUrl,
                Genre = genreText,
                VoteAverage = data["vote_average"]?.ToObject<double>() ?? 0,
                VoteCount = data["vote_count"]?.ToObject<int>() ?? 0,
                Review = reviewText,
                RatingAverage = 0, // TMDB에는 별도 평균 평점 제공하지 않으므로 0으로 초기화
                ReleaseYear = data["release_date"] != null
                              ? int.Parse(data["release_date"].ToString().Substring(0, 4))
                              : 0,
                Director = null // 감독은 아직 안정해서 빈칸
            };
        }

        // 리뷰 목록 조회 함수 
        public async Task<ObservableCollection<Review>> GetReviewsByTmdbIdAsync(int tmdbId)
        {
            string url = $"https://movie-api-lh8x.onrender.com/api/reviews/tmdb/{tmdbId}";
            HttpResponseMessage response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return new ObservableCollection<Review>();

            string json = await response.Content.ReadAsStringAsync();
            Console.WriteLine("🎯 리뷰 API 응답: " + json);
            return JsonConvert.DeserializeObject<ObservableCollection<Review>>(json);
        }

        // 별점 통계 조회 함수
        public async Task<RatingSummary> GetRatingSummaryAsync(int tmdbId)
        {
            string url = $"https://movie-api-lh8x.onrender.com/api/reviews/tmdb/{tmdbId}/rating";
            HttpResponseMessage response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return null;

            string json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<RatingSummary>(json);
        }

        // 리뷰 수정 함수
        public async Task<bool> UpdateReviewAsync(Review review, string jwtToken, string memberId)
        {
            if (review == null)
                throw new ArgumentNullException(nameof(review));
            if (string.IsNullOrWhiteSpace(jwtToken))
                throw new ArgumentException("jwtToken이 유효하지 않습니다.", nameof(jwtToken));
            if (string.IsNullOrWhiteSpace(memberId))
                throw new ArgumentException("memberId가 유효하지 않습니다.", nameof(memberId));

            // 백엔드 URL로 수정요청
            string url = $"https://movie-api-lh8x.onrender.com/api/reviews/{review.id}";

            // PATCH 메서드 쓰기 위해 HttpRequestMessage 생성
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), url);

            // Authorization 헤더에 Bearer 토큰을 붙입니다.
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);

            // 요청 바디(payload) 구성: member_id, content, rating, emotions, media_url, highlight_quote, highlight_image_url
            var payload = new
            {
                member_id = memberId,
                content = review.content,
                rating = double.TryParse(review.rating, out var parsed) ? parsed : 0,
                emotions = review.emotions,
                media_url = review.media_url,
                highlight_quote = review.highlight_quote,
                highlight_image_url = review.highlight_image_url
            };

            string jsonBody = JsonConvert.SerializeObject(payload);
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            // 실제 요청 보내기
            var response = await _httpClient.SendAsync(request);

            

            return response.IsSuccessStatusCode;
        }

        // 공통 호출 함수 
        private async Task<ObservableCollection<MovieModel>> GetMoviesFromUrlAsync(string url)
        {
            var movies = new ObservableCollection<MovieModel>();
            HttpResponseMessage response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"⚠ API 요청 실패: {response.StatusCode}");
                return movies;
            }

            string jsonData = await response.Content.ReadAsStringAsync();
            Debug.WriteLine("API 응답: " + jsonData);
            JObject data = JObject.Parse(jsonData);

            foreach (var movie in data["results"])
            {
                int tmdbId = movie["id"]?.ToObject<int>() ?? 0;
                string posterPath = movie["poster_path"]?.ToString();
                string fullPosterUrl = !string.IsNullOrEmpty(posterPath)
                    ? $"https://image.tmdb.org/t/p/w500{posterPath}"
                    : "https://via.placeholder.com/500x750?text=No+Image";

                JArray genreIds = (JArray)movie["genre_ids"];
                string genreText = GetGenreNames(genreIds);
                int movieId = movie["id"]?.ToObject<int>() ?? 0;
                string reviewText = await GetMovieReview(movieId);

                movies.Add(new MovieModel
                {
                    TmdbId = tmdbId,
                    Title = movie["title"]?.ToString(),
                    Overview = movie["overview"]?.ToString(),
                    PosterPath = fullPosterUrl,
                    Genre = genreText,
                    VoteAverage = movie["vote_average"]?.ToObject<double>() ?? 0,
                    VoteCount = movie["vote_count"]?.ToObject<int>() ?? 0,
                    Review = reviewText,
                    RatingAverage = 0,
                    ReleaseYear = movie["release_date"] != null
                                  ? int.Parse(movie["release_date"].ToString().Substring(0, 4))
                                  : 0,
                    Director = null
                });
            }

            return movies;
        }

        // 장르 텍스트 변환 -->나중에 장르별로 영화분류 기능도 도입예정
        private string GetGenreNames(JArray genreIds)
        {
            Dictionary<int, string> genreMap = new()
            {
                { 28, "액션" }, { 12, "모험" }, { 16, "애니메이션" }, { 35, "코미디" },
                { 80, "범죄" }, { 99, "다큐멘터리" }, { 18, "드라마" }, { 10751, "가족" },
                { 14, "판타지" }, { 36, "역사" }, { 27, "공포" }, { 10402, "음악" },
                { 9648, "미스터리" }, { 10749, "로맨스" }, { 878, "SF" }, { 10770, "TV 영화" },
                { 53, "스릴러" }, { 10752, "전쟁" }, { 37, "서부" }
            };

            List<string> genreNames = new();
            foreach (var id in genreIds)
            {
                if (genreMap.TryGetValue(id.ToObject<int>(), out string name))
                    genreNames.Add(name);
            }

            return genreNames.Count > 0 ? string.Join(", ", genreNames) : "장르 없음";
        }

        // 대표 리뷰 가져오기 (최신 리뷰순으로 상단에 뜸)
        private async Task<string> GetMovieReview(int movieId)
        {
            string reviewUrl = $"{baseUrl}/movie/{movieId}/reviews?api_key={apiKey}&language=ko-KR&page=1";
            HttpResponseMessage response = await _httpClient.GetAsync(reviewUrl);
            if (!response.IsSuccessStatusCode)
                return "리뷰 없음";

            string json = await response.Content.ReadAsStringAsync();
            JObject data = JObject.Parse(json);
            var results = (JArray)data["results"];

            return results.Count > 0
                ? results[0]["content"]?.ToString() ?? "리뷰 없음"
                : "리뷰 없음";
        }

        // 내 리뷰 전체 조회 
        public async Task<List<Review>> GetReviewsByMemberIdAsync(string memberId)
        {
            var url = $"https://movie-api-lh8x.onrender.com/api/reviews/member/{memberId}";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return new List<Review>();

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<Review>>(json);
        }

        //리뷰 삭제
        public async Task<bool> DeleteReviewAsync(int reviewId, string memberId)
        {
            var url = $"https://movie-api-lh8x.onrender.com/api/reviews/{reviewId}";
            var data = new { member_id = memberId };
            var json = JsonConvert.SerializeObject(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Delete, url)
            {
                Content = content
            };
            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
    }

    // 모델 클래스들

    public class MovieModel
    {
        public int TmdbId { get; set; }
        public string Title { get; set; }
        public string Overview { get; set; }
        public string PosterPath { get; set; }
        public string Genre { get; set; }
        public double VoteAverage { get; set; }
        public int VoteCount { get; set; }
        public string Review { get; set; }
        public double RatingAverage { get; set; }
        public int ReleaseYear { get; set; }
        public string Director { get; set; }
    }

    //백엔드 API랑 갯수 잘 맞추기
    public class Review
    {
        public int id { get; set; }
        public string member_id { get; set; }
        [JsonProperty("movie_id")]
        public int movie_id { get; set; }
        public string content { get; set; }
        public string rating { get; set; }
        public List<string> emotions { get; set; }
        public string media_url { get; set; }
        public string highlight_quote { get; set; }
        public string highlight_image_url { get; set; }
        public DateTime created_at { get; set; }

        [JsonIgnore]
        public IEnumerable<string> StarImages
        {
            get
            {
                if (!double.TryParse(rating, out var val))
                    val = 0;
                int fullStars = (int)Math.Round(val);
                for (int i = 1; i <= 5; i++)
                    yield return i <= fullStars ? "star_filled.png" : "star_empty.png";
            }
        }
    }

    //얘는 별점평균,총 리뷰 수
    public class RatingSummary
    {
        public int tmdb_id { get; set; }
        public double average_rating { get; set; }
        public int total_reviews { get; set; }
        public Dictionary<string, int> rating_distribution { get; set; }
    }
}
