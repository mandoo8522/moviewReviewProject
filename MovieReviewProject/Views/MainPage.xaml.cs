// 메인페이지(로그인 완료되면 넘어가는 화면)
using Microsoft.Maui.Controls;
using MovieReviewProject.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MovieReviewProject.Views
{
    public partial class MainPage : ContentPage
    {
        private readonly MovieService _movieService = new();

        public ObservableCollection<MovieModel> Movies { get; set; } = new();
        public string SearchQuery { get; set; }

        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;
            LoadMovies();
        }

        //영화 정보 불러오는
        private async void LoadMovies()
        {
            var result = await _movieService.GetPopularMoviesAsync();
            Movies.Clear();
            foreach (var movie in result)
                Movies.Add(movie);
        }

        //영화제목을 검색할 수 있음
        private async void OnSearchCompleted(object sender, System.EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                var result = await _movieService.GetMoviesBySearch(SearchQuery);
                Movies.Clear();
                foreach (var movie in result)
                    Movies.Add(movie);
            }
        }

        // 메인화면에 있는 영화를 클릭하면 상세페이지로 넘어가지는 함수임.
        private async void OnMovieSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is MovieModel selectedMovie)
            {
                ((CollectionView)sender).SelectedItem = null;
                await Task.Delay(80);

                // Shell 네비게이션: 쿼리 매개변수로 tmdbId 전달
                await Shell.Current.GoToAsync($"MovieDetailPage?tmdbId={selectedMovie.TmdbId}");
            }
        }

    }
}
