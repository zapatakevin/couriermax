namespace CourierMax.Domain.Exceptions;

/// <summary>
/// Excepción base para violaciones de reglas de negocio.
/// Es capturada en la capa de presentación para traducirse a respuestas HTTP 4xx.
/// </summary>
public class DomainException : Exception
{
    public string Code { get; }

    public DomainException(string code, string message) : base(message)
    {
        Code = code;
    }
}

/// <summary>Entidad no encontrada.</summary>
public class NotFoundException : DomainException
{
    public NotFoundException(string entity, object key)
        : base("NOT_FOUND", $"{entity} con id '{key}' no fue encontrado.") { }
}

/// <summary>Violación de una regla de negocio (HTTP 409 Conflict o 422).</summary>
public class BusinessRuleException : DomainException
{
    public BusinessRuleException(string code, string message) : base(code, message) { }
}

/// <summary>Validación de entrada (HTTP 400).</summary>
public class ValidationException : DomainException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("VALIDATION", "La solicitud contiene datos inválidos.")
    {
        Errors = errors;
    }

    public ValidationException(string field, string message)
        : this(new Dictionary<string, string[]> { [field] = new[] { message } }) { }
}
