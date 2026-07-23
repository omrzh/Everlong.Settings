using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Everlong.Settings.Generators.Analyzers;

using VerifyPartialAnalyzer = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
  Everlong.Settings.Generators.Analyzers.PartialKeywordAnalyzer,
  Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

using VerifySettingsAnalyzer = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
  Everlong.Settings.Generators.Analyzers.SettingsPropertyAnalyzer,
  Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Everlong.Settings.Generators.Tests.Settings;

/// <summary>Tests for NSTR0007 on [Settings]/[Section] classes, and NSTR2002/2004 on properties.</summary>
public class SettingsDiagnosticsTests
{
  // Minimal stubs — just the attribute declarations the analyzers look for by FQN.
  private const string SettingsAttributeStubs = """

    namespace Everlong.Settings
    {
        public sealed class SettingsAttribute : System.Attribute { }
        public sealed class SectionAttribute : System.Attribute { }
    }
    """;

  // ── NSTR0007: [Settings] class must be partial ───────────────────────

  [Fact]
  public async Task Report_NSTR0007_When_Settings_Class_NotPartial()
  {
    const string test = """
      using Everlong.Settings;

      namespace TestApp
      {
          [Settings]
          public class {|NSTR0007:AppSettings|} {}
      }
      """;

    await VerifyPartialAnalyzer.VerifyAnalyzerAsync(test + SettingsAttributeStubs);
  }

  [Fact]
  public async Task NoReport_When_Settings_Class_IsPartial()
  {
    const string test = """
      using Everlong.Settings;

      namespace TestApp
      {
          [Settings]
          public partial class AppSettings {}
      }
      """;

    await VerifyPartialAnalyzer.VerifyAnalyzerAsync(test + SettingsAttributeStubs);
  }

  // ── NSTR0007: [Section] class must be partial ────────────────────────

  [Fact]
  public async Task Report_NSTR0007_When_Section_Class_NotPartial()
  {
    const string test = """
      using Everlong.Settings;

      namespace TestApp
      {
          [Section]
          public class {|NSTR0007:SoundSettings|} {}
      }
      """;

    await VerifyPartialAnalyzer.VerifyAnalyzerAsync(test + SettingsAttributeStubs);
  }

  [Fact]
  public async Task NoReport_When_Section_Class_IsPartial()
  {
    const string test = """
      using Everlong.Settings;

      namespace TestApp
      {
          [Section]
          public partial class SoundSettings {}
      }
      """;

    await VerifyPartialAnalyzer.VerifyAnalyzerAsync(test + SettingsAttributeStubs);
  }

  // ── NSTR2002: [Section] property must be get-only ────────────────────

  [Fact]
  public async Task Report_NSTR2002_When_Section_Property_HasSetter()
  {
    // Note: 'partial' is omitted here since partial properties (C# 13) aren't needed
    // for testing the setter detection. The analyzer looks for [Section] + setter only.
    const string test = """
      using Everlong.Settings;

      namespace TestApp
      {
          [Section]
          public partial class SoundSettings {}

          [Settings]
          public partial class AppSettings
          {
              [Section]
              public SoundSettings {|NSTR2002:Sound|} { get; set; }
          }
      }
      """;

    await VerifySettingsAnalyzer.VerifyAnalyzerAsync(test + SettingsAttributeStubs);
  }

  [Fact]
  public async Task NoReport_NSTR2002_When_Section_Property_IsGetOnly()
  {
    const string test = """
      using Everlong.Settings;

      namespace TestApp
      {
          [Section]
          public partial class SoundSettings {}

          [Settings]
          public partial class AppSettings
          {
              [Section]
              public SoundSettings Sound { get; }
          }
      }
      """;

    await VerifySettingsAnalyzer.VerifyAnalyzerAsync(test + SettingsAttributeStubs);
  }

  // ── NSTR2004: [Section] property must be partial ─────────────────────

  [Fact]
  public async Task Report_NSTR2004_When_Section_Property_NotPartial()
  {
    const string test = """
      using Everlong.Settings;

      namespace TestApp
      {
          [Section]
          public partial class SoundSettings {}

          [Settings]
          public partial class AppSettings
          {
              [Section]
              public SoundSettings {|NSTR2004:Sound|} { get; }
          }
      }
      """;

    await VerifyPartialAnalyzer.VerifyAnalyzerAsync(test + SettingsAttributeStubs);
  }

  [Fact]
  public async Task NoReport_NSTR2004_When_Property_HasNoSectionAttribute()
  {
    // Guard test: properties without [Section] must never trigger NSTR2004,
    // even when they also lack 'partial'.
    const string test = """
      using Everlong.Settings;

      namespace TestApp
      {
          [Settings]
          public partial class AppSettings
          {
              public int Volume { get; set; }
          }
      }
      """;

    await VerifyPartialAnalyzer.VerifyAnalyzerAsync(test + SettingsAttributeStubs);
  }
}
