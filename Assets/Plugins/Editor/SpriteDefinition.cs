using System;
using UnityEditor;
using UnityEngine;

namespace AseImport
{
  public class SpriteDefinition
  {
    public SpriteMetaData MetaData { get; private set; }
    public readonly FrameImage Frame;

    public SpriteDefinition(SpriteMetaData metaData, FrameImage frame)
    {
      if (frame == null)
      {
        throw new ArgumentNullException(nameof(frame));
      }

      MetaData = metaData;
      Frame = frame;
    }

    public void UpdatePivot(Vector2 v)
    {
      var md = MetaData;
      md.pivot = v;
      MetaData = md;
    }
  }
}