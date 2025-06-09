//로그인페이지
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using MovieReviewProject.Services;

namespace MovieReviewProject.Views
{
    public partial class LoginPage : ContentPage
    {
        public LoginPage()
        {
            InitializeComponent();
        }

        // 로그인 버튼 클릭하면 로그인 시도
        private async void OnLoginClicked(object sender, EventArgs e)
        {
            await AttemptLoginAsync();
        }

        //실질적인 로그인 기능 구현 함수
        private async Task AttemptLoginAsync()
        {
            string memberId = EmailEntry.Text?.Trim() ?? "";
            string password = PasswordEntry.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(memberId) || string.IsNullOrWhiteSpace(password))
            {
                await DisplayAlert("입력 오류", "ID와 Password를 모두 입력해주세요.", "확인");
                return;
            }

            string? token = null;
            try
            {
                token = await ApiService.LoginAsync(memberId, password);
            }
            catch (HttpRequestException httpEx)
            {
                System.Diagnostics.Debug.WriteLine($"LoginAsync HttpRequestException: {httpEx.Message}");
                await DisplayAlert("네트워크 오류", "서버와 통신 중 오류가 발생했습니다. 다시 시도해주세요.", "확인");
                return;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoginAsync Exception: {ex.Message}");
                await DisplayAlert("오류", "로그인 중 알 수 없는 오류가 발생했습니다.", "확인");
                return;
            }

            if (string.IsNullOrEmpty(token))
            {
                await DisplayAlert("로그인 실패", "ID 또는 Password가 잘못되었습니다.", "확인");
                return;
            }

            // 로그인 성공 시 반드시 MemberId를 저장-->그래야 리뷰 저장하고 내 리뷰 불러올 수 있음.
            Preferences.Set("AuthToken", token);
            Preferences.Set("MemberId", memberId);

            try
            {
                await Navigation.PushAsync(new MainPage());
            }
            catch (Exception navEx)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation.PushAsync Exception: {navEx.Message}");
                await DisplayAlert("오류", "페이지 이동 중 오류가 발생했습니다.", "확인");
            }
        }

        //회원가입 화면으로 넘어가는 함수.
        private async void OnRegisterTapped(object sender, EventArgs e)
        {
            try
            {
                await Navigation.PushAsync(new RegisterPage());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnRegisterTapped Exception: {ex.Message}");
                await DisplayAlert("오류", "회원가입 화면으로 이동 중 오류가 발생했습니다.", "확인");
            }
        }
    }
}
