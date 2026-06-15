using FluentValidation;

namespace UserService.Application.Validators;

/// <summary>
/// E-posta için yeniden kullanılabilir, katı (ASCII-only) doğrulama kuralı.
/// FluentValidation'ın varsayılan EmailAddress() kuralı yalnızca '@' varlığını
/// kontrol ettiğinden Türkçe/Unicode karakterlere izin verir; bu kural bunu engeller.
/// </summary>
public static class EmailValidationExtensions
{
    /// <summary>Local ve domain kısımlarını ASCII alfanümerik + sınırlı sembollere kısıtlar; TLD en az 2 harf.</summary>
    public const string EmailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9-]+(\.[a-zA-Z0-9-]+)*\.[a-zA-Z]{2,}$";

    public static IRuleBuilderOptions<T, string> ValidEmail<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("E-posta zorunludur.")
            .MaximumLength(256)
            .Matches(EmailPattern)
            .WithMessage("Geçerli bir e-posta adresi giriniz (Türkçe veya özel karakter içeremez).");
}
