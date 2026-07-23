namespace Everlong.Settings.Generators.Models;

/// <summary>
/// A model describing a type info in a type hierarchy.
/// </summary>
/// <param name="Name">The name of the type.</param>
/// <param name="TypeParameters">The type parameters of the type (e.g. "&lt;T&gt;").</param>
/// <param name="Kind">The type of the type in the hierarchy.</param>
/// <param name="IsRecord">Whether the type is a record type.</param>
internal sealed record TypeInfo(string Name, string TypeParameters, TypeKind Kind, bool IsRecord, string? Modifiers = null)
{
  /// <summary>
  /// Creates a <see cref="TypeDeclarationSyntax"/> instance for the current info.
  /// </summary>
  /// <returns>A <see cref="TypeDeclarationSyntax"/> instance for the current info.</returns>
  public TypeDeclarationSyntax GetSyntax()
  {
    TypeDeclarationSyntax declaration = Kind switch
    {
      TypeKind.Struct => StructDeclaration(Name),
      TypeKind.Interface => InterfaceDeclaration(Name),
      TypeKind.Class when IsRecord =>
        RecordDeclaration(Token(SyntaxKind.RecordKeyword), Name)
          .WithOpenBraceToken(Token(SyntaxKind.OpenBraceToken))
          .WithCloseBraceToken(Token(SyntaxKind.CloseBraceToken)),
      _ => ClassDeclaration(Name)
    };

    // Add modifiers if any
    if (!string.IsNullOrEmpty(Modifiers))
    {
      foreach (var modifier in ParseTokens(Modifiers!))
      {
        declaration = declaration.AddModifiers(modifier);
      }
    }

    // Add partial modifier
    declaration = declaration.AddModifiers(Token(SyntaxKind.PartialKeyword));

    // Add type parameters if any
    if (!string.IsNullOrEmpty(TypeParameters))
    {
      // We construct a dummy class to parse the type parameter list
      // This is safer than manually constructing TypeParameterSyntax nodes
      // "class Dummy<T> {}"
      var dummyCode = $"class Dummy{TypeParameters} {{}}";
      var compilationUnit = ParseCompilationUnit(dummyCode);
      var dummyClass = (ClassDeclarationSyntax)compilationUnit.Members[0];

      if (dummyClass.TypeParameterList != null)
      {
        declaration = declaration.WithTypeParameterList(dummyClass.TypeParameterList);
      }
    }

    return declaration;
  }
}
