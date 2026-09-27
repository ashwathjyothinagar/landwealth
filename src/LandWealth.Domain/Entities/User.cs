using LandWealth.Domain.Common;

namespace LandWealth.Domain.Entities;

public class User : AggregateRoot<Guid>
{
    private User() { }

    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string FullName { get; private set; } = default!;
    public string PreferredCurrency { get; private set; } = "INR";
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastModifiedAt { get; private set; }

    public static User Create(string email, string passwordHash, string fullName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);

        return new User
        {
            Id = Guid.NewGuid(),
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            FullName = fullName.Trim(),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    public void UpdateProfile(string fullName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        FullName = fullName.Trim();
        LastModifiedAt = DateTime.UtcNow;
    }

    public void SetPasswordHash(string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        PasswordHash = passwordHash;
        LastModifiedAt = DateTime.UtcNow;
    }

    public void Deactivate() { IsActive = false; LastModifiedAt = DateTime.UtcNow; }
    public void Activate() { IsActive = true; LastModifiedAt = DateTime.UtcNow; }
}
