using System;
using UnityEngine;

namespace AseImport
{
  public class SpriteAndDefinition
  {
    public Sprite Sprite { get; }
    public SpriteDefinition Definition { get; }

    public SpriteAndDefinition(Sprite sprite, SpriteDefinition definition)
    {
      if (sprite == null)
      {
        throw new ArgumentNullException(nameof(sprite));
      }

      if (definition == null)
      {
        throw new ArgumentNullException(nameof(definition));
      }

      Sprite = sprite;
      Definition = definition;
    }
  }
}