using FinanceTracker.Domain.Common;

namespace FinanceTracker.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string FullName { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    private User() { }

    public static User Create(string email, string passwordHash, string fullName)
    {
        if (string.IsNullOrWhiteSpace(email)) throw new DomainException("Email is required");
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new DomainException("Password hash is required");
        if (string.IsNullOrWhiteSpace(fullName)) throw new DomainException("Full name is required");

        return new User
        {
            Id = Guid.NewGuid(),
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            FullName = fullName.Trim(),
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateProfile(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) throw new DomainException("Full name is required");
        FullName = fullName.Trim();
    }

    public void ChangePasswordHash(string newHash)
    {
        if (string.IsNullOrWhiteSpace(newHash)) throw new DomainException("Password hash is required");
        PasswordHash = newHash;
    }
}
