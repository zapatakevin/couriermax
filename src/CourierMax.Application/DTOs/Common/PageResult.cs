namespace CourierMax.Application.DTOs.Common;

public sealed record PageResult<T>(IReadOnlyList<T> Items, int Total);
