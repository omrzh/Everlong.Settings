using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Everlong.Settings.CodeFixers;
using Everlong.Settings.Generators.Analyzers;

using VerifyGetOnlyFix = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
  Everlong.Settings.Generators.Analyzers.SettingsPropertyAnalyzer,
  Everlong.Settings.CodeFixers.SectionPropertyGetOnlyCodeFixProvider,
  Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

using VerifyPartialFix = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
  Everlong.Settings.Generators.Analyzers.PartialKeywordAnalyzer,
  Everlong.Settings.CodeFixers.PartialKeywordCodeFixProvider,
  Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Everlong.Settings.CodeFixers.Tests;

public class SettingsCodeFixTests
{
  private const string SettingsAttributeStubs = """

    namespace Everlong.Settings
    {
        public sealed class SettingsAttribute : System.Attribute { }
        public sealed class SectionAttribute : System.Attribute { }
    }
    """;

  // Required for `init` accessor in Roslyn netstandard2.0 test host
  private const string IsExternalInitStub = """

    namespace System.Runtime.CompilerServices
    {
        internal static class IsExternalInit { }
    }
    """;

  // ── NSTR2002: Remove setter from [Section] property ──────────────────

  [Fact]
  public async Task Fix_NSTR2002_RemovesSetter()
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
              public SoundSettings {|NSTR2002:Sound|} { get; set; }
          }
      }
      """;

    const string fixedCode = """
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

    await VerifyGetOnlyFix.VerifyCodeFixAsync(
      test + SettingsAttributeStubs,
      fixedCode + SettingsAttributeStubs);
  }

  [Fact]
  public async Task Fix_NSTR2002_RemovesInitSetter()
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
              public SoundSettings {|NSTR2002:Sound|} { get; init; }
          }
      }
      """;

    const string fixedCode = """
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

    await VerifyGetOnlyFix.VerifyCodeFixAsync(
      test + SettingsAttributeStubs + IsExternalInitStub,
      fixedCode + SettingsAttributeStubs + IsExternalInitStub);
  }

  // ── NSTR2004: Make [Section] property partial ─────────────────────────
  // Note: the fixed code uses partial properties (C# 13) which Roslyn 4.8.0 cannot
  // compile. We use CSharpCodeFixTest directly with CompilerDiagnostics.None to
  // verify the fix produces the correct source without checking compilation.

  [Fact]
  public async Task Fix_NSTR2004_AddsPartialToProperty()
  {
    const string testCode = """
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
      """ + SettingsAttributeStubs;

    const string fixedCode = """
      using Everlong.Settings;

      namespace TestApp
      {
          [Section]
          public partial class SoundSettings {}

          [Settings]
          public partial class AppSettings
          {
              [Section]
              public partial SoundSettings Sound { get; }
          }
      }
      """ + SettingsAttributeStubs;

    var test = new CSharpCodeFixTest<PartialKeywordAnalyzer, PartialKeywordCodeFixProvider, DefaultVerifier>
    {
      TestCode = testCode,
      FixedCode = fixedCode,
      CompilerDiagnostics = CompilerDiagnostics.None,
    };

    await test.RunAsync();
  }
}
