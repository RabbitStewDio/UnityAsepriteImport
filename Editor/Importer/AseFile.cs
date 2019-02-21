using System;
using System.Collections;
using System.Collections.Generic;

namespace AseImport.Importer
{
  public class AseFile: IEnumerable<Frame>
  {
    public readonly int Width;
    public readonly int Height;
    public readonly LayerGroup Layers;
    readonly List<Frame> frames;
    readonly List<FrameTag> tags;

    public AseFile(int width, int height)
    {
      this.Width = width;
      this.Height = height;
      this.Layers = new LayerGroup(-1, "");
      this.frames = new List<Frame>();
      this.tags = new List<FrameTag>();
    }

    public void Add(Frame frame)
    {
      if (frame.FrameId != frames.Count)
      {
        throw new ArgumentException();
      }

      frames.Add(frame);
    }

    public void AddTag(FrameTag tag)
    {
      this.tags.Add(tag);
    }

    public IEnumerable<FrameTag> FrameTags => tags;

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    public IEnumerator<Frame> GetEnumerator()
    {
      return frames.GetEnumerator();
    }

    public Frame this[int index]
    {
      get
      {
        return frames[index];
      }
    }

    public IEnumerable<LayerGroup> SubImages
    {
      get
      {
        var retval = new List<LayerGroup>();
        retval.Add(Layers);
        retval.AddRange(Layers.AllSubImages);
        return retval;
      }
    }

    public void ResolveLinkedCels()
    {
      foreach (var frame in frames)
      {
        foreach (var cel in frame.Cels)
        {
          if (cel.IsLinked)
          {
            Cel src;
            if (this[cel.LinkedFrame].TryGetCel(cel.LayerIndex, out src))
            {
              cel.DereferenceFrom(src);
            }
          }
        }
      }
    }
  }
}