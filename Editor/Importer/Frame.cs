using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AseImport.Importer
{
  public class Frame
  {
    public readonly int Duration;
    public readonly int FrameId;
    readonly Dictionary<int, Cel> cels;

    public Frame(int frameId, int duration)
    {
      FrameId = frameId;
      Duration = duration;
      cels = new Dictionary<int, Cel>();
    }

    public IEnumerable<Cel> Cels => cels.Values;

    public void AddCel(Cel cel)
    {
      cels.Add(cel.LayerIndex, cel);
    }

    public bool TryGetCel(int layer, out Cel cel)
    {
      return cels.TryGetValue(layer, out cel);
    }
  }
}
