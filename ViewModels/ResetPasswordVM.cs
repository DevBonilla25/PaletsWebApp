using System.ComponentModel.DataAnnotations;

namespace PaletsWebApp.ViewModels
{
    public class ResetPasswordVM
    {
        public string? Id { get; set; }
        [Required]
        public string? UserName { get; set; }
        [Required]

        public string? UserFullName { get; set; }
        [Required]

        public string? NewPassword { get; set; }
        [Compare(nameof(NewPassword))]
        [Required]
        public string? ConfirmPassword { get; set; }
    }


    public class ResetPasswordForgetViewModel
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(50, MinimumLength = 6)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [StringLength(50, MinimumLength = 6)]
        public string ConfirmPassword { get; set; } = string.Empty;
    }


    public class ForgetPasswordVM
    {
        [Required(ErrorMessage = "El email es requerido")]
        public string? Email { get; set; }
        public bool FromMobile { get; set; }

    }
}


