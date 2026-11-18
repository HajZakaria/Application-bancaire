using System.Security.Cryptography;
using BankSim.Data;
using BankSim.Models;
using Microsoft.EntityFrameworkCore;

namespace BankSim.Services;

public class AuthService
{
    private readonly IDbContextFactory<BankDbContext> _factory;

    public AuthService(IDbContextFactory<BankDbContext> factory) => _factory = factory;

    public User? CurrentUser { get; private set; }

    public async Task<User> RegisterAsync(string name, string email, string password)
    {
        email = email.Trim().ToLowerInvariant();
        if (password.Length < 8)
            throw new InvalidOperationException("Mot de passe trop court (8 caractères minimum).");

        await using var db = await _factory.CreateDbContextAsync();
        if (await db.Users.AnyAsync(u => u.Email == email))
            throw new InvalidOperationException("Cet email est déjà utilisé.");

        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);

        var user = new User
        {
            Name = name.Trim(),
            Email = email,
            Salt = Convert.ToBase64String(salt),
            PasswordHash = Convert.ToBase64String(hash)
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        CurrentUser = user;
        return user;
    }

    public async Task<User> LoginAsync(string email, string password)
    {
        email = email.Trim().ToLowerInvariant();
        await using var db = await _factory.CreateDbContextAsync();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email)
                   ?? throw new InvalidOperationException("Identifiants invalides.");

        var salt = Convert.FromBase64String(user.Salt);
        var expected = Convert.FromBase64String(user.PasswordHash);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);

        if (!CryptographicOperations.FixedTimeEquals(expected, actual))
            throw new InvalidOperationException("Identifiants invalides.");

        CurrentUser = user;
        return user;
    }

    public void Logout() => CurrentUser = null;
}