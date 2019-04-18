using System.Collections.Generic;

namespace AseImport.Importer
{
  public enum LoopMode
  {
    Forward = 0,
    Reverse = 1,
    PingPong = 2
  }

  public class FrameTag
  {
    public readonly int From;
    public readonly int To;
    public readonly LoopMode LoopMode;
    public readonly string Name;
    public readonly HashSet<string> Properties;

    public FrameTag(string name, int from, int to, LoopMode loopMode, IEnumerable<string> properties)
    {
      this.Properties = new HashSet<string>(properties);
      this.Name = name;
      this.From = from;
      this.To = to;
      this.LoopMode = loopMode;
    }
  }
}