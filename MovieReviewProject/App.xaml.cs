// App.xaml.cs

using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace MovieReviewProject
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            
            MainPage = new AppShell();
        }
    }
}
