using Everlong.Settings.Generators.Models;

namespace Everlong.Settings.Generators;

/// <summary>A property marked with <c>[Setting]</c>.</summary>
internal sealed record SettingPropertyInfo(
  string Name,
  string FullyQualifiedType, // non-nullable, e.g., "global::System.Boolean" — for typeof() and PropertyBacker<T>
  bool IsNullable, // true = ref type with '?', or Nullable<T> for value types
  bool IsValueType,
  bool IsComplex, // true = JSON serialization; false = Convert.ChangeType
  string? FallbackExpression, // null = use default(T) / default!
  bool HasCoercion, // [Coercion] → generate CoerceXXX partial method + call in setter
  (decimal Min, decimal Max)? Range, // [Range] → inline clamp in setter
  (int Min, int Max)? Length); // [MaxLength]/[MinLength]/[Length] → inline slice in setter

/// <summary>A property marked with <c>[Section]</c> (property-level).</summary>
internal sealed record SectionPropertyInfo(
  string Name,
  string FullyQualifiedType // fully qualified type of the section class
);

/// <summary>All data needed to generate one settings class (either root group or sub-section).</summary>
internal sealed record SettingsClassInfo(
  HierarchyInfo Hierarchy,
  bool IsRoot, // true → ISettingsRoot; false → ISettingsSection
  EquatableArray<SettingPropertyInfo> Settings,
  EquatableArray<SectionPropertyInfo> Sections
);
