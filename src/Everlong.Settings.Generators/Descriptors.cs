using Microsoft.CodeAnalysis;
using Everlong.Settings.Generators.Constants;

namespace Everlong.Settings.Generators;

// Rule: ID,Category 不可以硬编码； title, message可以硬编码
internal static class Descriptors
{
  // 0xxx: Core runtime errors & prerequisites (Deadlock, Reentrancy, Partial keyword, Property rules)
  internal const string DeadlockId = "NSTR0001";
  internal const string ReentrancyId = "NSTR0002";
  internal const string DiscouragedId = "NSTR0003";
  internal const string PropertySetterId = "NSTR0004";
  internal const string PropertyStaticId = "NSTR0005";
  internal const string PropertyInitPartialId = "NSTR0006";
  internal const string ClassPartialId = "NSTR0007";

  // 1xxx: UI Framework (ViewModels, Views, Mappings, Templates, Routes, Dialogs, Configuration)

  // 1002-1004: Naming & Structure
  internal const string NestedTriggerTypeId = "NSTR1002";
  internal const string RouteClassId = "NSTR1003";
  internal const string RouteSuffixId = "NSTR1004";

  // 1010-1017: Mappings & Relationships
  internal const string MappingConflictId = "NSTR1010";
  internal const string MappingRedundantId = "NSTR1011";
  internal const string UnmappedViewId = "NSTR1013";
  internal const string ImportFromCurrentAssemblyId = "NSTR1014";
  internal const string ImportFromNesterId = "NSTR1015";
  internal const string ImportFromFullNameId = "NSTR1016";
  internal const string ImportFromMissingManifestId = "NSTR1017";

  // 1020-1026: View/ViewModel Diagnostics
  internal const string MissingParameterlessConstructorId = "NSTR1020";
  internal const string OrphanViewModelId = "NSTR1021";
  internal const string DataTemplateOrphanViewModelId = "NSTR1022";
  internal const string MissingRouteViewId = "NSTR1023";
  internal const string InferredMappingCandidateId = "NSTR1024";
  internal const string NestedCandidateIgnoredId = "NSTR1025";
  internal const string FieldInjectionToPropertyId = "NSTR1027";

  // 1030-1034: Configuration & Infrastructure
  internal const string MultipleServiceTablesId = "NSTR1030";
  internal const string MultipleViewLocatorsId = "NSTR1031";
  internal const string MultipleViewDictionariesId = "NSTR1032";
  internal const string DialogSessionInheritanceId = "NSTR1033";
  internal const string DialogSessionNamingId = "NSTR1034";

  // 1040-1041: Injection & Code Generation Rules
  internal const string InjectableRequiredId = "NSTR1040";
  internal const string ReadonlyInjectionId = "NSTR1041";

  // 9xxx: System & Internal Errors (reserved)
  internal const string TransformErrorId = "NSTR9998";
  internal const string ExecutionErrorId = "NSTR9999";

  // 2xxx: Settings
  internal const string InvalidSettingFallbackId = "NSTR2001";
  internal const string SectionPropertyMustBeGetOnlyId = "NSTR2002";
  internal const string SectionPropertyTypeIsSettingsGroupId = "NSTR2003";
  internal const string SectionPropertyMustBePartialId = "NSTR2004";


  internal static class Suppression
  {
    internal const string ReadonlyInjectionId = "NSTRSP001";
    internal const string CS0649 = "CS0649";
    internal const string Justification = "Field is injected by generated member injection code.";
  }

  internal static class Category
  {
    internal const string View = "View";
    internal const string Deadlock = "Deadlock";
    internal const string Reentrancy = "Reentrancy";
    internal const string Discouraged = "Discouraged";
    internal const string Injection = "Injection";
    internal const string Usage = "Usage";
    internal const string Framework = "Framework";
    internal const string Naming = "Naming";
    internal const string Route = "Route";
    internal const string Transform = "Transform";
    internal const string Generator = "Generator";
    internal const string Configuration = "Configuration";
    internal const string Dialog = "Dialog";
  }

  // 2xxx: Settings
  internal static readonly DiagnosticDescriptor InvalidSettingFallback = new(
    InvalidSettingFallbackId,
    "Invalid [Setting] fallback type",
    "The fallback value passed to [Setting] on property '{0}' must be an enum constant; use a typed overload (bool, int, long, double, decimal, string) for primitives",
    Category.Usage,
    DiagnosticSeverity.Error,
    isEnabledByDefault: true);

