using System;
using UnityEngine;

namespace AseImport
{
  public class FrameImage
  {
    public string BaseName { get; }
    public int SubImageIndex { get; }
    public int FrameId { get; }
    int minx;
    int miny;
    int maxx;
    int maxy;

    public IntRect BoundingBox
    {
      get
      {
        if (maxx == int.MinValue)
        {
          return new IntRect();
        }

        return new IntRect(minx, miny, maxx - minx + 1, maxy - miny + 1);
      }
    }

    /// <summary>
    ///  This is the same as the file's width shared across all frames.
    /// </summary>
    public readonly int Width;

    /// <summary>
    ///  This is the same as the file's height shared across all frames.
    /// </summary>
    public readonly int Height;

    readonly Color[] data;

    public FrameImage(int frameId, 
                      string baseName, 
                      int subImageIndex, 
                      int width, 
                      int height)
    {
      FrameId = frameId;
      BaseName = baseName;
      SubImageIndex = subImageIndex;
      this.Width = width;
      minx = int.MaxValue;
      miny = int.MaxValue;
      maxx = int.MinValue;
      maxy = int.MinValue;
      this.Height = height;
      data = new Color[this.Width * this.Height];
      for (var i = 0; i < data.Length; ++i)
      {
        data[i].a = 0;
      }
    }

    public Color this[int x, int y]
    {
      get
      {
        if (x < 0) throw new ArgumentOutOfRangeException(nameof(x), x, "cannot be negative");
        if (x >= Width) throw new ArgumentOutOfRangeException(nameof(x), x, "cannot be larger than width");
        if (y < 0) throw new ArgumentOutOfRangeException(nameof(y), y, "cannot be negative");
        if (y >= Height) throw new ArgumentOutOfRangeException(nameof(y), y, "cannot be larger than height");

        return data[y * Width + x];
      }
      set
      {
        if (x < 0) throw new ArgumentOutOfRangeException(nameof(x), x, "cannot be negative");
        if (x >= Width) throw new ArgumentOutOfRangeException(nameof(x), x, "cannot be larger than width");
        if (y < 0) throw new ArgumentOutOfRangeException(nameof(y), y, "cannot be negative");
        if (y >= Height) throw new ArgumentOutOfRangeException(nameof(y), y, "cannot be larger than height");

        data[y * Width + x] = value;
        if (value.a > 0)
        {
          // expand image area
          minx = Math.Min(minx, x);
          miny = Math.Min(miny, y);
          maxx = Math.Max(maxx, x);
          maxy = Math.Max(maxy, y);
        }
      }
    }
  }
}
