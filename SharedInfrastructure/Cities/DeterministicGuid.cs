using System.Security.Cryptography;
using System.Text;

namespace SharedInfrastructure.Cities;

public static class DeterministicGuid
{
    public static Guid Create(string seed)
    {
        if (string.IsNullOrWhiteSpace(seed))
        {
            throw new ArgumentException("Seed cannot be null or empty", nameof(seed));
        }

        var normalized = seed.Trim().ToLowerInvariant();
        var bytes = Encoding.UTF8.GetBytes(normalized);
        var hash = SHA256.HashData(bytes);
        var guidBytes = new byte[16];
        Array.Copy(hash, guidBytes, 16);
        return new Guid(guidBytes);
    }
}
