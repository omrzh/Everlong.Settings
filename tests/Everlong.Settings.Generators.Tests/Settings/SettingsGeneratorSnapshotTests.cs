using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Everlong.Settings.Generators;
using Everlong.Settings;
using VerifyXunit;

namespace Everlong.Settings.Generators.Tests.Settings;

public class SettingsGeneratorSnapshotTests
{
  [Fact]
  public Task Generates_SettingsGroup_WithBoolSetting()
  {
    const string source = """
      using Everlong.Settings;

      namespace TestApp
      {
          [Settings]
          public partial class AppSettings
          {
              [Setting(false)]
              public partial bool IsDarkMode { get; set; }
          }
      }
      """;
    return Verify(source);
  }

  [Fact]
  public Task Generates_SettingsGroup_WithStringSetting()
  {
    const string source = """
      using Everlong.Settings;

      namespace TestApp
      {
          [Settings]
          public partial class AppSettings
          {
              [Setting("light")]
              public partial string Theme { get; set; }
          }
      }
      """;
    return Verify(source);
  }

  [Fact]
  public Task Generates_SettingsGroup_WithNullableStringSetting()
  {
    const string source = """
      using Everlong.Settings;

      namespace TestApp
      {
          [Settings]
          public partial class AppSettings
          {
              [Setting]
              public partial string? Username { get; set; }
          }
      }
      """;
    return Verify(source);
  }

  [Fact]
  public Task Generates_SettingsSection_Standalone()
  {
    const string source = """
      using Everlong.Settings;

      namespace TestApp
      {
          [Section]
          public partial class SoundSettings
          {
              [Setting(50)]
              public partial int Volume { get; set; }
          }
      }
      """;
    return Verify(source);
  }

  [Fact]
  public Task Generates_SettingsGroup_WithSection()
  {
    const string source = """
      using Everlong.Settings;

      namespace TestApp
      {
          [Section]
          public partial class SoundSettings
          {
              [Setting(50)]
              public partial int Volume { get; set; }
          }

          [Settings]
          public partial class AppSettings
          {
              [Setting(false)]
              public partial bool IsDarkMode { get; set; }

              [Section]
              public partial SoundSettings Sound { get; }
          }
      }
      """;
    return Verify(source);
  }

  [Fact]
  public Task Generates_ComplexSetting_Nullable()
  {
    const string source = """
      using Everlong.Settings;

      namespace TestApp
      {
          [Settings]
          public partial class AppSettings
          {
              [Setting]
              public partial string[]? RecentFiles { get; set; }
          }
      }
      """;
    return Verify(source);
  }

  [Fact]
  public Task Generates_ComplexSetting_NonNullable()
  {
    const string source = """
      using Everlong.Settings;

      namespace TestApp
      {
          [Settings]
          public partial class AppSettings
          {
              [Setting]
              public partial string[] Tags { get; set; }
          }
      }
      """;
    return Verify(source);
  }

  [Fact]
  public Task Generates_Setting_WithRange()
  {
    const string source = """
      using Everlong.Settings;
      using System.ComponentModel.DataAnnotations;

      namespace TestApp
      {
          [Settings]
          public partial class AppSettings
          {
              [Setting(50)]
              [Range(0, 100)]
              public partial int Volume { get; set; }
          }
      }
      """;
    return Verify(source);
  }

  [Fact]
  public Task Generates_Setting_WithCoercion()
  {
    const string source = """
      using Everlong.Settings;

      namespace TestApp
      {
          [Settings]
          public partial class AppSettings
          {
              [Coercion]
              [Setting(14)]
              public partial int FontSize { get; set; }

              private static partial int CoerceFontSize(int value) => value < 8 ? 8 : value;
          }
      }
      """;
    return Verify(source);
  }

  [Fact]
  public Task Generates_Setting_WithCoercionAndRange()
  {
    const string source = """
      using Everlong.Settings;
      using System.ComponentModel.DataAnnotations;

      namespace TestApp
      {
          [Settings]
          public partial class AppSettings
          {
              [Coercion]
              [Setting(14), Range(10, 24)]
              public partial int FontSize { get; set; }

              private static partial int CoerceFontSize(int value) => value;
          }
      }
      """;
    return Verify(source);
  }

  [Fact]
  public Task Generates_ComplexSetting_WithCoercion()
  {
    const string source = """
      using Everlong.Settings;

      namespace TestApp
      {
          [Settings]
          public partial class AppSettings
          {
              [Coercion]
              [Setting]
              public partial string[] Tags { get; set; }

              private static partial string[] CoerceTags(string[] value) => value;
              private static partial string[] GetTagsDefault() => [];
          }
      }
      """;
    return Verify(source);
  }

  [Fact]
  public Task Generates_Setting_WithMaxLength()
  {
    const string source = """
      using Everlong.Settings;
      using System.ComponentModel.DataAnnotations;

      namespace TestApp
      {
          [Settings]
          public partial class AppSettings
          {
              [Setting("")]
              [MaxLength(100)]
              public partial string Username { get; set; }
          }
      }
      """;
    return Verify(source);
  }

  [Fact]
  public Task Generates_Setting_EnumFallback()
  {
    const string source = """
      using Everlong.Settings;

      namespace TestApp
      {
          public enum Theme { Light, Dark }

          [Settings]
          public partial class AppSettings
          {
              [Setting(Theme.Light)]
              public partial Theme ColorTheme { get; set; }
          }
      }
      """;
    return Verify(source);
  }

  [Fact]
  public Task Generates_Snapshot_Method()
  {
    const string source = """
      using Everlong.Settings;

      namespace TestApp
      {
          [Section]
          public partial class SoundSettings
          {
              [Setting(50)]
              public partial int Volume { get; set; }
          }

          [Settings]
          public partial class AppSettings
          {
              [Setting(false)]
              public partial bool IsDarkMode { get; set; }

              [Section]
              public partial SoundSettings Sound { get; }
          }
      }
      """;
    return Verify(source);
  }

  private static Task Verify(string source)
  {
    var syntaxTree = CSharpSyntaxTree.ParseText(source);

    var references = AppDomain.CurrentDomain.GetAssemblies()
      .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
      .Select(a => MetadataReference.CreateFromFile(a.Location))
      .Distinct()
      .ToList();

    references.Add(MetadataReference.CreateFromFile(typeof(SettingsAttribute).Assembly.Location));

    // Include DataAnnotations so [Range] attributes can be resolved
    references.Add(MetadataReference.CreateFromFile(
      typeof(System.ComponentModel.DataAnnotations.RangeAttribute).Assembly.Location));

    var compilation = CSharpCompilation.Create(
      "TestApp",
      [syntaxTree],
      references,
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    var generator = new SettingsGenerator();
    GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
    driver = driver.RunGenerators(compilation);

    var settings = new VerifySettings();
    settings.UseDirectory("Verified");
    return Verifier.Verify(driver, settings);
  }
}
