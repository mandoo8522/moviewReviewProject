using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace MovieReviewProject.Controls
{
    public class AnimatedMovieCard : ContentView
    {
        public AnimatedMovieCard()
        {
            // 포스터 이미지
            var posterImage = new Image
            {
                HeightRequest = 180,
                Aspect = Aspect.AspectFill
            };
            posterImage.SetBinding(Image.SourceProperty, "PosterPath");

            // 영화 제목
            var titleLabel = new Label
            {
                FontSize = 14,
                TextColor = Colors.White,
                LineBreakMode = LineBreakMode.TailTruncation
            };
            titleLabel.SetBinding(Label.TextProperty, "Title");

            // 카드 스타일
            var card = new Frame
            {
                Padding = 5,
                BackgroundColor = Color.FromArgb("#1c1c1c"),
                CornerRadius = 10,
                HasShadow = true,
                Content = new VerticalStackLayout
                {
                    Spacing = 8,
                    Children = { posterImage, titleLabel }
                }
            };

            Content = card;
        }

        protected override async void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();

            this.Opacity = 0;
            this.Scale = 0.9;

            await Task.Delay(80); // Stagger animation
            await Task.WhenAll(
                this.FadeTo(1, 300),
                this.ScaleTo(1, 300, Easing.CubicOut)
            );
        }
    }
}
