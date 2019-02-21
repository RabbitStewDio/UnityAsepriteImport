using System;
using System.Collections.Generic;
using System.Linq;
using AseImport.Importer;
using UnityEditor;
using UnityEngine;

namespace AseImport.MetaLayer
{
  /// <summary>
  /// @event Action 
  /// 
  /// Layer syntax: @event("messageName" [, param])
  /// Usage: Create an animation event at the given frames with an optional parameter to send.Only frames that have any pixels actually drawn are included.
  /// 
  /// </summary>
  public class MetaLayerEvent : IMetaLayerProcessor
  {
    public int ExecutionOrder => 0;
    public string ActionName => "event";

    public void Process(MetaProcessingContext ctx, Layer layer)
    {
      var file = ctx.ImportContext.AseFile;

      var eventFrames = ComputeEventFrameSet(layer, file);

      var paramType = layer.GetParamType(1);
      Debug.Log("Processing event " + layer.GetParamString(0));

      foreach (var frametag in file.FrameTags)
      {
        AnimationClip clip;
        if (!ctx.TryGetClip(frametag, out clip))
        {
          continue;
        }

        var events = new List<EventCarrier>();
        foreach (var existingEvents in clip.events)
        {
          events.Add(new EventCarrier(existingEvents, events.Count));
        }

        var time = 0.0f;
        for (var frameIndex = frametag.From; frameIndex <= frametag.To; ++frameIndex)
        {
          if (eventFrames.Contains(frameIndex))
          {
            var evt = new AnimationEvent
            {
              time = time,
              functionName = layer.GetParamString(0),
              messageOptions = SendMessageOptions.DontRequireReceiver
            };

            // Debug.Log(paramType + ", " + layer.metaInfo.ParamCount);

            if (paramType == LayerParamType.String)
            {
              evt.stringParameter = layer.GetParamString(1);
            }
            else if (paramType == LayerParamType.Number)
            {
              var fval = layer.GetParamFloat(1);
              evt.floatParameter = fval;
              if (Math.Abs(fval - Math.Floor(fval)) < 0.001f)
              {
                evt.intParameter = (int) fval;
              }
            }

            events.Add(new EventCarrier(evt, events.Count));
          }

          time += file[frameIndex].Duration / 1000f;
        }

        var eventsArray = events.OrderBy(t => t, new EventCarrierComparer()).Select(t => t.Event).ToArray();
        AnimationUtility.SetAnimationEvents(clip, eventsArray);
        EditorUtility.SetDirty(clip);
      }
    }

    static HashSet<int> ComputeEventFrameSet(Layer layer, AseFile file)
    {
      var eventFrames = new HashSet<int>();
      foreach (var frame in file)
      {
        Cel ignored;
        if (frame.TryGetCel(layer.Index, out ignored))
        {
          eventFrames.Add(frame.FrameId);
        }
      }

      return eventFrames;
    }

    class EventCarrierComparer : IComparer<EventCarrier>
    {
      public int Compare(EventCarrier x, EventCarrier y)
      {
        var cmp = x.SortOrder.CompareTo(y.SortOrder);
        if (cmp != 0)
        {
          return cmp;
        }

        return x.Event.time.CompareTo(y.Event.time);
      }
    }

    struct EventCarrier
    {
      public readonly AnimationEvent Event;
      public readonly int SortOrder;

      public EventCarrier(AnimationEvent evt, int sortOrder)
      {
        if (evt == null)
        {
          throw new ArgumentNullException(nameof(evt));
        }

        Event = evt;
        SortOrder = sortOrder;
      }
    }
  }
}
