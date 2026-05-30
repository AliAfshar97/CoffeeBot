namespace BaleManagerSystem.Models.ViewModels
{
    public class RegisterPageViewModel
    {
        public RegisterUserViewModel NewUser { get; set; }
            = new();

        public List<UserModel> Users { get; set; }
            = new();
    }
}
