namespace Everlong.Settings.Generators;

partial class SettingsGenerator
{
  private static void WrappedExecute(SourceProductionContext context, SettingsClassInfo info)
    => ExecuteHelper.Execute(context, info, static (ctx, i) => Execute(ctx, i));

  private static void Execute(SourceProductionContext context, SettingsClassInfo? info)
  {
    if (info == null) return;

    var members = new List<MemberDeclarationSyntax>();

    // ── Static Default (ISettingsRoot only) ───────────────────────────
    if (info.IsRoot)
    {
      string typeName = info.Hierarchy.MetadataName;
      members.Add(ParseMemberDeclaration(
                    $"public static {typeName} Default\n" +
                    $"{{\n" +
                    $"  get\n" +
                    $"  {{\n" +
                    $"    if (field is null)\n" +
                    $"    {{\n" +
                    $"      field = new {typeName}();\n" +
                    $"      ((global::Everlong.Settings.ISettingsRoot)field).Initialize(new global::Everlong.Settings.NoOpSettingsStore());\n" +
                    $"    }}\n" +
                    $"    return field;\n" +
                    $"  }}\n" +
                    $"}}\n")!);

      // ── _store backing field ──────────────────────────────────────────
      members.Add(ParseMemberDeclaration("private ISettingsStore? _store;\n")!);
    }

    // ── Backing fields for [Setting] ───────────────────────────────────
    foreach (var s in info.Settings)
    {
      string backerT = BackerTypeArg(s.FullyQualifiedType, s.IsNullable, s.IsValueType);
      members.Add(ParseMemberDeclaration(
                    $"private PropertyBacker<{backerT}>? _{ToCamel(s.Name)}Backer;\n")!);
    }


    // ── Backing fields for [Section] ──────────────────────────────────
    foreach (var sec in info.Sections)
    {
      members.Add(ParseMemberDeclaration(
                    $"private {sec.FullyQualifiedType}? _{ToCamel(sec.Name)};\n")!);
    }

    // ── [Section] partial property implementations ────────────────────
    foreach (var sec in info.Sections)
    {
      members.Add(ParseMemberDeclaration(
                    $"public partial {sec.FullyQualifiedType} {sec.Name}\n" +
                    $"{{\n" +
                    $"  get => _{ToCamel(sec.Name)} ??= new {sec.FullyQualifiedType}();\n" +
                    $"}}\n")!);
    }

    // ── [Setting] partial property implementations ────────────────────
    foreach (var s in info.Settings)
    {
      string backerT = BackerTypeArg(s.FullyQualifiedType, s.IsNullable, s.IsValueType);
      string fallback = s.IsComplex
        ? (s.IsNullable ? "default" : $"Get{s.Name}Default()")
        : s.FallbackExpression ?? DefaultExpression(s.FullyQualifiedType, s.IsNullable, s.IsValueType);
      string backerField = $"_{ToCamel(s.Name)}Backer";
      string propType = s.IsNullable && !s.IsValueType ? s.FullyQualifiedType + "?" : s.FullyQualifiedType;

      var sb = new StringBuilder();
      sb.AppendLine($"public partial {propType} {s.Name}");
      sb.AppendLine("{");
      sb.AppendLine($"  get => {backerField}?.Value ?? {fallback};");
      sb.AppendLine("  set");
      sb.AppendLine("  {");
      AppendInlineCoerce(sb, s.Range, s.Length, s.HasCoercion, s.Name, backerT, "    ");
      sb.AppendLine($"    if ({backerField}!.TrySet(value))");
      sb.AppendLine("    {");
      sb.AppendLine($"      OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof({s.Name})));");
      sb.AppendLine($"      On{s.Name}Changed(value);");
      sb.AppendLine("    }");
      sb.AppendLine("  }");
      sb.AppendLine("}");

      members.Add(ParseMemberDeclaration(sb.ToString())!);

      // For non-nullable complex settings: emit partial method declaration for the default value
      if (s.IsComplex && !s.IsNullable)
      {
        members.Add(ParseMemberDeclaration(
                      $"private static partial {s.FullyQualifiedType} Get{s.Name}Default();\n")!);
      }

      // [Coercion] static partial method declaration
      if (s.HasCoercion)
      {
        members.Add(ParseMemberDeclaration(
                      $"private static partial {backerT} Coerce{s.Name}({backerT} value);\n")!);
      }

      // OnXXXChanged partial hook — always emitted
      members.Add(ParseMemberDeclaration(
                    $"partial void On{s.Name}Changed({backerT} value);\n")!);
    }

    // ── ISettingsRoot.Initialize (root only) ─────────────────────────
    if (info.IsRoot)
    {
      string typeName = info.Hierarchy.MetadataName;
      members.Add(ParseMemberDeclaration(
                    "/// <inheritdoc/>\n" +
                    "void ISettingsRoot.Initialize(ISettingsStore? store)\n" +
                    "{\n" +
                    "  if (store is null && _store is null)\n" +
                    "    throw new InvalidOperationException(\"Initialize must be called with a non-null store before it can be re-invoked.\");\n" +
                    "  if (store is not null)\n" +
                    "    _store = store;\n" +
                    "  ((ISettingsSection)this).Initialize(_store!, string.Empty);\n" +
                    "}\n")!);

      // ── Snapshot() ───────────────────────────────────────────────────
      members.Add(ParseMemberDeclaration(
                    $"/// <summary>\n" +
                    $"/// Creates a <see cref=\"global::Everlong.Settings.SettingsSnapshot{{T}}\"/> containing a shadow copy of this\n" +
                    $"/// settings object backed by a <see cref=\"global::Everlong.Settings.CommittableSettingsStore\"/>.\n" +
                    $"/// Mutations on the shadow are buffered and can be committed or discarded atomically.\n" +
                    $"/// </summary>\n" +
                    $"/// <exception cref=\"InvalidOperationException\">Thrown when the settings have not been initialized yet.</exception>\n" +
                    $"public SettingsSnapshot<{typeName}> Snapshot()\n" +
                    $"{{\n" +
                    $"  var store = new CommittableSettingsStore(this, _store ?? throw new InvalidOperationException(\"Cannot create a snapshot before the settings are initialized.\"));\n" +
                    $"  var shadow = new {typeName}();\n" +
                    $"  ((ISettingsRoot)shadow).Initialize(store);\n" +
                    $"  return new(shadow, store);\n" +
                    $"}}\n")!);
    }

    // ── ISettingsSection.Initialize ───────────────────────────────────
    members.Add(ParseMemberDeclaration(BuildInitializeMethod(info))!);

    // ── INPC ──────────────────────────────────────────────────────────
    members.Add(ParseMemberDeclaration(
                  "public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;\n")!);
    members.Add(ParseMemberDeclaration(
                  "protected virtual void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs args)\n" +
                  "  => PropertyChanged?.Invoke(this, args);\n")!);

    // ── Base types ────────────────────────────────────────────────────
    var baseTypes = new List<BaseTypeSyntax>
    {
      SimpleBaseType(ParseTypeName(info.IsRoot ? "ISettingsRoot" : "ISettingsSection"))
    };

    // ── Usings ────────────────────────────────────────────────────────
    var usings = new[]
    {
      UsingDirective(ParseName(Ns.System)),
      UsingDirective(ParseName(Ns.NesterSettings))
    };

    var compilationUnit = info.Hierarchy
      .GetCompilationUnit(members.ToImmutableArray(), baseTypes, usings)
      .WithLeadingTrivia(ParseLeadingTrivia(Conventions.AutoGeneratedHeader));

    var code = compilationUnit.NormalizeWhitespace(indentation: "  ").ToFullString();
    context.AddSource($"{info.Hierarchy.FilenameHint}.Settings.g.cs", code);
  }

