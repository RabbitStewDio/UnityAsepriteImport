using System;
using UnityEngine;

namespace AseImport.Importer
{
  public class Cel : IUserDataAcceptor
  {
    static readonly Color opaque = new Color(0, 0, 0, 0);

    public readonly int LayerIndex;

    public float Opacity { get; private set; }
    public int X { get; private set; }
    public int Y { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }

    public string UserData { get; set; }

    public int LinkedFrame { get; }
    public bool IsLinked => LinkedFrame != -1;

    internal Color[] ColorBuffer;

    public Cel(int x, 
               int y, 
               int width, 
               int height, 
               Color[] buffer, 
               int layerIndex, 
               float opacity)
    {
      if (buffer.Length != (width * height))
      {
        throw new ArgumentException();
      }

      this.X = x;
      this.Y = y;
      this.Width = width;
      this.Height = height;
      this.ColorBuffer = (Color[]) buffer.Clone();
      this.LayerIndex = layerIndex;
      this.Opacity = opacity;
      this.LinkedFrame = -1;
    }

    public Cel(int linkFrameTarget, int layerIndex)
    {
      this.LinkedFrame = linkFrameTarget;
      this.LayerIndex = layerIndex;
    }

    public void DereferenceFrom(Cel linkTarget)
    {
      this.X = linkTarget.X;
      this.Y = linkTarget.Y;
      this.Width = linkTarget.Width;
      this.Height = linkTarget.Height;
      this.ColorBuffer = linkTarget.ColorBuffer;
      this.Opacity = linkTarget.Opacity;
      this.UserData = linkTarget.UserData;
    }

    // Get the color of the cel in cel space
    Color GetPixelRaw(int x, int y)
    {
      return ColorBuffer[y * Width + x];
    }

    // Get the color of the cel in sprite image space
    public Color GetPixel(int x, int y)
    {
      var relx = x - this.X;
      var rely = y - this.Y;
      if (0 <= relx && relx < Width && 0 <= rely && rely < Height)
      {
        return GetPixelRaw(relx, rely);
      }
      else
      {
        return opaque;
      }
    }
  }
}