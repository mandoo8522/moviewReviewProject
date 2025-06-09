// AppShell.xaml.cs

using Microsoft.Maui.Controls;
using MovieReviewProject.Views;

namespace MovieReviewProject
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            
            Routing.RegisterRoute(nameof(MovieDetailPage), typeof(MovieDetailPage));
            
            Routing.RegisterRoute(nameof(MyReviewsPage), typeof(MyReviewsPage));
        }
    }
}
