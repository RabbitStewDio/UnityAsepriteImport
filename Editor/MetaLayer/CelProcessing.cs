using System;
using AseImport.Importer;
using UnityEngine;

namespace AseImport.MetaLayer
{
  public static class CelProcessing
  {
    public static IntRect GetBoundingBox(this Cel cel)
    {
      var minx = int.MaxValue;
      var miny = int.MaxValue;
      var maxx = int.MinValue;
      var maxy = int.MinValue;

      for (var y = 0; y < cel.Height; ++y)
      {
        for (var x = 0; x < cel.Width; ++x)
        {
          var texX = cel.X + x;
          var texY = cel.Y + y;
          var col = cel.GetPixel(texX, texY);
          if (col.a > 0f)
          {
            minx = Mathf.Min(minx, texX);
            miny = Mathf.Min(miny, texY);
            maxx = Mathf.Max(maxx, texX);
            maxy = Mathf.Max(maxy, texY);
          }
        }
      }

      if (maxx == int.MinValue)
      {
        return new IntRect();
      }
      /*
      var texCenter = new Vector2((maxx + minx) / 2.0f, (maxy + miny) / 2.0f);
      var texSize = new Vector2(maxx - minx, maxy - miny);

      var pivotInFrame = Vector2.Scale(ctx.ImportContext.Settings.PivotRelativePos, new Vector2(file.Width, file.Height));
      var posWorld = (texCenter - pivotInFrame) / ctx.ImportContext.Settings.PixelsPerUnit;
      var sizeWorld = texSize / ctx.ImportContext.Settings.PixelsPerUnit;

      return new Rect(posWorld, sizeWorld);
      */
      return new IntRect(minx, miny, maxx - minx, maxy - miny);
    }

    public static bool ComputeWeightedCenter(this Cel cel, out Vector2 v)
    {
      var center = Vector2.zero;
      var pixelCount = 0;

      for (var y = 0; y < cel.Height; ++y)
      {
        for (var x = 0; x < cel.Width; ++x)
        {
          // tex coords relative to full texture boundaries
          var texX = cel.X + x;
          var texY = cel.Y + y;

          var col = cel.GetPixel(x, y);
          if (col.a > 0f)
          {
            center += new Vector2(texX, texY);
            pixelCount += 1;
          }
        }
      }

      if (pixelCount > 0)
      {
        center /= pixelCount;
        v = center;
        return true;
      }

      v = default(Vector2);
      return false;
    }
  }
}
