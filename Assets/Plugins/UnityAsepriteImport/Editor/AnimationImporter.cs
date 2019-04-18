using System;
using System.Collections.Generic;
using AseImport.Importer;
using UnityEditor;
using UnityEngine;

namespace AseImport
{
  public class AnimationImporter
  {
    string ChildPath { get; }

    public AnimationImporter(string childPath)
    {
      ChildPath = childPath;
    }

    public Dictionary<FrameTag, AnimationClip> GenerateAnimClips(ImportContext ctx,
                                                                 List<LayerGroup> subImages,
                                                                 List<SpriteAndDefinition> sprites)
    {
      var clips = new Dictionary<FrameTag, AnimationClip>();
      // Generate one animation for each tag
      foreach (var tag in ctx.AseFile.FrameTags)
      {
        var clip = new AnimationClip();
        clip.name = tag.Name;

        // Set loop property: We default to looping, as this is normally what you want.
        var loop = !tag.Properties.Contains("noloop");
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        if (loop)
        {
          clip.wrapMode = WrapMode.Loop;
          settings.loopBlend = true;
          settings.loopTime = true;
        }
        else
        {
          clip.wrapMode = WrapMode.Clamp;
          settings.loopBlend = false;
          settings.loopTime = false;
        }

        AnimationUtility.SetAnimationClipSettings(clip, settings);

        EditorUtility.SetDirty(clip);
        clips.Add(tag, clip);
      }

      // Generate main image
      GenerateClipImageLayer(ctx, clips, subImages, sprites);
      return clips;
    }

    Dictionary<int, Sprite> ProduceSpriteLookup(List<SpriteAndDefinition> sprites, int subImageIndex)
    {
      var retval = new Dictionary<int, Sprite>();
      foreach (var sprite in sprites)
      {
        if (sprite.Definition.Frame.SubImageIndex == subImageIndex)
        {
          retval[sprite.Definition.Frame.FrameId] = sprite.Sprite;
        }
      }

      return retval;
    }

    void GenerateClipImageLayer(ImportContext ctx,
                                Dictionary<FrameTag, AnimationClip> clips,
                                List<LayerGroup> subImages,
                                List<SpriteAndDefinition> sprites)
    {
      for (var subImageIndex = 0; subImageIndex < subImages.Count; subImageIndex++)
      {
        var subImage = subImages[subImageIndex];
        var frameSprites = ProduceSpriteLookup(sprites, subImageIndex);
        foreach (var pair in clips)
        {
          var clip = pair.Value;
          var tag = pair.Key;

          int time = 0;
          var keyFrames = new List<ObjectReferenceKeyframe>();

          for (int frameIndex = tag.From; frameIndex <= tag.To; ++frameIndex)
          {
            var aseFrame = ctx.AseFile[frameIndex];
            Sprite sprite;
            if (frameSprites.TryGetValue(aseFrame.FrameId, out sprite))
            {
              var keyframe = new ObjectReferenceKeyframe
              {
                time = time / 1000f,
                value = sprite
              };
              Debug.Log("Animation: Clip=" + clip.name + " frame=" + sprite.name + " idx=" + aseFrame.FrameId);
              keyFrames.Add(keyframe);
            }

            time += aseFrame.Duration;
          }

          var binding = new EditorCurveBinding
          {
            path = ConcatPath(subImage.ObjectPath),
            type = typeof(SpriteRenderer),
            propertyName = "m_Sprite"
          };

          AnimationUtility.SetObjectReferenceCurve(clip, binding, keyFrames.ToArray());
        }
      }
    }

    string ConcatPath(string path)
    {
      if (string.IsNullOrEmpty(ChildPath))
      {
        return path;
      }

      if (string.IsNullOrEmpty(path))
      {
        return ChildPath;
      }

      return ChildPath + "/" + path;
    }
  }
}
