using ContentService.Application.Common;
using ContentService.Application.DTOs;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace ContentService.Application.Validators;

public class CreateContentRequestValidator : AbstractValidator<CreateContentRequest>
{
    public CreateContentRequestValidator(IOptions<LocalizationOptions> localization)
    {
        var supported = localization.Value.SupportedLanguages;

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Başlık zorunludur.")
            .MaximumLength(200);

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("İçerik metni zorunludur.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("Kullanıcı kimliği (UserId) zorunludur.");

        RuleFor(x => x.Language)
            .NotEmpty().WithMessage("Dil (language) zorunludur.")
            .Must(lang => !string.IsNullOrWhiteSpace(lang) && supported.Contains(lang.Trim().ToLowerInvariant()))
            .WithMessage(_ => $"Desteklenmeyen dil. Desteklenen diller: {string.Join(", ", supported)}.");
    }
}
