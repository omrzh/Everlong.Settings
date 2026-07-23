using System.Runtime.CompilerServices;
using VerifyTests;

namespace Everlong.Settings.CodeFixers.Tests;

public static class ModuleInitializer
{
  [ModuleInitializer]
  public static void Init()
  {
    VerifySourceGenerators.Initialize();
  }
}
