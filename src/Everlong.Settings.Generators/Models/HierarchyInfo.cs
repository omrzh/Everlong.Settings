using Everlong.Settings.Generators.Extensions;

namespace Everlong.Settings.Generators.Models;

/// <summary>
/// A model describing the hierarchy info for a specific type.
/// </summary>
/// <param name="FilenameHint">The filename hint for the current type.</param>
/// <param name="MetadataName">The metadata name for the current type.</param>
/// <param name="Namespace">Gets the namespace for the current type.</param>
/// <param name="Hierarchy">Gets the sequence of type definitions containing the current type.</param>
internal sealed partial record HierarchyInfo(
  string FilenameHint,
  string MetadataName,
  string Namespace,
  EquatableArray<TypeInfo> Hierarchy)
{
  /// <summary>
  /// Creates a new <see cref="HierarchyInfo"/> instance from a given <see cref="INamedTypeSymbol"/>.
  /// </summary>
  /// <param name="typeSymbol">The input <see cref="INamedTypeSymbol"/> instance to gather info for.</param>
  /// <returns>A <see cref="HierarchyInfo"/> instance describing <paramref name="typeSymbol"/>.</returns>
  public static HierarchyInfo From(INamedTypeSymbol typeSymbol)
  {
    using ImmutableArrayBuilder<TypeInfo> hierarchy = ImmutableArrayBuilder<TypeInfo>.Rent();

    for (INamedTypeSymbol? parent = typeSymbol;
         parent is not null;
         parent = parent.ContainingType)
    {
      string typeParameters = parent.TypeParameters.IsEmpty
                                ? string.Empty
                                : $"<{string.Join(", ", parent.TypeParameters.Select(t => t.Name))}>";

      string modifiers = parent.IsStatic ? "static" : string.Empty;

      hierarchy.Add(new TypeInfo(
                      parent.Name,
                      typeParameters,
                      parent.TypeKind,
                      parent.IsRecord,
                      modifiers));
    }

    return new(
      typeSymbol.GetFullyQualifiedMetadataName(),
      typeSymbol.MetadataName,
      typeSymbol.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
      hierarchy.ToImmutable());
  }

  /// <summary>
  /// Creates a <see cref="CompilationUnitSyntax"/> instance with the specified members.
  /// </summary>
  /// <param name="memberDeclarations">The members to add to the type.</param>
  /// <param name="baseTypes">The base types to add to the type.</param>
  /// <param name="usings">The usings to add to the compilation unit.</param>
  /// <returns>A <see cref="CompilationUnitSyntax"/> instance with the specified members.</returns>
  public CompilationUnitSyntax GetCompilationUnit(
    ImmutableArray<MemberDeclarationSyntax> memberDeclarations,
    IEnumerable<BaseTypeSyntax>? baseTypes = null,
    IEnumerable<UsingDirectiveSyntax>? usings = null)
  {
    // Get the syntax for the target type (innermost type is at index 0)
    TypeDeclarationSyntax typeDeclaration = Hierarchy[0].GetSyntax();

    if (baseTypes != null)
    {
      typeDeclaration = typeDeclaration.WithBaseList(BaseList(SeparatedList(baseTypes)));
    }

    typeDeclaration = typeDeclaration.WithMembers(List(memberDeclarations));

    // Wrap in parent types
    for (int i = 1; i < Hierarchy.Length; i++)
    {
      typeDeclaration = Hierarchy[i].GetSyntax()
        .WithMembers(SingletonList<MemberDeclarationSyntax>(typeDeclaration));
    }

    // Create compilation unit
    var compilationUnit = CompilationUnit();

    // Add usings if present
    if (usings != null)
    {
      compilationUnit = compilationUnit.WithUsings(List(usings));
    }

    // Add namespace if present
    if (!string.IsNullOrEmpty(Namespace))
    {
      compilationUnit = compilationUnit.AddMembers(
        FileScopedNamespaceDeclaration(ParseName(Namespace))
          .WithMembers(SingletonList<MemberDeclarationSyntax>(typeDeclaration)));
    }
    else
    {
      compilationUnit = compilationUnit.AddMembers(typeDeclaration);
    }

    // Add header and nullable
    // Note: We avoid NormalizeWhitespace here to let the caller decide, 
    // or we should be careful not to mess up user code if we were editing (but we are generating).
    // However, NormalizeWhitespace is generally good for fresh code.
    return compilationUnit;
  }
}
