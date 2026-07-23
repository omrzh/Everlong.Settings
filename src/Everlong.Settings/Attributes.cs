namespace Everlong.Settings;

/// <summary>
/// Marks a class as a root-level settings group.
/// The source generator will implement <see cref="ISettingsRoot"/> on the class,
/// generate a <c>Default</c> static property, and wire up all
/// <c>[Setting]</c>, <c>[ComplexSetting]</c>, and <c>[Section]</c> members.
/// The class must be <c>partial</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class SettingsAttribute : Attribute;

/// <summary>
/// Marks a class as a nested settings section, or a property as containing a nested settings section.
/// </summary>
/// <remarks>
/// <para>
/// When applied to a <b>class</b>: the source generator implements <see cref="ISettingsSection"/>.
/// The class must be <c>partial</c>.
/// </para>
/// <para>
/// When applied to a <b>property</b>: the property must be <c>partial</c>, get-only, and its
/// type must be a class annotated with <c>[Section]</c>.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
public sealed class SectionAttribute : Attribute;

/// <summary>
/// Marks a property as a primitive settings value backed by the <see cref="ISettingsStore"/>.
/// </summary>
/// <remarks>
/// Supported fallback types: <see cref="bool"/>, <see cref="int"/>, <see cref="long"/>,
/// <see cref="double"/>, <see cref="decimal"/>, <see cref="string"/>, and any <c>enum</c>
/// (passed as <see cref="object"/>; the source generator validates that it is an enum constant).
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SettingAttribute : Attribute
{
  /// <summary>Marks the property as a setting with the type's default as the fallback.</summary>
  public SettingAttribute() { }

  /// <inheritdoc cref="SettingAttribute()"/>
  public SettingAttribute(bool fallback) { }

  /// <inheritdoc cref="SettingAttribute()"/>
  public SettingAttribute(int fallback) { }

  /// <inheritdoc cref="SettingAttribute()"/>
  public SettingAttribute(long fallback) { }

  /// <inheritdoc cref="SettingAttribute()"/>
  public SettingAttribute(double fallback) { }

  /// <inheritdoc cref="SettingAttribute()"/>
  public SettingAttribute(decimal fallback) { }

  /// <inheritdoc cref="SettingAttribute()"/>
  public SettingAttribute(string fallback) { }

  /// <summary>
  /// Enum-typed fallback. Passing any non-enum value will cause the source generator
  /// to emit a <c>NSTR2001</c> diagnostic error.
  /// </summary>
  public SettingAttribute(object fallback) { }
}

/// <summary>
/// Signals the source generator to emit a <c>private static partial T CoerceXXX(T value)</c>
/// method declaration alongside the property implementation.
/// The generated property setter calls this method before writing to the backing store,
/// giving the developer full control over value transformation (clamping, normalising, etc.).
/// </summary>
/// <remarks>
/// Implement the generated partial method in your own source file.
/// If <c>[Range]</c> or <c>[MaxLength]</c>/<c>[MinLength]</c>/<c>[Length]</c> are also present,
/// their inline coerce runs <em>first</em>, then the <c>CoerceXXX</c> method is called.
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public sealed class CoercionAttribute : Attribute;
