using FluentValidation;
using UserService.Application.DTOs;

namespace UserService.Application.Validators;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Ad soyad zorunludur.")
            .MaximumLength(150);

        RuleFor(x => x.Email).ValidEmail();
    }
}
