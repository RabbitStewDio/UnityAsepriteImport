using System;
using UnityEngine;

namespace AseImport
{
  public struct IntRect
  {
    public readonly int X;
    public readonly int Y;
    public readonly int Width;
    public readonly int Height;

    public IntRect(int x, int y, int width, int height)
    {
      if (width < 0) throw new ArgumentException("must not be negative", nameof(width));
      if (height < 0) throw new ArgumentException("must not be negative", nameof(height));

      X = x;
      Y = y;
      Width = width;
      Height = height;
    }

    public int MinX => X;
    public int MaxX => X + Width;
    public int MinY => Y;
    public int MaxY => Y + Height;
    public Vector2 Origin => new Vector2(X, Y);
    public Vector2 Size => new Vector2(Width, Height);
    public Vector2 Center => new Vector2((MaxX + MinX) / 2f, (MaxY + MinY) / 2f);

    public Rect Scale(float factor)
    {
      return new Rect(X * factor, Y * factor, Width * factor, Height * factor);
    }

    public override string ToString()
    {
      return $"IntRect({nameof(X)}: {X}, {nameof(Y)}: {Y}, {nameof(Width)}: {Width}, {nameof(Height)}: {Height})";
    }
  }
}
