//영화 리스트 불러오는 페이지임

using Microsoft.Maui.Controls;
using MovieReviewProject.Services;
using System.Collections.ObjectModel;

namespace MovieReviewProject.Views
{
    public partial class MovieListPage : ContentPage
    {
        private readonly MovieService _movieService = new();  

        
        public ObservableCollection<MovieModel> Movies { get; set; } = new();

        public MovieListPage()
        {
            InitializeComponent();
            BindingContext = this;
            LoadMovies();
        }

        private async void LoadMovies()
        {
            
            var result = await _movieService.GetPopularMoviesAsync();

            
            Movies.Clear();
            foreach (var movie in result)
            {
                Movies.Add(movie);
            }
        }
    }
}
