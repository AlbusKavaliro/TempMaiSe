using System.Diagnostics.CodeAnalysis;

namespace AlbusKavaliro.TempMaiSe.Mailer;

/// <summary>
/// Provides extension methods for dictionaries.
/// </summary>
public static class DictionaryExtensions
{
    /// <summary>
    /// Tries to get the value associated with the specified key from the dictionary.
    /// </summary>
    /// <typeparam name="T">The type of the value to retrieve.</typeparam>
    /// <param name="dictionary">The dictionary to search.</param>
    /// <param name="key">The key of the value to retrieve.</param>
    /// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter.</param>
    /// <returns><c>true</c> if the dictionary contains an element with the specified key and the value is of type T; otherwise, <c>false</c>.</returns>
    public static bool TryGetValue<T>(this IDictionary<string, object> dictionary, string key, [MaybeNullWhen(false)] out T value)
    {
        ArgumentNullException.ThrowIfNull(dictionary);

        if (dictionary.TryGetValue(key, out object? obj) && obj is T t)
        {
            value = t;
            return true;
        }

        value = default!;
        return false;
    }
}
