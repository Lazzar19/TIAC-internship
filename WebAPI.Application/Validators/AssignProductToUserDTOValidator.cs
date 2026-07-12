using FluentValidation;
using WebAPI.Domain;

namespace WebAPI.Application.Validators;

public sealed class AssignProductToUserDTOValidator : AbstractValidator<AsigningUserToProductDTO>
{

    public AssignProductToUserDTOValidator()
    {
        RuleFor(x => x.ProductID).GreaterThan(0).WithMessage("Valid Product ID is required");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than 10");
    }
    
}