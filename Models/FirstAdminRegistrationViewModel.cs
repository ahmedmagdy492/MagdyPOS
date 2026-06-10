using System.ComponentModel.DataAnnotations;

namespace MagdyPOS.Models;

public sealed class FirstAdminRegistrationViewModel
{
    [Required(ErrorMessage = "اسم المدير مطلوب.")]
    [Display(Name = "اسم المدير")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "البريد الإلكتروني مطلوب.")]
    [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة.")]
    [Display(Name = "البريد الإلكتروني")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "كلمة المرور مطلوبة.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "كلمة المرور يجب أن تكون 6 أحرف على الأقل.")]
    [DataType(DataType.Password)]
    [Display(Name = "كلمة المرور")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "تأكيد كلمة المرور مطلوب.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "كلمة المرور وتأكيدها غير متطابقين.")]
    [Display(Name = "تأكيد كلمة المرور")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "اسم المؤسسة مطلوب.")]
    [Display(Name = "اسم المؤسسة")]
    public string OrganizationName { get; set; } = string.Empty;

    [Required(ErrorMessage = "رقم الهاتف مطلوب.")]
    [Display(Name = "رقم الهاتف")]
    public string OrganizationPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "العنوان مطلوب.")]
    [Display(Name = "العنوان")]
    public string OrganizationAddress { get; set; } = string.Empty;
}
