//영화를 클릭하면 나오는 영화 정보 페이지(여기에 리뷰작성,수정,삭제 가능)

using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Storage;
using MovieReviewProject.Services;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MovieReviewProject.Views
{
    [QueryProperty(nameof(TmdbId), "tmdbId")]
    public partial class MovieDetailPage : ContentPage
    {
        private readonly MovieService _movieService = new MovieService();

        // TMDB 영화 ID
        private int _tmdbId;
        public int TmdbId
        {
            get => _tmdbId;
            set
            {
                _tmdbId = value;
                LoadMovieInfo();
                LoadRatingSummary();
                LoadReviews();
            }
        }

        // 별점 이미지
        private readonly ObservableCollection<Image> starImages = new();

        // 사용자가 선택한 평점
        private int selectedRating = 0;

        // 사용자가 선택한 감정
        private string selectedEmotion = "";

        // 전체 리뷰 목록
        public ObservableCollection<Review> AllReviews { get; set; } = new();

        // 화면에 표시할 리뷰-> 3개정도?
        public ObservableCollection<Review> DisplayedReviews { get; set; } = new();
        private bool showingAllReviews = false;

        // 나의 리뷰 목록
        public ObservableCollection<Review> MyReviewsForThisMovie { get; set; } = new();
        private Review myReview;

        public MovieDetailPage()
        {
            InitializeComponent();
            BindingContext = this;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // 별점 이미지 초기화
            if (starImages.Count == 0 && StarLayout.Children.Count > 0)
            {
                for (int i = 0; i < StarLayout.Children.Count; i++)
                {
                    if (StarLayout.Children[i] is Image starImage)
                    {
                        starImages.Add(starImage);
                        int ratingValue = i + 1;
                        var tap = new TapGestureRecognizer();
                        tap.Tapped += (s, e) => OnStarTapped(ratingValue);
                        starImage.GestureRecognizers.Add(tap);
                    }
                }
            }

            // 감정 이모지 초기화
            HappyEmoji.GestureRecognizers.Clear();
            MehEmoji.GestureRecognizers.Clear();
            SadEmoji.GestureRecognizers.Clear();

            HappyEmoji.GestureRecognizers.Add(CreateEmojiTapGesture("감동"));
            MehEmoji.GestureRecognizers.Add(CreateEmojiTapGesture("무난"));
            SadEmoji.GestureRecognizers.Add(CreateEmojiTapGesture("슬픔"));

            // 테두리(Stroke) 초기화
            HappyBorder.Stroke = Colors.Transparent;
            MehBorder.Stroke = Colors.Transparent;
            SadBorder.Stroke = Colors.Transparent;
        }

        // 영화 정보 불러오기 
        private async void LoadMovieInfo()
        {
            try
            {
                var movie = await _movieService.GetMovieByTmdbIdAsync(TmdbId);
                if (movie != null)
                {
                    TitleLabel.Text = movie.Title;
                    GenreLabel.Text = movie.Genre;
                    OverviewLabel.Text = string.IsNullOrEmpty(movie.Overview)
                        ? "줄거리가 없습니다."
                        : movie.Overview;
                    RepresentativeReviewLabel.Text = string.IsNullOrEmpty(movie.Review)
                        ? "대표 리뷰가 없습니다."
                        : movie.Review;

                    PosterImage.Source = !string.IsNullOrEmpty(movie.PosterPath)
                        ? movie.PosterPath
                        : "poster_placeholder.png";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadMovieInfo 예외: {ex.Message}");
            }
        }

        // ── 평점 요약 불러오기 ───────────────────────────────────
        private async void LoadRatingSummary()
        {
            try
            {
                var summary = await _movieService.GetRatingSummaryAsync(TmdbId);
                if (summary != null)
                {
                    RatingAverageLabel.Text = $"⭐ {summary.average_rating:F1} / 5.0";
                    TotalReviewsLabel.Text = $"({summary.total_reviews}개의 리뷰)";
                    RatingProgress.Progress = summary.average_rating / 5.0;
                }
                else
                {
                    RatingAverageLabel.Text = "⭐ 0.0 / 5.0";
                    TotalReviewsLabel.Text = "(0개의 리뷰)";
                    RatingProgress.Progress = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadRatingSummary 예외: {ex.Message}");
                await DisplayAlert("오류", "평점 정보를 불러오는 데 실패했습니다.", "확인");
            }
        }

        // ── 전체 리뷰 목록 불러오기 ───────────────────────────────
        private async void LoadReviews()
        {
            try
            {
                var reviews = await _movieService.GetReviewsByTmdbIdAsync(TmdbId);
                AllReviews.Clear();
                foreach (var r in reviews)
                    AllReviews.Add(r);

                UpdateDisplayedReviews();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadReviews 예외: {ex.Message}");
                await DisplayAlert("오류", "리뷰 목록을 불러오는 데 실패했습니다.", "확인");
            }
        }

        private void UpdateDisplayedReviews()
        {
            DisplayedReviews.Clear();
            var target = showingAllReviews ? AllReviews : AllReviews.Take(3);
            foreach (var r in target)
                DisplayedReviews.Add(r);

            ToggleReviewButton.Text = showingAllReviews ? "접기" : "더보기";
        }

        private void OnToggleReviewClicked(object sender, EventArgs e)
        {
            showingAllReviews = !showingAllReviews;
            UpdateDisplayedReviews();
        }

        // ── 별점 이미지 탭 처리 ───────────────────────────────────
        private void OnStarTapped(int rating)
        {
            selectedRating = rating;
            for (int i = 0; i < starImages.Count; i++)
            {
                starImages[i].Source = i < rating
                    ? "star_filled.png"
                    : "star_empty.png";
            }
        }

        // ── 감정 이모지 탭 제스처 생성 ───────────────────────────
        private TapGestureRecognizer CreateEmojiTapGesture(string emotionLabel)
        {
            return new TapGestureRecognizer
            {
                Command = new Command(() =>
                {
                    selectedEmotion = emotionLabel;

                    HappyBorder.Stroke = Colors.Transparent;
                    MehBorder.Stroke = Colors.Transparent;
                    SadBorder.Stroke = Colors.Transparent;

                    switch (emotionLabel)
                    {
                        case "감동":
                            HappyBorder.Stroke = Colors.Orange;
                            break;
                        case "무난":
                            MehBorder.Stroke = Colors.Orange;
                            break;
                        case "슬픔":
                            SadBorder.Stroke = Colors.Orange;
                            break;
                    }
                })
            };
        }

        // ── 리뷰 저장 ───────────────────────────────────────────
        private async void OnSaveReviewClicked(object sender, EventArgs e)
        {
            try
            {
                string reviewText = ReviewEditor.Text?.Trim();
                if (string.IsNullOrWhiteSpace(reviewText) && selectedRating == 0)
                {
                    await DisplayAlert("입력 오류", "별점 또는 리뷰 중 하나는 입력해야 합니다.", "확인");
                    return;
                }

                var movie = await _movieService.GetMovieByTmdbIdAsync(TmdbId);
                if (movie != null)
                {
                    await ApiService.SaveMovieIfNotExistsAsync(
                        new MovieModel { TmdbId = TmdbId, Title = movie.Title });
                }

                string memberId = Preferences.Get("MemberId", "");
                if (string.IsNullOrEmpty(memberId))
                {
                    await DisplayAlert("오류", "로그인 정보가 없습니다.", "확인");
                    return;
                }

                // 
                await ApiService.SubmitReviewAsync(
                    movie?.Title ?? "",           // title
                    TmdbId,                       // tmdbId
                    reviewText,                   // content
                    selectedRating,               // rating
                    new List<string> { selectedEmotion }, // emotions
                    "",                           // mediaUrl
                    "",                           // highlightQuote
                    ""
                );

                await DisplayAlert("리뷰 저장됨", $"별점: {selectedRating}점\n내용: {reviewText}", "확인");

                LoadRatingSummary();
                LoadReviews();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnSaveReviewClicked 예외: {ex.Message}");
                await DisplayAlert("오류", "리뷰 저장 중 문제가 발생했습니다.", "확인");
            }
        }

        // ── 좋아요 버튼 클릭 ─────────────────────────────────────
        private async void OnLikeButtonClicked(object sender, EventArgs e)
        {
            try
            {
                
                bool likedResult = await ApiService.ToggleLikeAsync(TmdbId);

                

                LikeButton.TextColor = likedResult ? Colors.Red : Colors.Gray;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnLikeButtonClicked 예외: {ex.Message}");
                await DisplayAlert("오류", "좋아요 토글 중 문제가 발생했습니다.", "확인");
            }
        }

        // ── “내 리뷰 조회” 클릭 ─────────────────────────────────
        private async void OnShowMyReviewsClicked(object sender, EventArgs e)
        {
            MyReviewSection.IsVisible = true;
            await LoadMyReviewsForThisMovie();
        }

        private async Task LoadMyReviewsForThisMovie()
        {
            try
            {
                MyReviewsForThisMovie.Clear();

                string memberId = Preferences.Get("MemberId", null);
                System.Diagnostics.Debug.WriteLine($"▶ LoadMyReviewsForThisMovie - MemberId: [{memberId}], TmdbId: [{TmdbId}]");

                if (string.IsNullOrEmpty(memberId))
                {
                    SelectedReviewLabel.Text = "로그인이 필요합니다.";
                    return;
                }

                //  TMDB ID로 해당 영화에 달린 모든 리뷰 불러오기
                var reviewsForMovie = await _movieService.GetReviewsByTmdbIdAsync(TmdbId);
                System.Diagnostics.Debug.WriteLine($"▶ LoadMyReviewsForThisMovie - 영화({TmdbId}) 전체 리뷰 개수: {reviewsForMovie.Count}");

                // 그중에서 memberId가 일치하는 것만 필터
                var mine = reviewsForMovie.Where(r => r.member_id == memberId);
                System.Diagnostics.Debug.WriteLine($"▶ LoadMyReviewsForThisMovie - 필터 후 내 리뷰 개수: {mine.Count()}");

                foreach (var r in mine)
                    MyReviewsForThisMovie.Add(r);

                if (MyReviewsForThisMovie.Count == 0)
                {
                    SelectedReviewLabel.Text = "내가 작성한 리뷰가 없습니다.";
                    EditReviewButton.IsEnabled = false;
                    DeleteReviewButton.IsEnabled = false;
                }
                else
                {
                    SelectedReviewLabel.Text = "리뷰를 선택하세요";
                    EditReviewButton.IsEnabled = false;
                    DeleteReviewButton.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadMyReviewsForThisMovie 예외: {ex}");
                await DisplayAlert("오류", "내 리뷰 목록을 불러오는 데 실패했습니다.", "확인");
            }
        }

        //내 리뷰 선택할 수 있는 함수
        private void OnMyReviewSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (e.CurrentSelection.FirstOrDefault() is Review chosen)
                {
                    myReview = chosen;
                    SelectedReviewLabel.Text =
                        $"[{chosen.created_at:MM/dd HH:mm}] {chosen.content}\n⭐ {chosen.rating}";
                    EditReviewButton.IsEnabled = true;
                    DeleteReviewButton.IsEnabled = true;
                }
                else
                {
                    myReview = null;
                    SelectedReviewLabel.Text = "리뷰를 선택하세요";
                    EditReviewButton.IsEnabled = false;
                    DeleteReviewButton.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnMyReviewSelectionChanged 예외: {ex.Message}");
                myReview = null;
                SelectedReviewLabel.Text = "리뷰를 선택하세요";
                EditReviewButton.IsEnabled = false;
                DeleteReviewButton.IsEnabled = false;
            }
        }

        //내 리뷰 수정
        private async void OnEditMyReviewClicked(object sender, EventArgs e)
        {
            if (myReview == null)
            {
                await DisplayAlert("오류", "먼저 리뷰를 선택하세요.", "확인");
                return;
            }

            string newContent = await DisplayPromptAsync(
                "리뷰 수정",
                "수정할 리뷰 내용을 입력하세요:",
                initialValue: myReview.content
            );
            if (string.IsNullOrWhiteSpace(newContent))
                return;

            myReview.content = newContent;

            string jwtToken = Preferences.Get("AuthToken", null);
            string memberId = Preferences.Get("MemberId", null);
            if (string.IsNullOrEmpty(jwtToken) || string.IsNullOrEmpty(memberId))
            {
                await DisplayAlert("오류", "로그인 정보가 없습니다.", "확인");
                return;
            }

            try
            {
                bool success = await _movieService.UpdateReviewAsync(
                    myReview,
                    jwtToken,
                    memberId
                );

                if (success)
                {
                    await DisplayAlert("성공", "리뷰가 수정되었습니다.", "확인");
                    await LoadMyReviewsForThisMovie();
                    LoadReviews();
                }
                else
                {
                    await DisplayAlert("실패", "리뷰 수정에 실패했습니다.", "확인");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnEditMyReviewClicked 예외: {ex.Message}");
                await DisplayAlert("오류", "리뷰 수정 중 문제가 발생했습니다.", "확인");
            }
        }

        // 내 리뷰 삭제 
        private async void OnDeleteMyReviewClicked(object sender, EventArgs e)
        {
            if (myReview == null)
            {
                await DisplayAlert("오류", "먼저 리뷰를 선택하세요.", "확인");
                return;
            }

            bool confirm = await DisplayAlert("리뷰 삭제", "정말 삭제하시겠습니까?", "예", "아니오");
            if (!confirm) return;

            string memberId = Preferences.Get("MemberId", null);
            if (string.IsNullOrEmpty(memberId))
            {
                await DisplayAlert("오류", "로그인 정보가 없습니다.", "확인");
                return;
            }

            try
            {
                bool success = await _movieService.DeleteReviewAsync(
                    myReview.id,
                    memberId
                );

                if (success)
                {
                    await DisplayAlert("성공", "리뷰가 삭제되었습니다.", "확인");
                    myReview = null;
                    await LoadMyReviewsForThisMovie();
                    LoadReviews();
                }
                else
                {
                    await DisplayAlert("실패", "리뷰 삭제에 실패했습니다.", "확인");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnDeleteMyReviewClicked 예외: {ex.Message}");
                await DisplayAlert("오류", "리뷰 삭제 중 문제가 발생했습니다.", "확인");
            }
        }
    }
}
