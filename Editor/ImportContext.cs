using System;
using System.IO;
using AseImport.Importer;
using JetBrains.Annotations;
using UnityEngine;

namespace AseImport
{
  public class ImportContext
  {
    public readonly AseFile AseFile;
    public readonly string BaseName;
    public readonly RuntimeSettings Settings;

    public ImportContext([NotNull] string path, [NotNull] RuntimeSettings settings)
    {
      if (path == null)
      {
        throw new ArgumentNullException(nameof(path));
      }

      if (settings == null)
      {
        throw new ArgumentNullException(nameof(settings));
      }

      this.AseFile = AseFileParser.Parse(path);
      this.BaseName = Path.GetFileNameWithoutExtension(path);
      this.Settings = settings;
    }

    public class RuntimeSettings
    {
      public readonly SpriteAlignment Alignment;
      public readonly Vector2 CustomPivot;
      public readonly bool DenselyPacked;
      public readonly int Border;
      public readonly float PixelsPerUnit;

      public RuntimeSettings(AseScriptedImporter settings)
      {
        Alignment = (SpriteAlignment) settings.Alignment;
        CustomPivot = settings.CustomPivot;
        Border = settings.Border;
        DenselyPacked = settings.DenselyPacked;
        PixelsPerUnit = settings.PixelsPerUnit;
      }

      public Vector2 PivotRelativePos
      {
        get { return Alignment.GetRelativePos(CustomPivot); }
      }
    }
  }
}