  internal static readonly DiagnosticDescriptor SectionPropertyMustBeGetOnly = new(
    SectionPropertyMustBeGetOnlyId,
    "[Section] property must be get-only",
    "Property '{0}' is marked [Section] and must be get-only; remove the setter",
    Category.Usage,
    DiagnosticSeverity.Error,
    isEnabledByDefault: true);

  internal static readonly DiagnosticDescriptor SectionPropertyTypeIsSettingsGroup = new(
    SectionPropertyTypeIsSettingsGroupId,
    "[Section] property type is a root settings group",
    "Property '{0}' has type '{1}' which is annotated with [Settings] (a root group); consider annotating '{1}' with [Section] instead to avoid a nested root group",
    Category.Usage,
    DiagnosticSeverity.Warning,
    isEnabledByDefault: true);

  internal static readonly DiagnosticDescriptor SectionPropertyMustBePartial = new(
    SectionPropertyMustBePartialId,
    "[Section] property must be partial",
    "Property '{0}' is marked [Section] and must be declared as partial",
    Category.Usage,
    DiagnosticSeverity.Error,
    isEnabledByDefault: true);

  internal static readonly DiagnosticDescriptor MissingParameterlessConstructor = new(
    MissingParameterlessConstructorId,
    "Missing parameterless constructor",
    "View '{0}' must have a internal parameterless constructor",
    Category.View,
    DiagnosticSeverity.Warning,
    isEnabledByDefault: true);

  // 0xxx: Core / Safety / Infrastructure
  internal static readonly DiagnosticDescriptor DeadlockDescriptor = new(
    DeadlockId,
    "Deadlock Error",
    "Awaiting '{0}' inside '{1}' will cause a deadlock. The navigation queue is blocked by this callback. Use 'Dispatcher.UIThread.Post(() => ...)' to schedule it safely.",
    Category.Deadlock,
    DiagnosticSeverity.Error,
    isEnabledByDefault: true);

  internal static readonly DiagnosticDescriptor ReentrancyDescriptor = new(
    ReentrancyId,
    "Unsafe Re-entrant Navigation",
    "Direct fire-and-forget navigation inside '{0}' is unsafe. Wrap the navigation call in 'Dispatcher.UIThread.Post(() => ...)' to schedule it safely.",
    Category.Reentrancy,
    DiagnosticSeverity.Warning,
    isEnabledByDefault: true);

  internal static readonly DiagnosticDescriptor DiscouragedDescriptor = new(
    DiscouragedId,
    "Discouraged Navigation Await",
    "Awaiting navigation inside '{0}' is safe but discouraged as it delays the navigation completion. Consider using 'Dispatcher.UIThread.Post' or fire-and-forget.",
    Category.Discouraged,
    DiagnosticSeverity.Warning,
    isEnabledByDefault: true);

  internal static readonly DiagnosticDescriptor PropertySetter = new(
    PropertySetterId,
    "Property must have a setter",
    "Property '{0}' must have a setter to be injectable",
    Category.Injection,
    DiagnosticSeverity.Error,
    isEnabledByDefault: true);

  internal static readonly DiagnosticDescriptor PropertyStatic = new(
    PropertyStaticId,
    "Member must not be static",
    "Member '{0}' must not be static to be injectable",
    Category.Injection,
    DiagnosticSeverity.Error,
    isEnabledByDefault: true);

  internal static readonly DiagnosticDescriptor PropertyInitPartial = new(
    PropertyInitPartialId,
    "Init-only property must be partial",
    "Init-only property '{0}' must be partial to be injectable",
    Category.Injection,
    DiagnosticSeverity.Error,
    isEnabledByDefault: true);

  internal static readonly DiagnosticDescriptor TargetPartial = new(
    ClassPartialId,
    "Class must be partial",
    "The target type '{0}' must be partial to allow code generation",
    Category.Usage,
    DiagnosticSeverity.Error,
    isEnabledByDefault: true);

  internal static readonly DiagnosticDescriptor ReadonlyInjection = new(
    ReadonlyInjectionId,
    "Readonly field injection",
    "Field '{0}' is readonly and cannot be injected. Remove readonly or use constructor injection.",
    Category.Injection,
    DiagnosticSeverity.Error,
    isEnabledByDefault: true);

  // 1xxx: Views & ViewModels
  internal static readonly DiagnosticDescriptor OrphanViewModel = new(
    OrphanViewModelId,
    "Route ViewModel has no View",
    "Route ViewModel '{0}' has no associated View; navigation will fail at runtime",
    Category.View,
    DiagnosticSeverity.Warning,
    isEnabledByDefault: true);

