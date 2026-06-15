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

        RuleFor(x => x.Language)
            .NotEmpty().WithMessage("Dil (language) zorunludur.")
            .Matches("^[a-z]{2}$").WithMessage("Dil ISO 639-1 formatında iki küçük harf olmalıdır (ör. tr, en).");
    }
}
