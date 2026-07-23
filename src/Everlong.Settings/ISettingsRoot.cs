namespace Everlong.Settings;

/// <summary>
/// Implemented by top-level (root) settings classes.
/// Inherits <see cref="ISettingsSection"/> and adds a store-accepting initializer
/// with an empty key prefix.
/// </summary>
public interface ISettingsRoot : ISettingsSection
{
  /// <summary>
  /// Initializes or re-initializes all properties from the provided <paramref name="store"/>.
  /// </summary>
  /// <param name="store">
  /// The settings store to use. Pass <see langword="null"/> on subsequent calls to re-pull from the current store.
  /// </param>
  /// <exception cref="InvalidOperationException">
  /// Thrown when <paramref name="store"/> is <see langword="null"/> on the first call.
  /// </exception>
  void Initialize(ISettingsStore? store);
}
