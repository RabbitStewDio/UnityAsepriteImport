using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AseImport
{
  public static class SpritePacker
  {
    struct PackData
    {
      public readonly int Index;
      public readonly int Width;
      public readonly int Height;
      public FrameImage FrameImage { get; }

      PackData(int index, int width, int height, FrameImage frameImage)
      {
        this.Index = index;
        this.Width = width;
        this.Height = height;
        FrameImage = frameImage;
      }

      public static Func<FrameImage, int, PackData> Factory(bool dense)
      {
        if (dense)
        {
          return AsDenselyPacked;
        }

        return AsLooselyPacked;
      }

      public static PackData AsDenselyPacked(FrameImage img, int index)
      {
        return new PackData(index, img.BoundingBox.Width, img.BoundingBox.Height, img);
      }

      public static PackData AsLooselyPacked(FrameImage img, int index)
      {
        return new PackData(index, img.Width, img.Height, img);
      }
    }

    public struct PackPos
    {
      public FrameImage FrameImage { get; }
      public int Width { get; }
      public int Height { get; }
      public int Index { get; }
      public int X { get; }
      public int Y { get; }

      public PackPos(int index, int x, int y, int width, int height, FrameImage frameImage)
      {
        Width = width;
        Height = height;
        FrameImage = frameImage;
        this.Index = index;
        this.X = x;
        this.Y = y;
      }
    }

    public class PackResult
    {
      public readonly int AtlasTextureSize;
      public readonly IReadOnlyList<PackPos> Positions;

      public PackResult(int atlasTextureSize, IReadOnlyList<PackPos> positions)
      {
        this.AtlasTextureSize = atlasTextureSize;
        this.Positions = positions;
      }
    }

    public static PackResult Pack(int borderSize,
                                  bool dense,
                                  IEnumerable<FrameImage> images)
    {
      // all the data, sorted by item height for maximum storage efficiency when using a 
      // shelf-stacking sprite packer.
      var selector = PackData.Factory(dense);
      var raw = images
        .Select(selector)
        .ToList();
      var packList = raw
        .OrderBy(t => t, new SortOrder())
        .ToList();

      var packResult = PackAtlas(packList, borderSize);

      if (packResult.AtlasTextureSize > 2048)
      {
        Debug.LogWarning("Generate atlas size is larger than 2048, this might force Unity to compress the image.");
      }

      foreach (var p in packResult.Positions)
      {
        Debug.Log($"{p.FrameImage.BaseName}_{p.FrameImage.FrameId}_{p.FrameImage.SubImageIndex} = ({p.X}, {p.Y}) ({p.Width}, {p.Height})" );
      }

      return packResult;
    }

    /// Pack the atlas
    static PackResult PackAtlas(List<PackData> list, int border)
    {
      var size = 16;
      while (true)
      {
        var result = DoPackAtlas(list, size, border);
        if (result != null)
        {
          return result;
        }

        size *= 2;
      }
    }

    static PackResult DoPackAtlas(List<PackData> list, int size, int border)
    {
      // Pack using the most simple shelf algorithm
      var posList = new List<PackPos>();

      // x: the position after last rect; y: the baseline height of current shelf
      // axis: x left -> right, y bottom -> top
      var x = 0;
      var y = 0;
      var shelfHeight = 0;

      foreach (var data in list)
      {
        if (data.Width >= size)
        {
          return null;
        }

        if (x + data.Width + border > size)
        {
          // create a new shelf
          y += shelfHeight;
          x = 0;
          shelfHeight = data.Height + border;
        }
        else if (data.Height + border > shelfHeight)
        {
          // increase shelf height
          shelfHeight = data.Height + border;
        }

        if (y + shelfHeight >= size)
        {
          // can't place this anymore
          return null;
        }

        posList.Add(new PackPos(data.Index, x, y, data.Width, data.Height, data.FrameImage));

        x += data.Width + border;
      }

      //var sorted = posList.OrderBy(e => e.Index).ToList();
      return new PackResult(size, posList);
    }

    class SortOrder : IComparer<PackData>
    {
      public int Compare(PackData x, PackData y)
      {
        var result = x.Height.CompareTo(y.Height);
        if (result != 0)
        {
          return result;
        }

        return x.Index.CompareTo(y.Index);
      }
    }
  }
}
