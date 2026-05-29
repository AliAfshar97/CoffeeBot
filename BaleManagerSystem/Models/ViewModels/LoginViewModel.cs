using System.ComponentModel.DataAnnotations;

namespace BaleManagerSystem.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(
            ErrorMessage =
            "نام کاربری را وارد کنید")]
        public string UserName { get; set; } = "";


        [Required(
            ErrorMessage =
            "کلمه عبور را وارد کنید.")]
        public string Password { get; set; } = "";
    }
}
