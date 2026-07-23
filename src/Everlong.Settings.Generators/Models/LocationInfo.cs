using Microsoft.CodeAnalysis.Text;

namespace Everlong.Settings.Generators.Models;

internal record LocationInfo(string FilePath, TextSpan TextSpan, LinePositionSpan LineSpan)
{
  public Location ToLocation() => Location.Create(FilePath, TextSpan, LineSpan);

  public static LocationInfo? CreateFrom(SyntaxNode node)
      => CreateFrom(node.GetLocation());

  public static LocationInfo? CreateFrom(Location? location)
  {
    if (location is null || location.SourceTree is null)
    {
      return null;
    }

    return new LocationInfo(location.SourceTree.FilePath, location.SourceSpan, location.GetLineSpan().Span);
  }
}

