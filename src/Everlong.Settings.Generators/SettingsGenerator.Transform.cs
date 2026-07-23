using Everlong.Settings.Generators.Extensions;

namespace Everlong.Settings.Generators;

partial class SettingsGenerator
{
  private static Result<SettingsClassInfo?> TransformSettingsClass(
    GeneratorAttributeSyntaxContext context,
    CancellationToken token,
    bool isRoot)
  {
    using ImmutableArrayBuilder<DiagnosticInfo> diagnostics = ImmutableArrayBuilder<DiagnosticInfo>.Rent();

    try
    {
      if (context.TargetSymbol is not INamedTypeSymbol classSymbol)
        return new Result<SettingsClassInfo?>(null, diagnostics.ToImmutable());

      if (!SymbolEqualityComparer.Default.Equals(classSymbol.ContainingAssembly,
                                                 context.SemanticModel.Compilation.Assembly))
        return new Result<SettingsClassInfo?>(null, diagnostics.ToImmutable());

      // For types declared across multiple partial parts, process only the canonical one.
      if (classSymbol.DeclaringSyntaxReferences.Length > 1)
      {
        var canonical = classSymbol.DeclaringSyntaxReferences
          .OrderBy(static r => r.SyntaxTree.FilePath, StringComparer.Ordinal)
          .ThenBy(static r => r.Span.Start)
          .First();

        if (!ReferenceEquals(context.TargetNode.SyntaxTree, canonical.SyntaxTree)
            || context.TargetNode.Span != canonical.Span)
          return new Result<SettingsClassInfo?>(null, diagnostics.ToImmutable());
      }

      var settings = new List<SettingPropertyInfo>();
      var sectionProps = new List<SectionPropertyInfo>();

      foreach (IPropertySymbol prop in classSymbol.GetMembers().OfType<IPropertySymbol>())
      {
        if (prop.IsImplicitlyDeclared)
          continue;

        token.ThrowIfCancellationRequested();

        var propAttrs = prop.GetAttributes();

        // ── [Setting] ──────────────────────────────────────────────────
        var settingAttr =
          propAttrs.FirstOrDefault(static a => a.AttributeClass?.ToDisplayString() == Attributes.SettingFull);

        if (settingAttr != null)
        {
          bool isNullable = IsNullableSyntax(prop, token);
          bool isValueType = prop.Type.IsValueType;
          bool isComplex = IsComplexTypeSymbol(prop.Type);
          bool hasCoercion = HasCoercionAttribute(propAttrs);

          string? fallback = null;
          (decimal Min, decimal Max)? range = null;
          (int Min, int Max)? length = null;

          if (!isComplex)
          {
            fallback = ExtractSettingFallback(settingAttr, prop, diagnostics);
            range = ExtractRange(propAttrs);
            length = ExtractLength(propAttrs);
          }
          else if (!settingAttr.ConstructorArguments.IsDefaultOrEmpty)
          {
            // Primitive fallback provided for a complex-type property — invalid
            diagnostics.Add(Descriptors.InvalidSettingFallback, prop, prop.Name);
          }

          settings.Add(new SettingPropertyInfo(
                         prop.Name,
                         prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                         isNullable,
                         isValueType,
                         isComplex,
                         fallback,
                         hasCoercion,
                         range,
                         length));
          continue;
        }

        // ── [Section] property ─────────────────────────────────────────
        var sectionPropAttr =
          propAttrs.FirstOrDefault(static a => a.AttributeClass?.ToDisplayString() == Attributes.SectionFull);

        if (sectionPropAttr != null)
        {
          ValidateSectionProperty(prop, diagnostics, token, out bool isValidSectionType);

          if (isValidSectionType)
          {
            sectionProps.Add(new SectionPropertyInfo(
                               prop.Name,
                               prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
          }
        }
      }

      token.ThrowIfCancellationRequested();
      var info = new SettingsClassInfo(
        HierarchyInfo.From(classSymbol),
        isRoot,
        settings.ToEquatableArray(),
        sectionProps.ToEquatableArray());

      return new Result<SettingsClassInfo?>(info, diagnostics.ToImmutable());
    }
    catch (OperationCanceledException)
    {
      throw;
    }
    catch (Exception ex)
    {
      diagnostics.Add(DiagnosticInfo.Create(Descriptors.TransformError, context.TargetNode, ex.Message));
      return new Result<SettingsClassInfo?>(null, diagnostics.ToImmutable());
    }
  }

  // ── Helpers ──────────────────────────────────────────────────────────

  /// <summary>
  /// Returns <see langword="true"/> when the type requires JSON serialization (non-primitive).
  /// Mirrors the runtime <c>IsComplexType&lt;T&gt;()</c> check in <c>SettingsStoreBase</c>.
  /// </summary>
  private static bool IsComplexTypeSymbol(ITypeSymbol type)
  {
    // Unwrap Nullable<T>
    if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
      type = nullable.TypeArguments[0];

    return type.SpecialType switch
    {
      SpecialType.System_Boolean => false,
      SpecialType.System_Int16   => false,
      SpecialType.System_Int32   => false,
      SpecialType.System_Int64   => false,
      SpecialType.System_Single  => false,
      SpecialType.System_Double  => false,
      SpecialType.System_String  => false,
      _                          => type is not INamedTypeSymbol { TypeKind: TypeKind.Enum }
                                    && !(type.ToDisplayString() == "decimal")
    };
  }
  private static bool IsNullableSyntax(IPropertySymbol prop, CancellationToken token)
  {
    if (prop.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(token) is PropertyDeclarationSyntax ps)
      return ps.Type is NullableTypeSyntax;
    return prop.NullableAnnotation == NullableAnnotation.Annotated;
  }

  /// <summary>Returns true when <c>[Coercion]</c> is present on the property.</summary>
  private static bool HasCoercionAttribute(ImmutableArray<AttributeData> attrs)
    => attrs.Any(static a => a.AttributeClass?.ToDisplayString() == Attributes.CoercionFull);

  /// <summary>Reads <c>[Range]</c> and returns (min, max), or <see langword="null"/> when absent.</summary>
  private static (decimal Min, decimal Max)? ExtractRange(ImmutableArray<AttributeData> attrs)
  {
    var rangeAttr = attrs.FirstOrDefault(static a =>
                                           a.AttributeClass?.ToDisplayString() ==
                                           "System.ComponentModel.DataAnnotations.RangeAttribute");

    if (rangeAttr == null || rangeAttr.ConstructorArguments.Length < 2)
      return null;

    decimal? min = ToDecimal(rangeAttr.ConstructorArguments[0]);
    decimal? max = ToDecimal(rangeAttr.ConstructorArguments[1]);

    return (min.HasValue && max.HasValue) ? (min.Value, max.Value) : null;
  }

  /// <summary>
  /// Reads <c>[Length]</c>, <c>[MaxLength]</c>, and <c>[MinLength]</c> attributes and
  /// returns a (min, max) length tuple, or <see langword="null"/> when none are present.
  /// </summary>
  private static (int Min, int Max)? ExtractLength(ImmutableArray<AttributeData> attrs)
  {
    int? min = null;
    int? max = null;

    foreach (var attr in attrs)
    {
      var name = attr.AttributeClass?.ToDisplayString();

      // [Length(minimumLength, maximumLength)] — .NET 6+
      if (name == "System.ComponentModel.DataAnnotations.LengthAttribute"
          && attr.ConstructorArguments.Length >= 2)
      {
        min = ToInt(attr.ConstructorArguments[0]) ?? min;
        max = ToInt(attr.ConstructorArguments[1]) ?? max;
        continue;
      }

      // [MaxLength(length)]
      if (name == "System.ComponentModel.DataAnnotations.MaxLengthAttribute"
          && attr.ConstructorArguments.Length >= 1)
      {
        max = ToInt(attr.ConstructorArguments[0]) ?? max;
        continue;
      }

      // [MinLength(length)]
      if (name == "System.ComponentModel.DataAnnotations.MinLengthAttribute"
          && attr.ConstructorArguments.Length >= 1)
      {
        min = ToInt(attr.ConstructorArguments[0]) ?? min;
      }
    }

    if (!min.HasValue && !max.HasValue) return null;
    return (min ?? 0, max ?? int.MaxValue);
  }

  /// <summary>Validates [Section] property constraints; sets <paramref name="isValidSectionType"/>.</summary>
  private static void ValidateSectionProperty(
    IPropertySymbol prop,
    in ImmutableArrayBuilder<DiagnosticInfo> diagnostics,
    CancellationToken token,
    out bool isValidSectionType)
  {
    isValidSectionType = false;

    // Must be get-only
    if (prop.SetMethod != null)
      diagnostics.Add(Descriptors.SectionPropertyMustBeGetOnly, prop, prop.Name);

    // Must be partial
    bool isPartial = false;
    if (prop.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(token) is PropertyDeclarationSyntax ps)
      isPartial = ps.Modifiers.Any(static m => m.IsKind(SyntaxKind.PartialKeyword));

    if (!isPartial)
      diagnostics.Add(Descriptors.SectionPropertyMustBePartial, prop, prop.Name);

    // Type must be [Section] or [Settings] class
    if (prop.Type is not INamedTypeSymbol typeSymbol)
      return;

    var typeAttrs = typeSymbol.GetAttributes();
    bool typeHasSection = typeAttrs.Any(static a => a.AttributeClass?.ToDisplayString() == Attributes.SectionFull);
    bool typeHasSettings = typeAttrs.Any(static a => a.AttributeClass?.ToDisplayString() == Attributes.SettingsFull);

    if (!typeHasSection && !typeHasSettings)
      return; // No valid settings attribute — skip generation (missing partial impl → natural compiler error)

    if (typeHasSettings && !typeHasSection)
      diagnostics.Add(Descriptors.SectionPropertyTypeIsSettingsGroup, prop, prop.Name, typeSymbol.Name);

    isValidSectionType = true;
  }

  /// <summary>Extracts the fallback expression string from a <c>[Setting]</c> attribute.</summary>
  private static string? ExtractSettingFallback(
    AttributeData attr,
    IPropertySymbol prop,
    in ImmutableArrayBuilder<DiagnosticInfo> diagnostics)
  {
    if (attr.ConstructorArguments.IsDefaultOrEmpty)
      return null; // no fallback: use default(T)

    TypedConstant arg = attr.ConstructorArguments[0];

    return arg.Kind switch
    {
      TypedConstantKind.Primitive when arg.Value is bool b => b ? "true" : "false",
      TypedConstantKind.Primitive when arg.Value is int i => i.ToString(),
      TypedConstantKind.Primitive when arg.Value is long l => $"{l}L",
      TypedConstantKind.Primitive when arg.Value is double d => $"{d}",
      TypedConstantKind.Primitive when arg.Value is float f => $"{f}f",
      TypedConstantKind.Primitive when arg.Value is decimal m => $"{m}m",
      TypedConstantKind.Primitive when arg.Value is string s => $"\"{EscapeString(s)}\"",
      TypedConstantKind.Enum when arg.Type is not null =>
        $"({arg.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}){arg.Value}",
      _ => ReportInvalidFallbackAndReturnNull(diagnostics, prop)
    };
  }

  private static string? ReportInvalidFallbackAndReturnNull(
    in ImmutableArrayBuilder<DiagnosticInfo> diagnostics,
    IPropertySymbol prop)
  {
    diagnostics.Add(Descriptors.InvalidSettingFallback, prop, prop.Name);
    return null;
  }

  private static decimal? ToDecimal(TypedConstant constant)
  {
    if (constant.Kind != TypedConstantKind.Primitive || constant.Value == null)
      return null;

    return constant.Value switch
    {
      int i => (decimal)i,
      long l => (decimal)l,
      double d => (decimal)d,
      float f => (decimal)f,
      decimal m => m,
      _ => null
    };
  }

  private static int? ToInt(TypedConstant constant)
  {
    if (constant.Kind != TypedConstantKind.Primitive || constant.Value == null)
      return null;

    return constant.Value switch
    {
      int i => i,
      long l => (int)l,
      _ => null
    };
  }

  private static string EscapeString(string s) => s.Replace("\\", @"\\").Replace("\"", "\\\"");
}
