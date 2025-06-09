// TMDB API와 백엔드 API를 연동시키는 곳

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MovieReviewProject.Services;
using Microsoft.Maui.Storage;

namespace MovieReviewProject.Services
{
    public static class ApiService
    {
        private static readonly HttpClient _httpClient = new();

        private static void SetJwtIfExists()
        {
            string token = Preferences.Get("jwt_token", null);
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }
        }

        //로그인
        public static async Task<string?> LoginAsync(string id, string password)
        {
            var payload = new { id, password };
            string json = JsonSerializer.Serialize(payload);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                "https://movie-api-lh8x.onrender.com/api/login",
                httpContent);

            if (!response.IsSuccessStatusCode)
                return null;

            string responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);

            if (doc.RootElement.TryGetProperty("token", out var tokenProp))
            {
                string token = tokenProp.GetString();
                Preferences.Set("jwt_token", token);
                Preferences.Set("member_id", id); // 로그인한 아이디 저장
                return token;
            }
            return null;
        }

        //회원가입
        public static async Task<string> RegisterAsync(
            string id,
            string password,
            string name,
            string birth,
            string gender,
            string email,
            string phone)
        {
            var payload = new
            {
                id = id,
                password = password,
                name = name,
                birth = birth,
                gender = gender,
                email = email,
                phone_number = phone
            };

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            string json = JsonSerializer.Serialize(payload, options);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                "https://movie-api-lh8x.onrender.com/api/register",
                httpContent);

            return await response.Content.ReadAsStringAsync();
        }

        //영화정보(영화의 고유ID.. 등이  DB에 저장되어야 리뷰용 DB에 리뷰저장이 가능하기 때문에 영화정보를 DB에 저장하는 함수
        public static async Task SaveMovieIfNotExistsAsync(MovieModel movie)
        {
            var payload = new
            {
                tmdb_id = movie.TmdbId,
                title = movie.Title,
                poster_path = movie.PosterPath,
                overview = movie.Overview,
                genre = movie.Genre,
                vote_average = movie.VoteAverage,
                vote_count = movie.VoteCount,
                release_year = movie.ReleaseYear,
                director = movie.Director
            };

            string json = JsonSerializer.Serialize(payload);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            await _httpClient.PostAsync("https://movie-api-lh8x.onrender.com/api/movies", httpContent);
        }
        
        //리뷰 저장함수
        public static async Task SubmitReviewAsync(
            string title,
            int tmdbId,
            string content,
            int rating,
            List<string> emotions,
            string mediaUrl,
            string highlightQuote,
            string highlightImageUrl)
        {
            SetJwtIfExists();

           
            string currentMemberId = Preferences.Get("MemberId" ,null);
            if (string.IsNullOrEmpty(currentMemberId))
                throw new InvalidOperationException("로그인된 사용자가 없습니다.");

            var payload = new
            {
                member_id = currentMemberId,
                title = title,
                tmdb_id = tmdbId,
                content = content,
                rating = rating,
                emotions = emotions,
                media_url = mediaUrl,
                highlight_quote = highlightQuote,
                highlight_image_url = highlightImageUrl
            };

            string json = JsonSerializer.Serialize(payload);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            await _httpClient.PostAsync(
                "https://movie-api-lh8x.onrender.com/api/reviews",
                httpContent);
        }

        //리뷰 저장하면 업데이트되는 함수
        public static async Task<bool> UpdateReviewAsync(
            string reviewId,
            string newContent,
            int newRating,
            List<string> newEmotions)
        {
            SetJwtIfExists();

            string currentMemberId = Preferences.Get("member_id", null);
            if (string.IsNullOrEmpty(currentMemberId))
                throw new InvalidOperationException("로그인된 사용자가 없습니다.");

            var payload = new
            {
                member_id = currentMemberId,
                content = newContent,
                rating = newRating,
                emotions = newEmotions
            };

            string json = JsonSerializer.Serialize(payload);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            // 서버가 PUT 또는 PATCH를 기대한다면 메서드를 바꿔주세요.
            var response = await _httpClient.PutAsync(
                $"https://movie-api-lh8x.onrender.com/api/reviews/{reviewId}",
                httpContent);

            return response.IsSuccessStatusCode;
        }

        public static async Task<bool> DeleteReviewAsync(int reviewId)
        {
            SetJwtIfExists();

            string currentMemberId = Preferences.Get("member_id", null);
            if (string.IsNullOrEmpty(currentMemberId))
                throw new InvalidOperationException("로그인된 사용자가 없습니다.");

            // URL에 reviewId(int)를 그대로 넣어도 문자열로 자동 변환됩니다.
            var response = await _httpClient.DeleteAsync(
                $"https://movie-api-lh8x.onrender.com/api/reviews/{reviewId}?member_id={currentMemberId}");

            return response.IsSuccessStatusCode;
        }

        public static async Task<bool> ToggleLikeAsync(int tmdbId)
        {
            SetJwtIfExists();

            string currentMemberId = Preferences.Get("member_id", null);
            if (string.IsNullOrEmpty(currentMemberId))
                throw new InvalidOperationException("로그인된 사용자가 없습니다.");

            var response = await _httpClient.PostAsync(
                $"https://movie-api-lh8x.onrender.com/api/likes/toggle?member_id={currentMemberId}&tmdb_id={tmdbId}",
                null);

            string json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("isLiked", out var prop) && prop.GetBoolean();
        }

        public static async Task<List<Review>> GetReviewsByMemberIdAsync()
        {
            SetJwtIfExists();

            string currentMemberId = Preferences.Get("member_id", null);
            if (string.IsNullOrEmpty(currentMemberId))
                throw new InvalidOperationException("로그인된 사용자가 없습니다.");

            var response = await _httpClient.GetAsync(
                $"https://movie-api-lh8x.onrender.com/api/reviews/member/{currentMemberId}");

            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Review>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<Review>();
        }
    }
}
