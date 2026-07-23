using Microsoft.CodeAnalysis;

namespace Everlong.Settings.Generators;

internal static class Descriptors
{
  // ── ID format ──────────────────────────────────────────────────
  // ELSTxxxx — all Everlong.Settings diagnostics use this prefix.
  // 0xxx: class/type requirements (partial, etc.)
  // 2xxx: settings-specific (attribute misuse, property rules)
  // 9xxx: generator internal errors

  internal const string ClassPartialId = "ELST0007";
  internal const string InvalidSettingFallbackId = "ELST2001";
  internal const string SectionPropertyMustBeGetOnlyId = "ELST2002";
  internal const string SectionPropertyTypeIsSettingsGroupId = "ELST2003";
  internal const string SectionPropertyMustBePartialId = "ELST2004";
  internal const string TransformErrorId = "ELST9998";
  internal const string ExecutionErrorId = "ELST9999";

  internal static class Category
  {
    internal const string Usage = "Usage";
    internal const string Transform = "Transform";
    internal const string Generator = "Generator";
  }

  /// <summary>ELST0007: [Settings] / [Section] class must be partial.</summary>
  internal static readonly DiagnosticDescriptor TargetPartial = new(
    ClassPartialId,
    "Class must be partial",
    "The target type '{0}' must be partial to allow code generation",
    Category.Usage,
    DiagnosticSeverity.Error,
    isEnabledByDefault: true);

  /// <summary>ELST2001: [Setting] fallback is neither a primitive nor an enum constant.</summary>
  internal static readonly DiagnosticDescriptor InvalidSettingFallback = new(
    InvalidSettingFallbackId,
    "Invalid [Setting] fallback type",
    "The fallback value passed to [Setting] on property '{0}' must be an enum constant; " +
    "use a typed overload (bool, int, long, double, decimal, string) for primitives",
    Category.Usage,
    DiagnosticSeverity.Error,
    isEnabledByDefault: true);

  /// <summary>ELST2002: [Section] property must not have a setter.</summary>
  internal static readonly DiagnosticDescriptor SectionPropertyMustBeGetOnly = new(
    SectionPropertyMustBeGetOnlyId,
    "[Section] property must be get-only",
    "Property '{0}' is marked [Section] and must be get-only; remove the setter",
    Category.Usage,
    DiagnosticSeverity.Error,
    isEnabledByDefault: true);

  /// <summary>ELST2003: [Section] property type is a root [Settings] group.</summary>
  internal static readonly DiagnosticDescriptor SectionPropertyTypeIsSettingsGroup = new(
    SectionPropertyTypeIsSettingsGroupId,
    "[Section] property type is a root settings group",
    "Property '{0}' has type '{1}' which is annotated with [Settings] (a root group); " +
    "consider annotating '{1}' with [Section] instead to avoid a nested root group",
    Category.Usage,
    DiagnosticSeverity.Warning,
    isEnabledByDefault: true);

  /// <summary>ELST2004: [Section] property must be partial.</summary>
  internal static readonly DiagnosticDescriptor SectionPropertyMustBePartial = new(
    SectionPropertyMustBePartialId,
    "[Section] property must be partial",
    "Property '{0}' is marked [Section] and must be declared as partial",
    Category.Usage,
    DiagnosticSeverity.Error,
    isEnabledByDefault: true);

  /// <summary>ELST9998: Generator transform phase internal error.</summary>
  internal static readonly DiagnosticDescriptor TransformError = new(
    TransformErrorId,
    "Transform Error",
    "Generator internal error at transform phase: input={0}; message={1}; stack={2}",
    Category.Transform,
    DiagnosticSeverity.Error,
    isEnabledByDefault: true);

  /// <summary>ELST9999: Generator execution phase internal error.</summary>
  internal static readonly DiagnosticDescriptor ExecutionError = new(
    ExecutionErrorId,
    "Source Generator Exception",
    "Generator internal error at execution phase: input={0}; message={1}; stack={2}",
    Category.Generator,
    DiagnosticSeverity.Error,
    isEnabledByDefault: true);
}
