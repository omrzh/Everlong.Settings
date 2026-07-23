namespace Everlong.Settings;

/// <summary>
/// A no-op implementation of <see cref="ISettingsStore"/>.
/// </summary>
public sealed class NoOpSettingsStore : ISettingsStore
{
  /// <summary>
  /// Returns the supplied fallback value.
  /// </summary>
  public T GetValue<T>(string key, T fallback) => fallback;

  /// <summary>
  /// Empty body
  /// </summary>
  public void SetValue<T>(string key, T value) { }
}
