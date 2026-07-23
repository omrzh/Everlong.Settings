namespace Everlong.Settings.Generators.Analyzers;

/// <summary>
/// 统一将需要partial关键词才可以生成的情况汇聚在这里
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PartialKeywordAnalyzer : DiagnosticAnalyzer
{
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    ImmutableArray.Create(Descriptors.TargetPartial, Descriptors.SectionPropertyMustBePartial);

  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();

    context.RegisterSyntaxNodeAction(AnalyzeNode,
      SyntaxKind.ClassDeclaration,
      SyntaxKind.RecordDeclaration,
      SyntaxKind.PropertyDeclaration);
  }

  private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
  {
    // ELST2004: [Section] property must be partial
    if (context.Node is PropertyDeclarationSyntax propDecl)
    {
      AnalyzePropertyNode(context, propDecl);
      return;
    }

    // ELST0007: type annotated for generation must be partial
    if (context.Node is TypeDeclarationSyntax typeDecl)
      AnalyzeTypeNode(context, typeDecl);
  }

  private static void AnalyzePropertyNode(SyntaxNodeAnalysisContext context, PropertyDeclarationSyntax propDecl)
  {
    if (context.SemanticModel.GetDeclaredSymbol(propDecl) is not IPropertySymbol propSymbol)
      return;

    bool hasSectionAttr = propSymbol.GetAttributes().Any(static a =>
      a.AttributeClass?.Name == "SectionAttribute" &&
      a.AttributeClass.ContainingNamespace?.ToDisplayString() == Ns.NesterSettings);

    if (!hasSectionAttr)
      return;

    if (!propDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
    {
      context.ReportDiagnostic(Diagnostic.Create(
        Descriptors.SectionPropertyMustBePartial,
        propDecl.Identifier.GetLocation(),
        propSymbol.Name));
    }
  }

  private static void AnalyzeTypeNode(SyntaxNodeAnalysisContext context, TypeDeclarationSyntax typeDecl)
  {
    var symbol = context.SemanticModel.GetDeclaredSymbol(typeDecl);
    if (symbol == null)
      return;

    bool requiresPartial = false;

    foreach (var attr in symbol.GetAttributes())
    {
      if (attr.AttributeClass == null)
        continue;

      var attrName = attr.AttributeClass.Name;
      var attrNs = attr.AttributeClass.ContainingNamespace?.ToDisplayString();

      if (string.IsNullOrEmpty(attrNs))
        continue;

      if (attrNs == Ns.NesterNav)
      {
        if (attrName is "GenerateRouteAttribute" or "RouteForAttribute")
        {
          requiresPartial = true;
          break;
        }
      }
      else if (attrNs == Ns.NesterDialogs)
      {
        if (attrName == "DialogSessionAttribute")
        {
          requiresPartial = true;
          break;
        }
      }
      else if (attrNs == Ns.NesterViews)
      {
        if (attrName is "ViewLocatorAttribute" or "WpfViewLocatorAttribute" or "LayoutControlAttribute")
        {
          requiresPartial = true;
          break;
        }
      }
      else if (attrNs == Ns.NesterDi)
      {
        if (attrName is "InjectAttribute" or "ServiceRegistrarAttribute")
        {
          requiresPartial = true;
          break;
        }
      }
      else if (attrNs == Ns.NesterSettings)
      {
        if (attrName is "SettingsAttribute" or "SectionAttribute")
        {
          requiresPartial = true;
          break;
        }
      }
    }

    // Also check members for [Inject]
    if (!requiresPartial)
    {
      foreach (var member in symbol.GetMembers())
      {
        if (member is IPropertySymbol or IFieldSymbol)
        {
          foreach (var attr in member.GetAttributes())
          {
            if (attr.AttributeClass?.Name == "InjectAttribute" &&
                attr.AttributeClass.ContainingNamespace?.ToDisplayString() == Ns.NesterDi)
            {
              requiresPartial = true;
              goto EndCheck;
            }
          }
        }
      }
    }

EndCheck:

    if (requiresPartial)
    {
      if (!typeDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
      {
        context.ReportDiagnostic(Diagnostic.Create(
                                   Descriptors.TargetPartial,
                                   typeDecl.Identifier.GetLocation(),
                                   symbol.Name));
      }

      var current = typeDecl.Parent;
      while (current is TypeDeclarationSyntax parentType)
      {
        if (!parentType.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
        {
          context.ReportDiagnostic(Diagnostic.Create(
                                     Descriptors.TargetPartial,
                                     parentType.Identifier.GetLocation(),
                                     parentType.Identifier.Text));
        }

        current = current.Parent;
      }
    }
  }
}
