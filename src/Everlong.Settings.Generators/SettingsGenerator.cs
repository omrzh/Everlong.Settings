using Everlong.Settings.Generators.Extensions;

namespace Everlong.Settings.Generators;

[Generator(LanguageNames.CSharp)]
public sealed partial class SettingsGenerator : IIncrementalGenerator
{
  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    // Pipeline 1: [Settings] classes → ISettingsRoot
    var groups = context.SyntaxProvider.ForAttributeWithMetadataName<Result<SettingsClassInfo?>>(
      Attributes.SettingsFull,
      predicate: PredicateHelper.IsPartialClassDecl,
      transform: static (ctx, token) => TransformSettingsClass(ctx, token, isRoot: true));

    context.ReportDiagnostics(groups.SelectMany(static (item, _) => item.Errors));

    var validGroups = groups
      .Where(static item => item.Value is not null)
      .Select(static (item, _) => item.Value!);

    context.RegisterSourceOutput(validGroups, WrappedExecute);

    // Pipeline 2: [Section] classes → ISettingsSection
    var sections = context.SyntaxProvider.ForAttributeWithMetadataName<Result<SettingsClassInfo?>>(
      Attributes.SectionFull,
      predicate: PredicateHelper.IsPartialClassDecl,
      transform: static (ctx, token) => TransformSettingsClass(ctx, token, isRoot: false));

    context.ReportDiagnostics(sections.SelectMany(static (item, _) => item.Errors));

    var validSections = sections
      .Where(static item => item.Value is not null)
      .Select(static (item, _) => item.Value!);

    context.RegisterSourceOutput(validSections, WrappedExecute);
  }


}
