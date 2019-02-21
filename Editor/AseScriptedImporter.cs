using System;
using System.Collections.Generic;
using System.Linq;
using AseImport.Importer;
using AseImport.MetaLayer;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

namespace AseImport
{
  [ScriptedImporter(4, new[] {"ase", "aseprite"})]
  public class AseScriptedImporter : ScriptedImporter
  {
    public enum MySpriteAlignment
    {
      Center,
      TopLeft,
      TopCenter,
      TopRight,
      LeftCenter,
      RightCenter,
      BottomLeft,
      BottomCenter,
      BottomRight,
      Custom,
    }

    public class ImportFailedMessage : ScriptableObject
    {
    }

    public int Alignment;
    public Vector2 CustomPivot;
    public bool DenselyPacked;
    public int Border;
    public float PixelsPerUnit;
    public string ChildPath;

    public AseScriptedImporter()
    {
      PixelsPerUnit = 64;
      Border = 0;
      DenselyPacked = true;
      Alignment = (int) SpriteAlignment.Center;
    }

    ImportContext.RuntimeSettings FindSettingsForAsset()
    {
      return new ImportContext.RuntimeSettings(this);
    }

    public override void OnImportAsset(AssetImportContext assetContext)
    {
      var asset = assetContext.assetPath;
      var ctx = new ImportContext(asset, FindSettingsForAsset());

      var images = AtlasGenerator.LoadImages(ctx.BaseName, ctx.AseFile);
      if (images.Count == 0)
      {
        Debug.Log("No Images imported. Generating dummy object.");
        return;
      }

      ImportSpriteSet(assetContext, ctx, images);
    }

    IEnumerable<ISpritePostProcessor> SpritePostProcessors
    {
      get
      {
        return new[]
        {
          new SpriteLayerPivot(),
        };
      }
    }

    IEnumerable<IMetaLayerProcessor> MetaPostProcessors
    {
      get
      {
        var processors = new List<IMetaLayerProcessor>
        {
          new MetaLayerBoxCollider(),
          new MetaLayerEvent(),
          new MetaLayerTransform()
        };
        return processors.OrderBy(e => e.ExecutionOrder).ToList();
      }
    }

    void ImportSpriteSet(AssetImportContext assetContext,
                         ImportContext ctx,
                         List<FrameImage> images)
    {
      // Stage 0: Create the texture atlas and define the sprites
      var packResult = SpritePacker.Pack(ctx.Settings.Border, ctx.Settings.DenselyPacked, images);
      var texture = AtlasGenerator.CreateTexture(packResult);
      var metaList = AtlasGenerator.GenerateAtlasMetaData(ctx, packResult);

      var preview = images.Count > 0 ? AtlasGenerator.CreateTexture(images[0]) : texture;
      assetContext.AddObjectToAsset(ctx.BaseName, texture, preview);
      assetContext.SetMainObject(texture);

      var subImages = ctx.AseFile.SubImages.ToList();
      foreach (var subImage in subImages)
      {
        var metaLayers = subImage.AllLayers.Where(layer => layer.Type == LayerType.Meta).ToList();

        // State 1 - Post process the sprite definitions
        var sprteContext = new SpriteProcessingContext(ctx, metaList);
        foreach (var processor in SpritePostProcessors)
        {
          foreach (var layer in metaLayers.Where(l => l.ActionName == processor.ActionName))
          {
            processor.Process(sprteContext, layer);
          }
        }
      }

      // Stage 2: Actually generate the sprite instances. Generate generic animation templates ..
      var sprites = GenerateSprites(metaList, ctx, texture);
      var animImporter = new AnimationImporter(ChildPath);
      var animations = animImporter.GenerateAnimClips(ctx, subImages, sprites);

      // Stage 3: Post process the animations and add events etc.
      foreach (var subImage in subImages)
      {
        var metaLayers = subImage.AllLayers.Where(layer => layer.Type == LayerType.Meta).ToList();
        var metaContext = new MetaProcessingContext(ctx, animations, sprites);
        foreach (var processor in MetaPostProcessors)
        {
          foreach (var layer in metaLayers.Where(l => l.ActionName == processor.ActionName))
          {
            processor.Process(metaContext, layer);
          }
        }
      }

      // Step 4: Add all generated sprites and animations to the import context.

      foreach (var def in sprites)
      {
        var sprite = def.Sprite;
        assetContext.AddObjectToAsset(sprite.name, sprite, AtlasGenerator.CreateTexture(def.Definition.Frame));
      }

      foreach (var anim in animations)
      {
        var clip = anim.Value;
        var tag = anim.Key;
        assetContext.AddObjectToAsset(tag.Name, clip);
      }
    }

    List<SpriteAndDefinition> GenerateSprites(List<SpriteDefinition> metaList, ImportContext ctx, Texture2D texture)
    {
      return metaList
        .Select(def =>
        {
          var spriteMetaData = def.MetaData;
          Debug.Log("Generating sprite " + spriteMetaData.name + " with " + spriteMetaData.pivot);

          var sprite = Sprite.Create(texture,
                                     spriteMetaData.rect,
                                     spriteMetaData.pivot,
                                     ctx.Settings.PixelsPerUnit,
                                     0,
                                     SpriteMeshType.FullRect,
                                     spriteMetaData.border);
          sprite.name = spriteMetaData.name;
          
          return new SpriteAndDefinition(sprite, def);
        })
        .ToList();
    }
  }
}
