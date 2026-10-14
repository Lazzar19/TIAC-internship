using FluentValidation;
using WebAPI.Application;
using WebAPI.Domain;

namespace WebAPI.Application.Validators;

public sealed class CreateUserDTOValidator : AbstractValidator<CreateUserDTO>
{
    public CreateUserDTOValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage("Username is required.")
            .MaximumLength(20).WithMessage("Username cannot exceed 20 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email format is invalid");

        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required")
            .MinimumLength(PasswordRules.MinimumLength).WithMessage("Password must have at least 12 characters")
            .MaximumLength(PasswordRules.MaximumLength).WithMessage("Password cannot exceed 128 characters.");
    }
}
