//"내 리뷰 조회" 버튼 누르면 나오는 내 리뷰 페이지

using Microsoft.Maui.Controls;
using MovieReviewProject.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System;

namespace MovieReviewProject.Views
{
    public partial class MyReviewsPage : ContentPage
    {
        public ObservableCollection<Review> MyReviews { get; set; } = new();

        public MyReviewsPage()
        {
            InitializeComponent();
            BindingContext = this;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            var reviews = await ApiService.GetReviewsByMemberIdAsync(); // 네 ID로
            MyReviews.Clear();
            foreach (var review in reviews)
                MyReviews.Add(review);
        }
    }
}
