namespace Everlong.Settings.Generators.Analyzers;

/// <summary>
/// Reports <c>ELST2002</c> when a <c>[Section]</c>-annotated property has a setter.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SettingsPropertyAnalyzer : DiagnosticAnalyzer
{
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    ImmutableArray.Create(Descriptors.SectionPropertyMustBeGetOnly);

  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();

    context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.PropertyDeclaration);
  }

  private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
  {
    if (context.Node is not PropertyDeclarationSyntax propDecl)
      return;

    if (context.SemanticModel.GetDeclaredSymbol(propDecl) is not IPropertySymbol propSymbol)
      return;

    bool hasSectionAttr = propSymbol.GetAttributes().Any(static a =>
      a.AttributeClass?.Name == "SectionAttribute" &&
      a.AttributeClass.ContainingNamespace?.ToDisplayString() == Ns.NesterSettings);

    if (!hasSectionAttr)
      return;

    bool hasSetter = propDecl.AccessorList?.Accessors.Any(
      a => a.IsKind(SyntaxKind.SetAccessorDeclaration) || a.IsKind(SyntaxKind.InitAccessorDeclaration)) == true;

    if (hasSetter)
    {
      context.ReportDiagnostic(Diagnostic.Create(
        Descriptors.SectionPropertyMustBeGetOnly,
        propDecl.Identifier.GetLocation(),
        propSymbol.Name));
    }
  }
}
