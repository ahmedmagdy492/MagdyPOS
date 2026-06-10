using System.ComponentModel.DataAnnotations;

namespace MagdyPOS.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
    [EmailAddress(ErrorMessage = "صيغة البريد غير صحيحة")]
    [Display(Name = "البريد الإلكتروني")]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "كلمة المرور مطلوبة")]
    [DataType(DataType.Password)]
    [Display(Name = "كلمة المرور")]
    public string Password { get; set; } = "";
}