  internal static readonly DiagnosticDescriptor MissingRouteView = new(
    MissingRouteViewId,
    "Route ViewModel has no View (strict)",
    "Route ViewModel '{0}' has no associated View; navigation will fail at runtime. Add [ViewFor<{0}>] to a View or declare [Mapping<{0}, TView>] on the ViewLocator.",
    Category.View,
    DiagnosticSeverity.Error,
    isEnabledByDefault: true);

  internal static readonly DiagnosticDescriptor MappingConflict = new(
    MappingConflictId,
    "Conflicting Mapping and ViewFor mappings",
    "'{0}' is mapped to '{1}' by [Mapping], which overrides [ViewFor] mapping to '{2}'. Remove one of them.",
    Category.View, DiagnosticSeverity.Warning, isEnabledByDefault: true,
    description: "If both [ViewFor] and [Mapping] are present, [Mapping] takes precedence.",
    customTags: WellKnownDiagnosticTags.CompilationEnd);

  internal static readonly DiagnosticDescriptor MappingRedundant = new(
    MappingRedundantId,
    "Redundant Mapping",
    "This [Mapping<{1}, {0}>] is redundant; [ViewFor<{0}>] on '{1}' already provides the same mapping",
    Category.View, DiagnosticSeverity.Info, isEnabledByDefault: true,
    customTags: WellKnownDiagnosticTags.CompilationEnd);

  internal static readonly DiagnosticDescriptor UnmappedView = new(
    UnmappedViewId,
    "Unmapped View in ViewLocator",
    "View '{0}' has no ViewModel mapping in the ViewLocator. Add [Mapping<TModel, {0}>] on the ViewLocator class to declare it explicitly, or add [ViewFor<TModel>] to the view.",
    Category.View, DiagnosticSeverity.Warning, isEnabledByDefault: true,
    description: "View is not mapped to any ViewModel. Consider using [ViewFor] or naming conventions.");

  internal static readonly DiagnosticDescriptor DataTemplateOrphanViewModel = new(
    DataTemplateOrphanViewModelId,
    "DataTemplate orphan ViewModel",
    "ViewModel '{0}' has no associated View for DataTemplate and will render fallback TextBlock",
    Category.View,
    DiagnosticSeverity.Warning,
    isEnabledByDefault: true);

  internal static readonly DiagnosticDescriptor ImportFromCurrentAssembly= new(
    ImportFromCurrentAssemblyId,
    "Invalid ImportFrom usage",
    "Type '{0}' is in the current assembly and does not need to be imported via [ImportFrom]",
    Category.Configuration,
    DiagnosticSeverity.Error,
    isEnabledByDefault: true);

  internal static readonly DiagnosticDescriptor ImportFromNester = new(
    ImportFromNesterId,
    "Redundant ImportFrom usage",
    "Type '{0}' is from Nester assembly which is implicitly imported",
    Category.Configuration,
    DiagnosticSeverity.Warning,
    isEnabledByDefault: true);

  internal static readonly DiagnosticDescriptor ImportFromFullName = new(
    ImportFromFullNameId,
    "Use fully-qualified type name in ImportFrom",
    "Type '{0}' should use fully-qualified name '{1}'",
    Category.Configuration,
    DiagnosticSeverity.Info,
    isEnabledByDefault: true,
    customTags: WellKnownDiagnosticTags.Unnecessary);

  internal static readonly DiagnosticDescriptor ImportFromMissingManifest = new(
    ImportFromMissingManifestId,
    "ImportFrom target missing manifest",
    "Type '{0}' is imported from assembly '{1}', but that assembly has no [assembly: ExportedContract]. Check whether the referenced assembly exported manifest entries or whether ImportFrom<T> points to the wrong type.",
    Category.Configuration,
    DiagnosticSeverity.Error,
    isEnabledByDefault: true);

  internal static readonly DiagnosticDescriptor InferredMappingCandidate = new(
    InferredMappingCandidateId,
    "Inferred Mapping Candidate",
    "ViewModel '{0}' can be mapped to View '{1}' by convention. Apply code fix to add [Mapping<{0}, {1}>] explicitly.",
    Category.View,
    DiagnosticSeverity.Info,
    isEnabledByDefault: true,
    customTags: WellKnownDiagnosticTags.CompilationEnd);

