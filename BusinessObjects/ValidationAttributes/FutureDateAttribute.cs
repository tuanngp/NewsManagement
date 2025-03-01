using System.ComponentModel.DataAnnotations;

namespace BusinessObjects.ValidationAttributes;

public class FutureDateAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value == null) return true; // Allow null dates

        return value is DateTime date && date > DateTime.Now;
    }
}
