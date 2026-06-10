using System.ComponentModel.DataAnnotations;
using MagdyPOS.Authorization;

namespace MagdyPOS.Models;

public class UserListRowVm
{
    public string Id { get; set; } = "";
    public string UserName { get; set; } = "";
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public string Roles { get; set; } = "";
}

public class CreateUserInput
{
    [Required(ErrorMessage = "البريد / اسم الدخول مطلوب")]
    [Display(Name = "البريد الإلكتروني")]
    [EmailAddress(ErrorMessage = "صيغة البريد غير صحيحة")]
    public string Email { get; set; } = "";

    [Display(Name = "الاسم الظاهر")]
    public string? FullName { get; set; }

    [Required(ErrorMessage = "كلمة المرور مطلوبة")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "كلمة المرور لا تقل عن 6 أحرف")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "نوع المستخدم مطلوب")]
    [Display(Name = "الصلاحية")]
    public string Role { get; set; } = AppRoles.Sales;
}

public class EditUserInput
{
    [Required]
    public string Id { get; set; } = "";

    [Required(ErrorMessage = "البريد مطلوب")]
    [EmailAddress]
    public string Email { get; set; } = "";

    [Display(Name = "الاسم الظاهر")]
    public string? FullName { get; set; }

    [Required(ErrorMessage = "نوع المستخدم مطلوب")]
    public string Role { get; set; } = AppRoles.Sales;

    [DataType(DataType.Password)]
    [Display(Name = "كلمة مرور جديدة (اختياري)")]
    public string? NewPassword { get; set; }
}
