using System;
using System.Collections.Generic;
using AseImport.Importer;
using JetBrains.Annotations;
using UnityEngine;

namespace AseImport.MetaLayer
{
  public class MetaProcessingContext
  {
    readonly Dictionary<FrameTag, AnimationClip> animationClips;

    public ImportContext ImportContext { get; }
    public IReadOnlyList<SpriteAndDefinition> SpriteDefinitions { get; }
    public IEnumerable<FrameTag> FrameTags => ImportContext.AseFile.FrameTags;

    public MetaProcessingContext(ImportContext importContext,
                                 [NotNull] Dictionary<FrameTag, AnimationClip> animationClips,
                                 [NotNull] IReadOnlyList<SpriteAndDefinition> spriteDefinitions)
    {
      if (importContext == null)
      {
        throw new ArgumentNullException(nameof(importContext));
      }

      if (animationClips == null)
      {
        throw new ArgumentNullException(nameof(animationClips));
      }

      if (spriteDefinitions == null)
      {
        throw new ArgumentNullException(nameof(spriteDefinitions));
      }

      this.animationClips = animationClips;

      ImportContext = importContext;
      SpriteDefinitions = spriteDefinitions;
    }

    public bool TryGetClip(FrameTag tag, out AnimationClip clip)
    {
      return animationClips.TryGetValue(tag, out clip);
    }
  }
}