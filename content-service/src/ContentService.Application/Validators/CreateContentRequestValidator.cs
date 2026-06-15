using ContentService.Application.DTOs;
using FluentValidation;

namespace ContentService.Application.Validators;

public class CreateContentRequestValidator : AbstractValidator<CreateContentRequest>
{
    public CreateContentRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Başlık zorunludur.")
            .MaximumLength(200);

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("İçerik metni zorunludur.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("Kullanıcı kimliği (UserId) zorunludur.");

        // Dil bir enum olduğundan geçerli değerler tip düzeyinde garanti edilir;
        // geçersiz bir değer model binding aşamasında reddedilir.
        RuleFor(x => x.Language)
            .IsInEnum().WithMessage("Geçersiz dil değeri.");
    }
}