  internal static readonly DiagnosticDescriptor NestedTriggerType = new(
    NestedTriggerTypeId,
    "Nested trigger type is not allowed",
    "Type '{0}' cannot be used as ViewLocator/WpfViewLocator trigger because it is a nested class. Move it to a top-level type.",
    Category.Configuration,
    DiagnosticSeverity.Error,
    isEnabledByDefault: true);

  internal static readonly DiagnosticDescriptor NestedCandidateIgnored = new(
    NestedCandidateIgnoredId,
    "Nested candidate is ignored",
    "Type '{0}' is a nested class and is ignored by automatic ViewLocator/WpfViewLocator discovery. Use explicit [Mapping<,>] on the trigger if needed.",
    Category.View,
    DiagnosticSeverity.Warning,
    isEnabledByDefault: true);

  internal static readonly DiagnosticDescriptor FieldInjectionToProperty = new(
    FieldInjectionToPropertyId,
    "Use partial property injection",
    "Consider using partial property injection for field '{0}'",
    Category.Injection,
    DiagnosticSeverity.Info,
    isEnabledByDefault: true);

  internal static readonly DiagnosticDescriptor InjectableRequired = new(
    InjectableRequiredId,
    "Missing [Injectable] on injection type",
    "Type '{0}' contains [Inject] members and must be marked with [Injectable]",
    Category.Usage,
    DiagnosticSeverity.Error,
    isEnabledByDefault: true);

  // 1xxx: UI Framework Routes
  internal static readonly DiagnosticDescriptor RouteSuffix = new(
    RouteSuffixId,
    "Route class name should end with 'Route'",
    "The class '{0}' should end with 'Route' suffix",
    Category.Route,
    DiagnosticSeverity.Warning,
    isEnabledByDefault: true);

  internal static readonly DiagnosticDescriptor RouteClass = new(
    RouteClassId,
    "Invalid Route Class",
    "Route class '{0}' must not be abstract",
    Category.Route,
    DiagnosticSeverity.Error,
    isEnabledByDefault: true);

  internal static readonly DiagnosticDescriptor MultipleServiceTables = new(
    MultipleServiceTablesId,
    "Multiple ServiceRegistrar attributes",
    "Multiple [ServiceRegistrar] attributes found. Only one ServiceRegistrar is allowed per assembly.",
    Category.Configuration,
    DiagnosticSeverity.Error,
    isEnabledByDefault: true);

  internal static readonly DiagnosticDescriptor MultipleViewLocators = new(
    MultipleViewLocatorsId,
    "Multiple ViewLocator attributes",
    "Multiple [ViewLocator] attributes found. Only one ViewLocator is allowed per assembly.",
    Category.Configuration,
    DiagnosticSeverity.Error,
    isEnabledByDefault: true);

  internal static readonly DiagnosticDescriptor MultipleViewDictionaries = new(
    MultipleViewDictionariesId,
    "Multiple WpfViewLocator attributes",
    "Multiple [WpfViewLocator] attributes found. Only one WpfViewLocator is allowed per assembly.",
    Category.Configuration,
    DiagnosticSeverity.Error,
    isEnabledByDefault: true);

  // 1xxx: UI Framework Dialogs
  internal static readonly DiagnosticDescriptor DialogSessionInheritance = new(
    DialogSessionInheritanceId,
    "DialogSession inheritance",
    "DialogSession '{0}' should inherit from 'DialogSessionBase' or 'DialogSessionBase<T>' to avoid code generation",
    Category.Dialog,
    DiagnosticSeverity.Warning,
    isEnabledByDefault: true);

  internal static readonly DiagnosticDescriptor DialogSessionNaming = new(
    DialogSessionNamingId,
    "DialogSession naming",
    "DialogSession '{0}' should end with 'DialogSession'",
    Category.Dialog,
    DiagnosticSeverity.Warning,
    isEnabledByDefault: true);

  // 9xxx: System & Internal Errors
  internal static readonly DiagnosticDescriptor TransformError = new(
    TransformErrorId,
    "Transform Error",
    "Generator internal error at transform phase: input={0}; message={1}; stack={2}",
    Category.Transform,
    DiagnosticSeverity.Error,
    isEnabledByDefault: true);

  internal static readonly DiagnosticDescriptor ExecutionError = new(
    ExecutionErrorId,
    "Source Generator Exception",
    "Generator internal error at execution phase: input={0}; message={1}; stack={2}",
    Category.Generator,
    DiagnosticSeverity.Error,
    isEnabledByDefault: true);
}
