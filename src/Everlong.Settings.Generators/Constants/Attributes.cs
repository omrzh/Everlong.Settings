namespace Everlong.Settings.Generators.Constants;

internal static class Attributes
{
  // Attributes
  internal const string Category = Ns.Nester;

  internal const string RouteForPrefix = "Route";
  internal const string RouteFor = $"RouteForAttribute`1";
  internal const string RouteForFull = $"{Ns.NesterNav}.{RouteFor}";
  internal const string GenerateRoute = "GenerateRouteAttribute";
  internal const string GenerateRouteFull = $"{Ns.NesterNav}.{GenerateRoute}";
  internal const string Layout1Prefix = "LayoutAttribute";
  internal const string Layout1Full = $"{Ns.NesterNav}.{Layout1Prefix}`1";



  internal const string DialogSessionPrefix = "DialogSessionAttribute";
  internal const string DialogSessionFull = $"{Ns.NesterDialogs}.{DialogSessionPrefix}";
  internal const string DialogSessionGenericFull = $"{Ns.NesterDialogs}.{DialogSessionPrefix}`1";

  internal const string ViewFor = "ViewForAttribute`1";
  internal const string ViewForFull = $"{Ns.NesterViews}.{ViewFor}";
  internal const string TemplateModel = "TemplateModelAttribute";
  internal const string TemplateModelFull = $"{Ns.NesterViews}.{TemplateModel}";
  internal const string TemplateView = "TemplateViewAttribute";
  internal const string TemplateViewFull = $"{Ns.NesterViews}.{TemplateView}";
  internal const string LayoutControl = "LayoutControlAttribute";
  internal const string LayoutControlFull = $"{Ns.NesterViews}.{LayoutControl}";

  internal const string ImportFromFull = $"{Ns.NesterViews}.ImportFromAttribute`1";
  internal const string InjectFull = $"{Ns.NesterDi}.InjectAttribute";
  internal const string InjectableFull = $"{Ns.NesterDi}.InjectableAttribute";
  internal const string Authorize = "AuthorizeAttribute";
  internal const string EditorBrowsable = "global::System.ComponentModel.EditorBrowsable";

  internal const string ExportContracts = "ExportContractsAttribute";
  internal const string ExportContractsFull = $"{Ns.NesterContracts}.{ExportContracts}";
  internal const string ExportedContract = "ExportedContractAttribute";
  internal const string ExportedContractFull = $"{Ns.NesterContracts}.{ExportedContract}";

  internal const string Ephemeral = "EphemeralAttribute";
  internal const string EphemeralFull = $"{Ns.NesterNav}.{Ephemeral}";

  internal const string Payload = "PayloadAttribute";
  internal const string Payload1 = "PayloadAttribute`1";
  internal const string Payload1Full = $"{Ns.NesterNav}.{Payload1}";

  internal const string Detail = "DetailAttribute";
  internal const string Detail1 = "DetailAttribute`1";
  internal const string Detail1Full = $"{Ns.NesterNav}.{Detail1}";

  internal const string ViewLocator = "ViewLocatorAttribute";
  internal const string ViewLocatorFull = $"{Ns.NesterTemplates}.{ViewLocator}";

  internal const string WpfViewLocator = "WpfViewLocatorAttribute";
  internal const string WpfViewLocatorFull = $"{Ns.NesterTemplates}.{WpfViewLocator}";

  internal const string Mapping = "MappingAttribute";
  internal const string Mapping2Full = $"{Ns.NesterMappings}.MappingAttribute`2";

  internal const string SingletonFull = $"{Ns.NesterDi}.SingletonAttribute";
  internal const string SingletonGenericFull = $"{Ns.NesterDi}.SingletonAttribute`1";
  internal const string TransientFull = $"{Ns.NesterDi}.TransientAttribute";
  internal const string TransientGenericFull = $"{Ns.NesterDi}.TransientAttribute`1";
  internal const string ScopedFull = $"{Ns.NesterDi}.ScopedAttribute";
  internal const string ScopedGenericFull = $"{Ns.NesterDi}.ScopedAttribute`1";

  internal const string ServiceRegistrar = "ServiceRegistrarAttribute";
  internal const string ServiceRegistrarFull = $"{Ns.NesterDi}.{ServiceRegistrar}";

  // Settings
  internal const string SettingsFull = $"{Ns.NesterSettings}.SettingsAttribute";
  internal const string SectionFull = $"{Ns.NesterSettings}.SectionAttribute";
  internal const string SettingFull = $"{Ns.NesterSettings}.SettingAttribute";
  internal const string CoercionFull = $"{Ns.NesterSettings}.CoercionAttribute";
}
