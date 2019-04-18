using System.Collections.Generic;
using AseImport.Importer;
using UnityEngine;

namespace AseImport.MetaLayer
{
  /// <summary>
  ///  @pivot 
  ///    Uses the weighted center position of all non-empty pixels as pivot point.
  /// </summary>
  public class SpriteLayerPivot : ISpritePostProcessor
  {
    public int ExecutionOrder => 0;
    public string ActionName => "pivot";

    public void Process(SpriteProcessingContext ctx, Layer layer)
    {
      var pivotsByFrameId = ComputePivotPointsForLayer(ctx, layer);
      if (pivotsByFrameId.Count == 0)
      {
        return;
      }

      foreach (var spriteDefinition in ctx.SpriteDefinitions)
      {
        Vector2 pivotPointPx;
        if (!pivotsByFrameId.TryGetValue(spriteDefinition.Frame.FrameId, out pivotPointPx))
        {
          continue;
        }

        // normalise to sprite size 
        var boundingBox = spriteDefinition.MetaData.rect;
        pivotPointPx -= boundingBox.min;
        var pivotPointRel = Vector2.Scale(pivotPointPx, new Vector2(1.0f / boundingBox.width, 1.0f / boundingBox.height));

        spriteDefinition.UpdatePivot(pivotPointRel);
      }
    }

    static Dictionary<int, Vector2> ComputePivotPointsForLayer(SpriteProcessingContext ctx, Layer layer)
    {
      var pivotsByFrameId = new Dictionary<int, Vector2>();
      var file = ctx.ImportContext.AseFile;

      foreach (var frame in file)
      {
        Cel cel;
        if (frame.TryGetCel(layer.Index, out cel))
        {
          Vector2 center;
          if (cel.ComputeWeightedCenter(out center))
          {
            pivotsByFrameId.Add(frame.FrameId, center);
          }
        }
      }

      return pivotsByFrameId;
    }
  }
}
