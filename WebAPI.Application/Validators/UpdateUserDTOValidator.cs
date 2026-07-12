
using FluentValidation;
using WebAPI.Domain;

namespace WebAPI.Application.Validators;


public sealed class UpdateUserDTOValidator : AbstractValidator<UpdateUserDTO>
{
    public UpdateUserDTOValidator()
    {
        RuleFor(x => x.UserName).NotEmpty().WithMessage("Username is required.")
            .MaximumLength(20).WithMessage("Username cannot exceed 20 characters.");

        RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email format is invalid.");
    }
    
    
}