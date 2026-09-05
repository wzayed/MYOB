using System.ComponentModel.DataAnnotations;

namespace MYOB.Support;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class NotFutureDateAttribute : ValidationAttribute
{
    public NotFutureDateAttribute()
    {
        ErrorMessage = "لا يمكن إدخال تاريخ بعد تاريخ اليوم.";
    }

    public override bool IsValid(object? value)
    {
        return value switch
        {
            null => true,
            DateOnly date => date <= BusinessDate.Today,
            DateTime date => DateOnly.FromDateTime(date) <= BusinessDate.Today,
            DateTimeOffset date => DateOnly.FromDateTime(date.DateTime) <= BusinessDate.Today,
            _ => false
        };
    }
}
