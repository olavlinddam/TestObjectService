using FluentValidation;
using System.Text.RegularExpressions;

namespace TestObjectService.Models.Validation;

///<summary>
/// This class is used to validate the properties of a TestObject object. 
/// </summary>

public class TestObjectValidator : AbstractValidator<TestObject>
{
    public TestObjectValidator()
    {
        RuleFor(x => x.Id)
            .NotNull().WithMessage("Id can not be null.")
            .NotEmpty().WithMessage("Id can not be empty.");
        
        RuleFor(x => x.Type)
            .NotNull().WithMessage("Type can not be null.")
            .NotEmpty().WithMessage("Type can not be empty.")
            .Custom((value, context) =>
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    context.AddFailure("Type should not be whitespace.");
                }
                else if (!Regex.IsMatch(value, @"^[a-zA-Z0-9-_]+$"))
                {
                    context.AddFailure("Type can only contain alphanumeric characters, hyphens, and underscores.");
                }
            })
            .Length(1, 999).WithMessage("Type must have a length between 1 and 999 characters.");

        RuleFor(x => x.SerialNr)
            .NotNull().WithMessage("'S/N' cannot be null.")
            .NotEmpty().WithMessage("'S/N' cannot be empty.")
            .Matches(new Regex(@"^[a-zA-Z0-9-_]+$")).WithMessage("'S/N' can only contain alphanumeric characters, hyphens, and underscores.");
        

        // RuleFor(x => x.ImagePath)
        //     .NotNull().WithMessage("'ImagePath' cannot be null.")
        //     .NotEmpty().WithMessage("'ImagePath' cannot be empty.")
        //     .Must(BeAValidFileName).WithMessage("'ImagePath' must be a valid file name.")
        //     .Must(HaveValidImageExtension).WithMessage("'ImagePath' must have a valid image file extension.");
        
        RuleFor(x => x.SniffingPoints)
            .NotNull().WithMessage("'SniffingPoints' cannot be null.")
            .Must(BeAValidList).WithMessage("'SniffingPoints' must contain valid entries.")
            .ForEach(sniffingPointValidator => sniffingPointValidator.SetValidator(new SniffingPointValidator()));
    }
    

    private static bool IsValidGuid(Guid guid)
    {
        return guid != Guid.Empty;
    }
    
    private static bool BeAValidFileName(string path)
    {
        try
        {
            var fileName = Path.GetFileName(path);
            return !string.IsNullOrEmpty(fileName) && fileName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool HaveValidImageExtension(string path)
    {
        if (string.IsNullOrEmpty(path)) return false;

        var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
        var extension = Path.GetExtension(path).ToLower();

        return validExtensions.Contains(extension);
    }

    private static bool BeAValidList(List<SniffingPoint> list)
    {
        return list != null && list.Count > 0;
    }
        

}
