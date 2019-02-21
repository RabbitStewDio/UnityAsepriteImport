using System;
using System.Collections.Generic;
using AseImport.Importer;
using UnityEditor;
using UnityEngine;
using TangentMode = UnityEditor.AnimationUtility.TangentMode;

namespace AseImport.MetaLayer
{
  public class MetaLayerBoxCollider : IMetaLayerProcessor
  {
    public int ExecutionOrder => 0;
    public string ActionName => "boxCollider";


    public void Process(MetaProcessingContext ctx, Layer layer)
    {
      var path = layer.GetParamString(0);
      var bindingOffX = Binding(path, typeof(BoxCollider2D), "m_Offset.x");
      var bindingOffY = Binding(path, typeof(BoxCollider2D), "m_Offset.y");
      var bindingSizeX = Binding(path, typeof(BoxCollider2D), "m_Size.x");
      var bindingSizeY = Binding(path, typeof(BoxCollider2D), "m_Size.y");
      var bindingEnable = Binding(path, typeof(BoxCollider2D), "m_Enabled");

      LayerParam changeEnableParam;
      var changeEnable = layer.TryGetParam(1, LayerParamType.Bool, out changeEnableParam) && changeEnableParam.BoolValue;

      var file = ctx.ImportContext.AseFile;
      var frameRects = ComputeFrameBoundingBoxes(layer, file);

      foreach (var frameTag in file.FrameTags)
      {
        AnimationClip clip;
        if (!ctx.TryGetClip(frameTag, out clip))
        {
          continue;
        }

        var curveOffX = new AnimationCurve();
        var curveOffY = new AnimationCurve();
        var curveSizeX = new AnimationCurve();
        var curveSizeY = new AnimationCurve();
        var curveEnable = new AnimationCurve();

        float t = 0;
        var hasEnable = false;
        for (var frameIndex = frameTag.From; frameIndex <= frameTag.To; ++frameIndex)
        {
          var rect = frameRects[frameIndex];
          var enable = rect.Size != Vector2.zero;
          curveEnable.AddKey(new Keyframe(t, enable ? 1 : 0));
          if (enable)
          {
            hasEnable = true;
            curveOffX.AddKey(t, rect.Center.x);
            curveOffY.AddKey(t, rect.Center.y);
            curveSizeX.AddKey(t, rect.Size.x);
            curveSizeY.AddKey(t, rect.Size.y);
          }

          t += file[frameIndex].Duration / 1000.0f;
        }

        if (hasEnable)
        {
          MakeConstant(curveOffX);
          MakeConstant(curveOffY);
          MakeConstant(curveSizeX);
          MakeConstant(curveSizeY);
          MakeConstant(curveEnable);

          AnimationUtility.SetEditorCurve(clip, bindingOffX, curveOffX);
          AnimationUtility.SetEditorCurve(clip, bindingOffY, curveOffY);
          AnimationUtility.SetEditorCurve(clip, bindingSizeX, curveSizeX);
          AnimationUtility.SetEditorCurve(clip, bindingSizeY, curveSizeY);

          if (changeEnable)
          {
            AnimationUtility.SetEditorCurve(clip, bindingEnable, curveEnable);
          }

          EditorUtility.SetDirty(clip);
        }
      }
    }

    static List<IntRect> ComputeFrameBoundingBoxes(Layer layer, AseFile file)
    {
      var frameRects = new List<IntRect>();
      foreach (var frame in file)
      {
        Cel cel;
        var boundingBox = frame.TryGetCel(layer.Index, out cel) ? cel.GetBoundingBox() : new IntRect();
        frameRects.Add(boundingBox);
      }

      return frameRects;
    }

    static void MakeConstant(AnimationCurve curve)
    {
      for (var i = 0; i < curve.length; ++i)
      {
        AnimationUtility.SetKeyLeftTangentMode(curve, i, TangentMode.Constant);
      }
    }

    static EditorCurveBinding Binding(string path, Type type, string property)
    {
      return new EditorCurveBinding
      {
        path = path,
        type = type,
        propertyName = property
      };
    }
  }
}
