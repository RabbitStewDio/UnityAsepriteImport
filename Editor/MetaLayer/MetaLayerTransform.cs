using System.Collections.Generic;
using AseImport.Importer;
using UnityEditor;
using UnityEngine;

namespace AseImport.MetaLayer
{
  /// <summary>
  ///  @transform("EffectPosition") layer controls the position of child object named EffectPosition.
  /// </summary>
  public class MetaLayerTransform : IMetaLayerProcessor
  {
    public int ExecutionOrder => 0;
    public string ActionName => "transform";

    public void Process(MetaProcessingContext ctx, Layer layer)
    {
      var childName = layer.GetParamString(0);
      var bindingX = new EditorCurveBinding {path = childName, type = typeof(Transform), propertyName = "m_LocalPosition.x"};
      var bindingY = new EditorCurveBinding {path = childName, type = typeof(Transform), propertyName = "m_LocalPosition.y"};

      var framePositionByIndex = ComputeCenterPoints(ctx, layer);

      var file = ctx.ImportContext.AseFile;
      foreach (var frameTag in file.FrameTags)
      {
        AnimationClip clip;
        if (!ctx.TryGetClip(frameTag, out clip))
        {
          continue;
        }

        var curveX = new AnimationCurve();
        var curveY = new AnimationCurve();

        float t = 0;
        for (var frameIndex = frameTag.From; frameIndex <= frameTag.To; frameIndex += 1)
        {
          Vector2 pos;
          if (framePositionByIndex.TryGetValue(frameIndex, out pos))
          {
            curveX.AddKey(t, pos.x);
            curveY.AddKey(t, pos.y);
          }

          // Duration is given in milliseconds, but Unity wants seconds ..
          t += file[frameIndex].Duration / 1000f;
        }

        if (curveX.length > 0)
        {
          MakeConstant(curveX);
          MakeConstant(curveY);

          AnimationUtility.SetEditorCurve(clip, bindingX, curveX);
          AnimationUtility.SetEditorCurve(clip, bindingY, curveY);

          EditorUtility.SetDirty(clip);
        }
      }
    }

    static Dictionary<int, Vector2> ComputeCenterPoints(MetaProcessingContext ctx, Layer layer)
    {
      var frames = new Dictionary<int, Vector2>();
      var file = ctx.ImportContext.AseFile;

      foreach (var frame in file)
      {

        Cel cel;
        frame.TryGetCel(layer.Index, out cel);

        if (cel == null)
        {
          continue;
        }

        Vector2 center;
        if (cel.ComputeWeightedCenter(out center))
        {
          var pivot = Vector2.Scale(ctx.ImportContext.Settings.PivotRelativePos, new Vector2(file.Width, file.Height));
          var posWorld = (center - pivot) / ctx.ImportContext.Settings.PixelsPerUnit;
          frames.Add(frame.FrameId, posWorld);
        }
      }

      return frames;
    }

    static void MakeConstant(AnimationCurve curve)
    {
      for (var i = 0; i < curve.length; ++i)
      {
        AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
      }
    }
  }
}
