namespace Everlong.Settings;

/// <summary>
/// Abstraction over the underlying persistence layer for settings values.
/// </summary>
public interface ISettingsStore
{
  /// <summary>Reads a value from the store, returning <paramref name="fallback"/> if absent.</summary>
  T GetValue<T>(string key, T fallback);

  /// <summary>Writes a value to the store.</summary>
  void SetValue<T>(string key, T value);
}
