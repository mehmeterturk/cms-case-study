using FluentValidation;
using UserService.Application.DTOs;

namespace UserService.Application.Validators;

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Ad soyad zorunludur.")
            .MaximumLength(150);

        RuleFor(x => x.Email).ValidEmail();
    }
}
