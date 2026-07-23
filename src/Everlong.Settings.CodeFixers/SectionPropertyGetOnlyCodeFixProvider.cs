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

namespace Everlong.Settings.CodeFixers;

/// <summary>
/// Code fix for NSTR2002 — removes the setter accessor from a <c>[Section]</c> property.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SectionPropertyGetOnlyCodeFixProvider)), Shared]
public sealed class SectionPropertyGetOnlyCodeFixProvider : CodeFixProvider
{
  public override ImmutableArray<string> FixableDiagnosticIds =>
    ImmutableArray.Create(Descriptors.SectionPropertyMustBeGetOnlyId);

  public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

  public override async Task RegisterCodeFixesAsync(CodeFixContext context)
  {
    var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
    var diagnostic = context.Diagnostics.FirstOrDefault(d => d.Id == Descriptors.SectionPropertyMustBeGetOnlyId);
    if (diagnostic == null || root == null)
      return;

    var propDecl = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent?
      .AncestorsAndSelf()
      .OfType<PropertyDeclarationSyntax>()
      .FirstOrDefault();

    if (propDecl == null)
      return;

    context.RegisterCodeFix(
      CodeAction.Create(
        title: "Remove setter from [Section] property",
        createChangedDocument: c => RemoveSetterAsync(context.Document, propDecl, c),
        equivalenceKey: nameof(SectionPropertyGetOnlyCodeFixProvider)),
      diagnostic);
  }

  private static async Task<Document> RemoveSetterAsync(
    Document document,
    PropertyDeclarationSyntax propDecl,
    CancellationToken cancellationToken)
  {
    var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
    if (root == null) return document;

    // Find and remove the setter accessor
    var setterAccessor = propDecl.AccessorList?.Accessors
      .FirstOrDefault(a => a.IsKind(SyntaxKind.SetAccessorDeclaration)
                           || a.IsKind(SyntaxKind.InitAccessorDeclaration));

    if (setterAccessor == null)
      return document;

    var newAccessorList = propDecl.AccessorList!.WithAccessors(
      propDecl.AccessorList.Accessors.Remove(setterAccessor));

    // If only one accessor remains, normalize whitespace
    var newPropDecl = propDecl.WithAccessorList(newAccessorList);

    var newRoot = root.ReplaceNode(propDecl, newPropDecl);
    return document.WithSyntaxRoot(newRoot);
  }
}
