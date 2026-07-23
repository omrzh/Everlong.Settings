using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Everlong.Settings.Generators;
using Everlong.Settings.Generators.Constants;

namespace Everlong.Settings.CodeFixers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PartialKeywordCodeFixProvider)), Shared]
public class PartialKeywordCodeFixProvider : CodeFixProvider
{
  public sealed override ImmutableArray<string> FixableDiagnosticIds =>
    ImmutableArray.Create(Descriptors.ClassPartialId, Descriptors.SectionPropertyMustBePartialId);

  public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

  public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
  {
    var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
    if (root == null) return;

    // ELST0007: class/type must be partial
    var classPartialDiag = context.Diagnostics.FirstOrDefault(d => d.Id == Descriptors.ClassPartialId);
    if (classPartialDiag != null)
    {
      var typeDecl = root.FindToken(classPartialDiag.Location.SourceSpan.Start).Parent?
        .AncestorsAndSelf()
        .OfType<TypeDeclarationSyntax>()
        .FirstOrDefault();

      if (typeDecl != null)
      {
        context.RegisterCodeFix(
          CodeAction.Create(
            title: "Make class partial",
            createChangedDocument: c => MakeTypePartialAsync(context.Document, typeDecl, c),
            equivalenceKey: "MakeClassPartial"),
          classPartialDiag);
      }
    }

    // ELST2004: [Section] property must be partial
    var propPartialDiag = context.Diagnostics.FirstOrDefault(d => d.Id == Descriptors.SectionPropertyMustBePartialId);
    if (propPartialDiag != null)
    {
      var propDecl = root.FindToken(propPartialDiag.Location.SourceSpan.Start).Parent?
        .AncestorsAndSelf()
        .OfType<PropertyDeclarationSyntax>()
        .FirstOrDefault();

      if (propDecl != null)
      {
        context.RegisterCodeFix(
          CodeAction.Create(
            title: "Make property partial",
            createChangedDocument: c => MakePropertyPartialAsync(context.Document, propDecl, c),
            equivalenceKey: "MakePropertyPartial"),
          propPartialDiag);
      }
    }
  }

  private static async Task<Document> MakeTypePartialAsync(Document document,
                                                       TypeDeclarationSyntax typeDecl,
                                                       CancellationToken cancellationToken)
  {
    var root = await document.GetSyntaxRootAsync(cancellationToken);
    if (root == null)
      return document;

    if (typeDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
      return document;

    var partialToken = SyntaxFactory.Token(SyntaxKind.PartialKeyword).WithTrailingTrivia(SyntaxFactory.Space);
    var newModifiers = typeDecl.Modifiers.Add(partialToken);
    var newTypeDecl = typeDecl.WithModifiers(newModifiers);

    var newRoot = root.ReplaceNode(typeDecl, newTypeDecl);
    return document.WithSyntaxRoot(newRoot);
  }

  private static async Task<Document> MakePropertyPartialAsync(Document document,
                                                               PropertyDeclarationSyntax propDecl,
                                                               CancellationToken cancellationToken)
  {
    var root = await document.GetSyntaxRootAsync(cancellationToken);
    if (root == null)
      return document;

    if (propDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
      return document;

    var partialToken = SyntaxFactory.Token(SyntaxKind.PartialKeyword).WithTrailingTrivia(SyntaxFactory.Space);
    var newModifiers = propDecl.Modifiers.Add(partialToken);
    var newPropDecl = propDecl.WithModifiers(newModifiers);

    var newRoot = root.ReplaceNode(propDecl, newPropDecl);
    return document.WithSyntaxRoot(newRoot);
  }
}
