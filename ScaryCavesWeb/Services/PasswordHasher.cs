namespace ScaryCavesWeb.Services;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

public class PasswordHasher
{
    /// <summary>
    /// Creates a hashed password with salt.
    /// </summary>
    /// <remarks>
    /// Not trying to be REALLY secure but wanting better than MD5
    /// </remarks>
    /// <param name="password"></param>
    /// <returns>the hashed and salted password</returns>
    public string HashPassword(string password)
    {
        byte[] salt = GenerateSalt();
        string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256, // SHA 256
            iterationCount: 10_000,
            numBytesRequested: 32));
        return $"{Convert.ToBase64String(salt)}:{hashed}";
    }

    /// <summary>
    /// Does this provided password match?
    /// </summary>
    /// <param name="storedHash"></param>
    /// <param name="providedPassword"></param>
    /// <returns></returns>
    public bool VerifyPassword(string storedHash, string providedPassword)
    {
        var parts = storedHash.Split(":");
        byte[] salt = Convert.FromBase64String(parts[0]);
        var passwordHash = parts[1];
        var hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: providedPassword,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 10_000,
            numBytesRequested: 32));

        return hashed == passwordHash;
    }

    private byte[] GenerateSalt()
    {
        byte[] salt = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(salt);
        return salt;
    }

}