  // ── Initialize method body builder ─────────────────────────────────

  private static string BuildInitializeMethod(SettingsClassInfo info)
  {
    var sb = new StringBuilder();
    sb.AppendLine("/// <inheritdoc/>");
    sb.AppendLine("void ISettingsSection.Initialize(ISettingsStore store, string keyPrefix)");
    sb.AppendLine("{");

    // [Setting] properties — create backer once, then re-pull value on subsequent calls
    foreach (var s in info.Settings)
    {
      string backerT = BackerTypeArg(s.FullyQualifiedType, s.IsNullable, s.IsValueType);
      string backerField = $"_{ToCamel(s.Name)}Backer";
      string fallback = s.IsComplex
        ? (s.IsNullable ? "default!" : $"Get{s.Name}Default()")
        : s.FallbackExpression ?? DefaultExpression(s.FullyQualifiedType, s.IsNullable, s.IsValueType);

      sb.AppendLine(
        $"  {backerField} ??= new PropertyBacker<{backerT}>(keyPrefix, nameof({s.Name}), {fallback});");
      sb.AppendLine($"  {backerField}.SetStore(store);");
      sb.AppendLine($"  if ({backerField}.GetInitialValue({fallback}))");
      sb.AppendLine("  {");
      sb.AppendLine($"    OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof({s.Name})));");
      sb.AppendLine($"    On{s.Name}Changed({backerField}.Value);");
      sb.AppendLine("  }");
    }

    // [Section] properties — recurse
    foreach (var sec in info.Sections)
    {
      sb.AppendLine($"  ((ISettingsSection){sec.Name}).Initialize(store, {ConfigKeyExpression(sec.Name)});");
    }

    sb.AppendLine("}");
    return sb.ToString();
  }

