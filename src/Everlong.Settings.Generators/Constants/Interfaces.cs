namespace Everlong.Settings.Generators.Constants;

internal static class Interfaces
{
  internal const string IDataTemplate = "IDataTemplate";
  internal const string IDataTemplateFull = "Avalonia.Controls.Templates.IDataTemplate";
  internal const string IEphemeralHint = "IEphemeralHint";
  internal const string IPayloadHint1 = "IPayloadHint`1";
  internal const string IDetailHint1 = "IDetailHint`1";
  internal const string IAuthHint = "IAuthHint";
  internal const string IAuthHintFull = $"{Ns.NesterAuth}.{IAuthHint}";

  // Lifecycle Interfaces
  internal const string IShellInputHandler = "IShellInputHandler";
  internal const string INavigatedFrom = "INavigatedFrom";
  internal const string IRequireInit = "IRequireInit";
  internal const string IPreloadable = "IPreloadable";
  internal const string IBodyAware = "IBodyAware";
  internal const string INavigatedTo = "INavigatedTo";
  internal const string INavigatedTo1 = "INavigatedTo`1";
  internal const string IDialogSession = "IDialogSession";

  internal const string IServiceProvider = "IServiceProvider";
  internal const string IServiceCollection = "IServiceCollection";
  internal const string IInjectable = "IInjectable";
}

internal static class Methods
{
  // Lifecycle Methods
  internal const string HandleAsync = "HandleAsync";
  internal const string OnNavigatedFromAsync = "OnNavigatedFromAsync";
  internal const string InitializeAsync = "InitializeAsync";
  internal const string PreloadAsync = "PreloadAsync";
  internal const string OnBodyChangedAsync = "OnBodyChangedAsync";
  internal const string OnNavigatedToAsync = "OnNavigatedToAsync";

  internal const string TryAddTransient = "TryAddTransient";
  internal const string GetService = "GetService";
  internal const string GetRequiredService = "GetRequiredService";
  internal const string GetKeyedService = "GetKeyedService";
  internal const string GetRequiredKeyedService = "GetRequiredKeyedService";
  internal const string Inject = "Inject";
  internal const string Close = "Close";
  internal const string Build = "Build";
  internal const string Match = "Match";
}
