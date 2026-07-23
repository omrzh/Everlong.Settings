namespace Everlong.Settings;

/// <summary>
/// Typed backing store for a single settings property.
/// </summary>
/// <typeparam name="T">The property value type.</typeparam>
public sealed class PropertyBacker<T>
{
  private readonly string _configKey;
  private ISettingsStore? _store;

  /// <param name="keyPrefix">
  /// The dot-separated prefix inherited from the parent section.
  /// Pass <see cref="string.Empty"/> for root-level properties.
  /// </param>
  /// <param name="propertyName">The C# property name (used as the leaf key segment).</param>
  /// <param name="initialValue">
  /// Fallback used when the store does not contain a value for this property.
  /// </param>
  public PropertyBacker(string keyPrefix, string propertyName, T initialValue)
  {
    _configKey = string.IsNullOrEmpty(keyPrefix)
      ? propertyName
      : $"{keyPrefix}.{propertyName}";

    Value = initialValue;
  }

  /// <summary>Gets the current in-memory value.</summary>
  public T Value { get; private set; }

  /// <summary>
  /// Replaces the backing store reference used by this backer.
  /// </summary>
  public void SetStore(ISettingsStore store) => _store = store;

  /// <summary>
  /// Attempts to update the value.
  /// If <paramref name="value"/> differs from the current value, persists it to the store and returns
  /// <see langword="true"/>. Returns <see langword="false"/> when the value is unchanged.
  /// </summary>
  public bool TrySet(T value)
  {
    if (EqualityComparer<T>.Default.Equals(Value, value))
      return false;

    Value = value;
    (_store ?? throw new InvalidOperationException("Property backer store has not been initialized."))
      .SetValue(_configKey, value);
    return true;
  }

  /// <summary>
  /// Re-pulls the value from the backing store without writing back to it.
  /// Updates <see cref="Value"/> in-memory and returns <see langword="true"/> if the value changed,
  /// <see langword="false"/> if it is unchanged.
  /// </summary>
  /// <param name="fallback">
  /// The fallback value passed to <see cref="ISettingsStore.GetValue{T}"/> when the key is absent.
  /// Must be the same fallback used when the backer was constructed.
  /// </param>
  /// <remarks>
  /// Called by the generated <c>ISettingsSection.Initialize</c> after backer construction to
  /// support re-initialization semantics. On first initialization this is always a no-op
  /// (the constructor already loaded the value from the store). On re-initialization it detects
  /// changed store values and signals the caller to fire <see cref="System.ComponentModel.INotifyPropertyChanged.PropertyChanged"/>.
  /// </remarks>
  public bool GetInitialValue(T fallback)
  {
    var value = (_store ?? throw new InvalidOperationException("Property backer store has not been initialized."))
      .GetValue(_configKey, fallback);
    if (EqualityComparer<T>.Default.Equals(Value, value))
      return false;

    Value = value;
    return true;
  }
}
