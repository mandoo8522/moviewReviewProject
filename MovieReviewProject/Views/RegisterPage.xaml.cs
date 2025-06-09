//회원가입 페이지

using System;
using Microsoft.Maui.Controls;
using MovieReviewProject.Services;

namespace MovieReviewProject.Views
{
    public partial class RegisterPage : ContentPage
    {
        public RegisterPage()
        {
            InitializeComponent();
        }

        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            string id = IdEntry.Text?.Trim();
            string pw = PasswordEntry.Text?.Trim();
            string name = NameEntry.Text?.Trim();
            string birth = BirthEntry.Text?.Trim();
            string gender = GenderPicker.SelectedItem?.ToString();
            string email = EmailEntry.Text?.Trim();
            string phone = PhoneEntry.Text?.Trim();

            // 필수 입력 확인
            if (string.IsNullOrWhiteSpace(id) ||
                string.IsNullOrWhiteSpace(pw) ||
                string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(birth) ||
                string.IsNullOrWhiteSpace(gender) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(phone))
            {
                await DisplayAlert("입력 오류", "모든 항목을 빠짐없이 입력해주세요.", "확인");
                return;
            }


            // 회원가입 요청
            var result = await ApiService.RegisterAsync(id, pw, name, birth, gender, email, phone);

            if (result.Contains("회원가입") || result.Contains("성공") || result.Contains("완료"))
            {
                await DisplayAlert("회원가입 완료", "환영합니다! 로그인 해주세요.", "확인");

                // 로그인 페이지로 이동
                await Shell.Current.GoToAsync("//LoginPage");
            }
            else
            {
                await DisplayAlert("회원가입 실패", result, "확인");
            }
        }
        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//LoginPage");
        }

    }
}
