using FluentValidation;

namespace TestObjectService.Models.Validation;

///<summary>
/// This class is used to validate the properties of a SniffingPoint object. 
/// </summary>
/// 
public class SniffingPointValidator : AbstractValidator<SniffingPoint>
{
    public SniffingPointValidator()
    {
        RuleFor(x => x.Id)
            .NotNull().WithMessage("'Id' cannot be null.")
            .NotEmpty().WithMessage("'Id' cannot be empty.")
            .Must(IsValidGuid).WithMessage("'Id' must be a valid GUID.");

        RuleFor(x => x.Name)
            .NotNull().WithMessage("'Name' cannot be null.")
            .NotEmpty().WithMessage("'Name' cannot be empty.")
            .Length(1, 255).WithMessage("'Name' must have a length between 1 and 255 characters.");

        // Assuming X and Y should be within specific ranges, adjust the values as necessary
        RuleFor(x => x.X)
            .InclusiveBetween(-1000, 1000).WithMessage("'X' must be between -1000 and 1000.");

        RuleFor(x => x.Y)
            .InclusiveBetween(-1000, 1000).WithMessage("'Y' must be between -1000 and 1000.");

        RuleFor(x => x.TestObjectId)
            .NotNull().WithMessage("'TestObjectId' cannot be null.")
            .NotEmpty().WithMessage("'TestObjectId' cannot be empty.")
            .Must(IsValidGuid).WithMessage("'TestObjectId' must be a valid GUID.");
    }

    private bool IsValidGuid(Guid guid)
    {
        return guid != Guid.Empty;
    }
}