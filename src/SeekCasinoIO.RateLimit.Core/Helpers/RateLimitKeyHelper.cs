using System.Security.Cryptography;
using System.Text;

namespace SeekCasinoIO.RateLimit.Core.Helpers;

/// <summary>
/// Helper methods for creating and managing rate limit keys.
/// </summary>
public static class RateLimitKeyHelper
{
    /// <summary>
    /// Creates a rate limit key that combines client ID and resource.
    /// </summary>
    /// <param name="clientId">The client identifier.</param>
    /// <param name="resource">The resource/endpoint.</param>
    /// <returns>A rate limit key.</returns>
    public static string CreateKey(string clientId, string resource)
    {
        return $"rl:{NormalizeIdentifier(clientId)}:{NormalizeIdentifier(resource)}";
    }

    /// <summary>
    /// Creates a pattern for client keys.
    /// </summary>
    /// <param name="clientId">The client identifier.</param>
    /// <returns>A pattern for matching all keys for the specified client.</returns>
    public static string CreateClientKeyPattern(string clientId)
    {
        return $"rl:{NormalizeIdentifier(clientId)}:*";
    }

    /// <summary>
    /// Creates a pattern for resource keys.
    /// </summary>
    /// <param name="resource">The resource/endpoint.</param>
    /// <returns>A pattern for matching all keys for the specified resource.</returns>
    public static string CreateResourceKeyPattern(string resource)
    {
        return $"rl:*:{NormalizeIdentifier(resource)}";
    }

    /// <summary>
    /// Normalizes an identifier by removing invalid characters and truncating as needed.
    /// </summary>
    /// <param name="identifier">The identifier to normalize.</param>
    /// <returns>The normalized identifier.</returns>
    private static string NormalizeIdentifier(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
        {
            return "empty";
        }

        // Replace problematic characters
        var normalized = identifier
            .Replace(':', '_')
            .Replace(' ', '_')
            .Replace('/', '_')
            .Replace('\\', '_')
            .Replace('?', '_')
            .Replace('&', '_')
            .Replace('=', '_')
            .Replace('+', '_');

        // If it's too long, use a hash instead
        if (normalized.Length > 64)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(identifier));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        return normalized;
    }
}