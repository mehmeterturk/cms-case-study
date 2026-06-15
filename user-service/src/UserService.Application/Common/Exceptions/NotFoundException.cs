namespace UserService.Application.Common.Exceptions;

/// <summary>
/// İstenen kayıt bulunamadığında fırlatılır. API katmanında 404'e çevrilir.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string entity, object key)
        : base($"{entity} ({key}) bulunamadı.")
    {
    }
}
