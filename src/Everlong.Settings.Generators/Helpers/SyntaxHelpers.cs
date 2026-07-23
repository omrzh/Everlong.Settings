using Everlong.Settings.Generators.Models;
using Microsoft.CodeAnalysis;

namespace Everlong.Settings.Generators.Helpers;

public static class SyntaxHelpers
{
  // Parse simple statements
  public static StatementSyntax Statement(string code) => ParseStatement(code);
  
  // Parse with placeholder replacement (safe interpolation)
  public static StatementSyntax Statement(string template, params (string placeholder, string value)[] replacements)
  {
    var syntax = ParseStatement(template);
    foreach (var (placeholder, value) in replacements)
    {
      while (true)
      {
        var token = syntax.DescendantTokens()
          .FirstOrDefault(t => t.Text == placeholder);

        if (token == default)
          break;

        syntax = syntax.ReplaceToken(token, SyntaxFactory.Identifier(value));
      }
    }

    return syntax;
  }

  // Parse member declarations (fields, properties)
  public static MemberDeclarationSyntax Member(string code) => ParseMemberDeclaration(code)!;

  // Parse member with placeholder replacement
  public static MemberDeclarationSyntax Member(string template, params (string placeholder, string value)[] replacements)
  {
    var syntax = SyntaxFactory.ParseMemberDeclaration(template)!;
    foreach (var (placeholder, value) in replacements)
    {
      while (true)
      {
        var token = syntax.DescendantTokens()
          .FirstOrDefault(t => t.Text == placeholder);

        if (token == default)
          break;

        syntax = syntax.ReplaceToken(token, SyntaxFactory.Identifier(value));
      }
    }
    return syntax;
  }

  // Parse multiple statements
  public static IEnumerable<StatementSyntax> Statements(string code) =>
    ParseCompilationUnit(code)
      .DescendantNodes()
      .OfType<StatementSyntax>();

  public static MemberDeclarationSyntax WrapInClasses(MemberDeclarationSyntax member,
                                                      IEnumerable<ContainingTypeInfo> containingTypes)
  {
      var result = member;
      foreach (var info in containingTypes)
      {
          TypeDeclarationSyntax typeDecl = info.IsRecord
              ? RecordDeclaration(Token(SyntaxKind.RecordKeyword), info.Name)
              : ClassDeclaration(info.Name);

          var modifiers = new List<SyntaxToken>();
          if (info.IsStatic) modifiers.Add(Token(SyntaxKind.StaticKeyword));
          modifiers.Add(Token(SyntaxKind.PartialKeyword));

          typeDecl = typeDecl
              .WithModifiers(TokenList(modifiers))
              .WithMembers(SingletonList(result));
          
          result = typeDecl;
      }
      return result;
  }

  public static string GetAccessibilityString(Accessibility accessibility)
  {
      switch (accessibility)
      {
          case Accessibility.Public: return "public";
          case Accessibility.Internal: return "internal";
          case Accessibility.Private: return "private";
          case Accessibility.Protected: return "protected";
          case Accessibility.ProtectedAndInternal: return "private protected";
          case Accessibility.ProtectedOrInternal: return "protected internal";
          default: return "";
      }
  }
}
