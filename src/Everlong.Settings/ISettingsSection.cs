using System.ComponentModel;

namespace Everlong.Settings;

/// <summary>
/// A contract for settings classes that receive a backing store and key prefix
/// for all contained properties.
/// </summary>
public interface ISettingsSection : INotifyPropertyChanged
{
  /// <summary> Initializes from the provided <paramref name="store"/>. </summary>
  void Initialize(ISettingsStore store, string sectionKey);
}