  // ── Inline coerce emitter ───────────────────────────────────────────

  /// <summary>
  /// Appends inline coerce statements to the setter body:
  /// Range clamp → Length clamp → [Coercion] call, in that order.
  /// </summary>
  private static void AppendInlineCoerce(
    StringBuilder sb,
    (decimal Min, decimal Max)? range,
    (int Min, int Max)? length,
    bool hasCoercion,
    string propName,
    string backerT,
    string indent)
  {
    if (range.HasValue)
    {
      var (min, max) = range.Value;
      sb.AppendLine($"{indent}if (value < {FormatDecimal(min)}) value = {FormatDecimal(min)};");
      sb.AppendLine($"{indent}else if (value > {FormatDecimal(max)}) value = {FormatDecimal(max)};");
    }

    if (length.HasValue)
    {
      var (min, max) = length.Value;
      if (max < int.MaxValue)
        sb.AppendLine($"{indent}if (value is {{ Length: > {max} }}) value = value[..{max}];");
      if (min > 0)
        sb.AppendLine($"{indent}if (value is {{ Length: < {min} }}) value = default!;");
    }

    if (hasCoercion)
      sb.AppendLine($"{indent}value = Coerce{propName}(value);");
  }

  private static string FormatDecimal(decimal d)
  {
    // Emit int literal when possible (cleaner output)
    if (d == Math.Truncate(d) && d >= int.MinValue && d <= int.MaxValue)
      return ((int)d).ToString();
    return $"{d}m";
  }

  // ── Small helpers ───────────────────────────────────────────────────

  /// <summary>Converts a PascalCase name to camelCase.</summary>
  private static string ToCamel(string name)
    => name.Length == 0 ? name : char.ToLowerInvariant(name[0]) + name.Substring(1);

  /// <summary>Returns the T for PropertyBacker&lt;T&gt; given the non-nullable type.</summary>
  private static string BackerTypeArg(string fullyQualifiedType, bool isNullable, bool isValueType)
    => (isNullable && !isValueType) ? fullyQualifiedType + "?" : fullyQualifiedType;

  /// <summary>
  /// Returns a <c>default</c> or <c>default!</c> expression for when no fallback is specified.
  /// </summary>
  private static string DefaultExpression(string fullyQualifiedType, bool isNullable, bool isValueType)
  {
    if (isValueType) return "default";
    if (isNullable) return "default";
    return "default!";
  }

  /// <summary>
  /// Emits the expression for the ConfigKey property:
  /// <c>string.IsNullOrEmpty(keyPrefix) ? nameof(X) : $"{keyPrefix}.{nameof(X)}"</c>
  /// </summary>
  private static string ConfigKeyExpression(string propName)
    => $"string.IsNullOrEmpty(keyPrefix) ? nameof({propName}) : $\"{{keyPrefix}}.{{nameof({propName})}}\"";
}
