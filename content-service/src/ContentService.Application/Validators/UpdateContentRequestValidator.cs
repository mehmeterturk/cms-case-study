using ContentService.Application.DTOs;
using FluentValidation;

namespace ContentService.Application.Validators;

public class UpdateContentRequestValidator : AbstractValidator<UpdateContentRequest>
{
    public UpdateContentRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Başlık zorunludur.")
            .MaximumLength(200);

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("İçerik metni zorunludur.");
    }
}
