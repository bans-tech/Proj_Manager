namespace Pm.Api.Domain.Entities;

public sealed class User
{
    public Guid Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public Board? Board { get; set; }
}
