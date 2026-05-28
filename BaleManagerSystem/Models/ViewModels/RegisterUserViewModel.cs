using System.ComponentModel.DataAnnotations;

namespace BaleManagerSystem.Models.ViewModels
{
    public class RegisterUserViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "شماره همراه")]

        [RegularExpression(
            @"^98[1-9][0-9]{9}$",
            ErrorMessage =
            "شماره همراه باید با 98 و بدون 0 شروع شود.")]
        public string PhoneNumber { get; set; } = "";

        public DateTime FirstSeen { get; set; }
    }
}
