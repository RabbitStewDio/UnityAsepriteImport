using System;
using System.Collections.Generic;
using System.Linq;
using AseImport.Importer;
using UnityEditor;
using UnityEngine;

namespace AseImport
{
  public static class AtlasGenerator
  {
    static List<FrameImage> LoadFrameImage(string baseName,
                                           AseFile file,
                                           Frame frame)
    {
      var cels = frame.Cels.OrderBy(it => it.LayerIndex).ToList();

      var subImages = file.SubImages.ToList();
      var images = new List<FrameImage>();
      for (var subImageIndex = 0; subImageIndex < subImages.Count; subImageIndex++)
      {
        var subImage = subImages[subImageIndex];
        var spriteName = string.IsNullOrEmpty(subImage.ImageName) ? baseName : subImage.ImageName;
        var image = new FrameImage(frame.FrameId, spriteName, subImageIndex, file.Width, file.Height);

        foreach (var cel in cels)
        {
          var layer = subImage.FindLayer(cel.LayerIndex);
          if (layer == null || layer.Type != LayerType.Content)
          {
            continue;
          }

          Debug.Log($"Cel size: {cel.X}, {cel.Y}, {cel.Width}, {cel.Height}");

          // Cels can have data outside of the visible frame area.
          // As we are manipulating image data, not cel data, this
          // can cause problems if we dont check bounds carefully.
          var maxX = Math.Min(image.Width - cel.X, cel.Width);
          var maxY = Math.Min(image.Height - cel.Y, cel.Height);
          var minX = Math.Max(0, -cel.X);
          var minY = Math.Max(0, -cel.Y);

          Debug.Log($"Cel processing: {minX}, {minY}, {maxX}, {maxY}");

          for (int cy = minY; cy < maxY; ++cy)
          {
            for (int cx = minX; cx < maxX; ++cx)
            {
              var x = cx + cel.X;
              var y = cy + cel.Y;
              var layerColor = cel.GetPixel(x, y);
              if (layerColor.a > 0f)
              {
                image[x, y] = AlphaBlend(image[x, y], layerColor);
              }
            }
          }
        }

        images.Add(image);
      }

      return images;
    }

    static Color AlphaBlend(Color baseColor, Color layerColor)
    {
      var color = Color.Lerp(baseColor, layerColor, layerColor.a);
      color.a = baseColor.a + layerColor.a * (1 - baseColor.a);
      color.r /= color.a;
      color.g /= color.a;
      color.b /= color.a;
      return color;
    }

    public static List<FrameImage> LoadImages(string baseName, AseFile file)
    {
      var retval = new List<FrameImage>();
      foreach (var frame in file)
      {
        var loadFrameImage = LoadFrameImage(baseName, file, frame);
        retval.AddRange(loadFrameImage);
      }

      return retval;
    }

    static Texture2D CreateEmptyTexture(int imageSize)
    {
      if (imageSize <= 0)
      {
        throw new ArgumentException("must be positive number", nameof(imageSize));
      }

      var texture = new Texture2D(imageSize, imageSize);
      texture.filterMode = FilterMode.Point;
      texture.wrapMode = TextureWrapMode.Clamp;
      texture.SetPixels(new Color[texture.width * texture.height]);
      return texture;
    }

    public static Texture2D CreateTexture(FrameImage image)
    {
      var boundingBox = image.BoundingBox;
      var imageSize = Math.Max(1, Math.Max(boundingBox.Width, boundingBox.Height));
      var textureSize = (int) Math.Pow(2, Math.Ceiling(Math.Log(imageSize, 2)));
      var texture = CreateEmptyTexture(textureSize);

      for (var y = boundingBox.MinY; y < boundingBox.MaxY; ++y)
      {
        for (var x = boundingBox.MinX; x < boundingBox.MaxX; ++x)
        {
          var texX = (x - boundingBox.MinX);
          var texY = -(y - boundingBox.MinY) + boundingBox.Height - 1;
          texture.SetPixel(texX, texY, image[x, y]);
        }
      }

      texture.Apply();
      return texture;
    }

    public static Texture2D CreateTexture(SpritePacker.PackResult packResult)
    {
      var texture = CreateEmptyTexture(packResult.AtlasTextureSize);
      foreach (var pos in packResult.Positions)
      {
        var image = pos.FrameImage;
        var bb = image.BoundingBox;

        for (var y = bb.MinY; y < bb.MaxY; ++y)
        {
          for (var x = bb.MinX; x < bb.MaxX; ++x)
          {
            var texX = (x - bb.MinX) + pos.X;
            var texY = -(y - bb.MinY) + pos.Y + bb.Height - 1;
            texture.SetPixel(texX, texY, image[x, y]);
          }
        }
      }

      texture.Apply();
      return texture;
    }

    public static List<SpriteDefinition> GenerateAtlasMetaData(ImportContext ctx,
                                                               SpritePacker.PackResult packResult)
    {
      var oldPivotNorm = ctx.Settings.PivotRelativePos;
      var metaList = new List<SpriteDefinition>();

      for (int i = 0; i < packResult.Positions.Count; ++i)
      {
        var pos = packResult.Positions[i];
        var image = pos.FrameImage;
        var name = pos.FrameImage.BaseName + "_" + pos.FrameImage.FrameId + "_" + pos.FrameImage.SubImageIndex;
        var bounds = new Rect(pos.X, pos.Y, pos.Width, pos.Height);
        var md = GenerateSpriteMetaData(image, name, bounds, oldPivotNorm);
        metaList.Add(new SpriteDefinition(md, image));
      }

      return metaList;
    }

    public static SpriteMetaData GenerateSpriteMetaData(FrameImage image,
                                                        string name,
                                                        Rect rect,
                                                        Vector2 normalisedPivotPoint)
    {
      var metadata = new SpriteMetaData();
      metadata.name = name;
      metadata.alignment = (int) SpriteAlignment.Custom;
      metadata.rect = rect;

      // calculate relative pivot
      var pivotPointInFrame = Vector2.Scale(normalisedPivotPoint, new Vector2(image.Width, image.Height));
      var newPivotTex = pivotPointInFrame - new Vector2(image.BoundingBox.MinX, image.Height - image.BoundingBox.MaxY - 1);
      var newPivotNorm = Vector2.Scale(newPivotTex, new Vector2(1.0f / rect.width, 1.0f / rect.height));
      metadata.pivot = newPivotNorm;

      Debug.Log("For Sprite " + name + " using pivot point at pixel " + pivotPointInFrame + " normalised to " + newPivotNorm + " (with bb: " + image.BoundingBox + ")");
      return metadata;
    }
  }
}
