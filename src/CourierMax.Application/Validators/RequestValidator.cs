namespace CourierMax.Application.Validators;

/// <summary>Utilidad para validaciones transversales rápidas (early returns).</summary>
public static class RequestValidator
{
    public static string Required(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new Domain.Exceptions.ValidationException(field, "Campo obligatorio.");
        return value;
    }
}
