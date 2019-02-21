using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace AseImport.MetaLayer
{
  public class SpriteProcessingContext
  {
    public ImportContext ImportContext { get; }
    public IReadOnlyList<SpriteDefinition> SpriteDefinitions { get; }

    public SpriteProcessingContext(ImportContext importContext,
                                   [NotNull] IReadOnlyList<SpriteDefinition> spriteDefinitions)
    {
      if (importContext == null)
      {
        throw new ArgumentNullException(nameof(importContext));
      }
      
      if (spriteDefinitions == null)
      {
        throw new ArgumentNullException(nameof(spriteDefinitions));
      }


      ImportContext = importContext;
      SpriteDefinitions = spriteDefinitions;
    }
  }
}