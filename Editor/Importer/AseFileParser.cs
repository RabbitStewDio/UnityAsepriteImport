using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;

namespace AseImport.Importer
{
  public static class AseFileParser
  {
    enum AseLayerType
    {
      Layer = 0,
      LayerGroup = 1
    }

    enum ColorType
    {
      Unknown = 0,  
      RGBA = 1, 
      GrayScale = 2, 
      Indexed = 3
    }

    const ushort CHUNK_LAYER = 0x2004;
    const ushort CHUNK_CEL = 0x2005;
    const ushort CHUNK_CELEXTRA = 0x2006;
    const ushort CHUNK_FRAME_TAGS = 0x2018;
    const ushort CHUNK_PALETTE = 0x2019;
    const ushort CHUNK_USERDATA = 0x2020;


    public static AseFile Parse(string path)
    {
      using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
      {
        using (var reader = new BinaryReader(stream))
        {
          return Parse(reader);
        }
      }
    }

    public static AseFile Parse(byte[] bytes)
    {
      var stream = new MemoryStream(bytes);
      using (var reader = new BinaryReader(stream))
      {
        return Parse(reader);
      }
    }

    public static LayerGroup ParseSubImageStructure(string path)
    {
      using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
      {
        using (var reader = new BinaryReader(stream))
        {
          return ParseSubImageStructure(reader);
        }
      }
    }

    public static LayerGroup ParseSubImageStructure(byte[] bytes)
    {
      var stream = new MemoryStream(bytes);
      using (var reader = new BinaryReader(stream))
      {
        return ParseSubImageStructure(reader);
      }
    }

    public static AseFile Parse(BinaryReader reader)
    {
      int frameCount;
      ColorType colorType;
      var file = ParseHeader(reader, out frameCount, out colorType);

      var layerGroups = new Stack<LayerGroup>();
      layerGroups.Push(file.Layers);
      var readLayerIndex = 0;

      IUserDataAcceptor lastUserdataAcceptor = new NoOpUserDataAcceptor();

      for (var i = 0; i < frameCount; ++i)
      {
        reader.ReadDWord(); // frameBytes
        _CheckMagicNumber(reader.ReadWord(), 0xF1FA);

        var oldChunkCount = reader.ReadWord();
        var duration = reader.ReadWord();
        var frame = new Frame(i, duration);
        reader.ReadBytes(2);
        var newChunkCount = reader.ReadDWord();
        var chunkCount = (oldChunkCount == 0xFFFF) ? newChunkCount : oldChunkCount;

        for (int j = 0; j < chunkCount; ++j)
        {
          var chunkBytes = reader.ReadDWord(); // 4
          var chunkType = reader.ReadWord(); // 2

          switch (chunkType)
          {
            case CHUNK_LAYER:
            {
              // only found in the first frame. This defines the layer layout for all frames.
              lastUserdataAcceptor = ParseLayer(file, reader, layerGroups, readLayerIndex);
              readLayerIndex += 1;
              break;
            }
            case CHUNK_CEL:
            {
              lastUserdataAcceptor = ParseCel(file, colorType, reader, chunkBytes, frame);
              break;
            }
            case CHUNK_FRAME_TAGS:
            {
              ParseFrameTags(file, reader);
              break;
            }
            case CHUNK_USERDATA:
            {
              ParseUserData(reader, lastUserdataAcceptor);
              break;
            }
            default:
            {
              reader.ReadBytes(chunkBytes - 6);
              break;
            }
          }
        }

        file.Add(frame);
      }

      // Post process: calculate pixel alpha
      foreach (var frame in file)
      {
        foreach (var cel in frame.Cels)
        {
          if (!cel.IsLinked)
          {
            var transparency = cel.Opacity * file.FindLayer(cel.LayerIndex).Opacity;
            for (var i = 0; i < cel.ColorBuffer.Length; ++i)
            {
              cel.ColorBuffer[i].a *= transparency;
            }
          }
        }
      }

      // Post process: eliminate reference cels
      file.ResolveLinkedCels();
      return file;
    }

    static LayerGroup ParseSubImageStructure(BinaryReader reader)
    {
      int frameCount;
      ColorType colorType;
      var file = ParseHeader(reader, out frameCount, out colorType);

      var layerGroups = new Stack<LayerGroup>();
      layerGroups.Push(file.Layers);
      var readLayerIndex = 0;

      if (frameCount > 0)
      {
        // reading exactly one frame
        // Aseprite stores all layer information as chunk in the first frame.

        reader.ReadDWord(); // frameBytes
        _CheckMagicNumber(reader.ReadWord(), 0xF1FA);

        var oldChunkCount = reader.ReadWord();
        reader.ReadWord();
        reader.ReadBytes(2);
        var newChunkCount = reader.ReadDWord();
        var chunkCount = (oldChunkCount == 0xFFFF) ? newChunkCount : oldChunkCount;

        for (int j = 0; j < chunkCount; ++j)
        {
          var chunkBytes = reader.ReadDWord(); // 4
          var chunkType = reader.ReadWord(); // 2

          switch (chunkType)
          {
            case CHUNK_LAYER:
            {
              // only found in the first frame. This defines the layer layout for all frames.
              ParseLayer(file, reader, layerGroups, readLayerIndex);
              readLayerIndex += 1;
              break;
            }
            default:
            {
              reader.ReadBytes(chunkBytes - 6);
              break;
            }
          }
        }
      }

      return file.Layers;
    }

    static AseFile ParseHeader(BinaryReader reader, 
                               out int frameCount,
                               out ColorType colorType)
    {
      reader.ReadDWord(); // File size
      _CheckMagicNumber(reader.ReadWord(), 0xA5E0);

      frameCount = reader.ReadWord();

      var width = reader.ReadWord();
      var height = reader.ReadWord();
      var file = new AseFile(width, height);

      var colorDepth = reader.ReadWord();

      if (colorDepth == 32)
      {
        colorType = ColorType.RGBA;
      }
      else if (colorDepth == 16)
      {
        colorType = ColorType.GrayScale;
      }
      else
      {
        throw new ArgumentException("Colour depth of " + colorDepth + " bits is not currently supported.");
      }

      reader.ReadDWord(); // Flags
      reader.ReadWord(); // Deprecated speed
      _CheckMagicNumber(reader.ReadDWord(), 0);
      _CheckMagicNumber(reader.ReadDWord(), 0);

      reader.ReadBytes(4);
      reader.ReadWord();
      reader.ReadBytes(2);
      reader.ReadBytes(92);
      return file;
    }

    static IUserDataAcceptor ParseLayer(AseFile file,
                                        BinaryReader reader,
                                        Stack<LayerGroup> layerGroups,
                                        int readLayerIndex)
    {
      IUserDataAcceptor lastUserdataAcceptor;
      var flags = reader.ReadWord();

      var visible = (flags & 0x1) != 0;
      var referenceLayer = (flags & 0x20) != 0;

      var layerType = (AseLayerType) reader.ReadWord();
      var childLevel = reader.ReadWord(); // childLevel
      if ((layerGroups.Count - 1) != (childLevel))
      {
        layerGroups.Pop();
        if (layerGroups.Count == 0)
        {
          // workaround broken files ..
          Debug.LogWarning("Layer Structure Parsing Error.");
          layerGroups.Push(file.Layers);
        }
      }

      reader.ReadWord();
      reader.ReadWord();

      var blendMode = (BlendMode) reader.ReadWord();
      var opacity = reader.ReadByte() / 255.0f;
      reader.ReadBytes(3);

      var layerName = reader.ReadUTF8();
      var type = layerName.StartsWith("@") ? LayerType.Meta : LayerType.Content;
      Debug.Log("Reading layer definition " + readLayerIndex + ", " + layerName);

      if (layerType == AseLayerType.Layer && !layerName.StartsWith("//") && !referenceLayer)
      {
        //layer.index = readLayerIndex;
        var layer = new Layer(readLayerIndex, type, layerName, visible, blendMode, opacity);
        if (layer.Type == LayerType.Meta || visible)
        {
          MetaLayerParser.Parse(layer);
          layerGroups.Peek().Layers.Add(layer);
        }

        lastUserdataAcceptor = layer;
      }
      else if (layerType == AseLayerType.LayerGroup)
      {
        // is a layer group.
        var layerGroup = new LayerGroup(readLayerIndex, layerName);
        MetaLayerParser.Parse(layerGroup);
        layerGroups.Peek().SubGroups.Add(layerGroup);
        layerGroups.Push(layerGroup);
        lastUserdataAcceptor = layerGroup;
      }
      else
      {
        lastUserdataAcceptor = new NoOpUserDataAcceptor();
      }

      return lastUserdataAcceptor;
    }

    static void ParseUserData(BinaryReader reader, IUserDataAcceptor lastUserdataAcceptor)
    {
      var flags = reader.ReadDWord();
      var hasText = (flags & 0x01) != 0;
      var hasColor = (flags & 0x02) != 0;

      if (hasText)
      {
        lastUserdataAcceptor.UserData = reader.ReadUTF8();
      }

      if (hasColor)
      {
        reader.ReadBytes(4);
      }
    }

    static void ParseFrameTags(AseFile file, BinaryReader reader)
    {
      var count = reader.ReadWord();
      reader.ReadBytes(8);

      for (int c = 0; c < count; ++c)
      {
        var fromFrame = reader.ReadWord();
        var toFrame = reader.ReadWord();
        var loopMode = (LoopMode) reader.ReadByte();
        reader.ReadBytes(8);
        reader.ReadBytes(3);
        reader.ReadByte();

        var name = reader.ReadUTF8();

        if (name.StartsWith("//"))
        {
          // Commented tags are ignored
          continue;
        }

        var originalName = name;

        var tagIdx = name.IndexOf('#');
        var nameInvalid = false;
        var properties = new HashSet<string>();
        if (tagIdx != -1)
        {
          name = name.Substring(0, tagIdx).Trim();
          var possibleProperties = originalName.Substring(tagIdx).Split(' ');
          foreach (var possibleProperty in possibleProperties)
          {
            if (possibleProperty.Length > 1 && possibleProperty[0] == '#')
            {
              properties.Add(possibleProperty.Substring(1));
            }
            else
            {
              nameInvalid = true;
            }
          }
        }

        if (nameInvalid)
        {
          Debug.LogWarning("Invalid frame name: " + originalName);
        }

        file.AddTag(new FrameTag(name, fromFrame, toFrame, loopMode, properties));
      }
    }

    static IUserDataAcceptor ParseCel(AseFile file, ColorType colorType,
                                      BinaryReader reader, int chunkBytes, Frame frame)
    {
      var layerIndex = reader.ReadWord(); // 2
      var x = reader.ReadInt16(); // 2
      var y = reader.ReadInt16(); // 2
      var opacity = reader.ReadByte() / 255.0f; // 1
      var type = (CelType) reader.ReadWord(); // 2
      reader.ReadBytes(7); // 7

      Cel cel;
      switch (type)
      {
        case CelType.Raw:
        {
          var celWidth = reader.ReadWord(); // 2
          var celHeight = reader.ReadWord(); // 2
          var colorBuffer = ToColorBuffer(colorType, reader.ReadBytes(chunkBytes - 6 - 16 - 4));
          cel = new Cel(x, y, celWidth, celHeight, colorBuffer, layerIndex, opacity);
          break;
        }
        case CelType.Linked:
        {
          var linkedFrame = reader.ReadWord();
          cel = new Cel(linkedFrame, layerIndex);
          break;
        }
        case CelType.Compressed:
        {
          var celWidth = reader.ReadWord();
          var celHeight = reader.ReadWord();
          var colorBuffer = ToColorBuffer(colorType, reader.ReadCompressedBytes(chunkBytes - 6 - 16 - 4));
          cel = new Cel(x, y, celWidth, celHeight, colorBuffer, layerIndex, opacity);
          break;
        }
        default:
        {
          throw new ArgumentException();
        }
      }

      if (file.FindLayer(cel.LayerIndex) != null)
      {
        frame.AddCel(cel);
      }

      return cel;
    }

    static Layer FindLayer(this AseFile file, int index)
    {
      return FindLayerGlobally(file.Layers, index);
    }

    static Layer FindLayerGlobally(LayerGroup group, int index)
    {
      foreach (var g in group.Layers)
      {
        if (g.Index == index)
        {
          return g;
        }
      }

      foreach (var g in group.SubGroups)
      {
        var r = FindLayerGlobally(g, index);
        if (r != null)
        {
          return r;
        }
      }

      return null;
    }

    static int ReadDWord(this BinaryReader reader)
    {
      return (int) reader.ReadUInt32();
    }

    static ushort ReadWord(this BinaryReader reader)
    {
      return reader.ReadUInt16();
    }

    static string ReadUTF8(this BinaryReader reader)
    {
      var length = reader.ReadWord();
      var chars = reader.ReadBytes(length);
      return Encoding.UTF8.GetString(chars);
    }

    static Color[] ToColorBuffer(ColorType colorType, byte[] bytes)
    {
      switch (colorType)
      {
        case ColorType.RGBA:
          return ToColorBufferRGBA(bytes);
        case ColorType.GrayScale:
          return ToColorBufferGrayscale(bytes);
        case ColorType.Indexed:
          throw new ArgumentOutOfRangeException(nameof(colorType), "Indexed mode is not yet supported.");
        default:
          throw new ArgumentOutOfRangeException(nameof(colorType), colorType, null);
      }
    }

    static Color[] ToColorBufferGrayscale(byte[] bytes)
    {
      if (bytes.Length % 2 != 0)
      {
        _Error("Invalid color data");
      }

      var arr = new Color[bytes.Length / 2];
      for (var i = 0; i < arr.Length; ++i)
      {
        var offset = i * 2;

        var color = Color.white;
        color.r = bytes[offset] / 255.0f;
        color.g = bytes[offset] / 255.0f;
        color.b = bytes[offset] / 255.0f;
        color.a = bytes[offset + 1] / 255.0f;

        arr[i] = color;
      }

      return arr;
    }    
    
    static Color[] ToColorBufferRGBA(byte[] bytes)
    {
      if (bytes.Length % 4 != 0)
      {
        _Error("Invalid color data");
      }

      var arr = new Color[bytes.Length / 4];
      for (var i = 0; i < arr.Length; ++i)
      {
        var offset = i * 4;

        var color = Color.white;
        color.r = bytes[offset] / 255.0f;
        color.g = bytes[offset + 1] / 255.0f;
        color.b = bytes[offset + 2] / 255.0f;
        color.a = bytes[offset + 3] / 255.0f;

        arr[i] = color;
      }

      return arr;
    }

    static byte[] ReadCompressedBytes(this BinaryReader reader, int count)
    {
      reader.ReadByte();
      reader.ReadByte();
      using (var deflateStream = new DeflateStream(new MemoryStream(reader.ReadBytes(count - 2 - 4)),
                                                   CompressionMode.Decompress))
      {
        var bytes = ReadFully(deflateStream);
        reader.ReadDWord(); // Skip the ADLER32 checksum
        return bytes;
      }
    }

    static byte[] ReadFully(Stream input)
    {
      byte[] buffer = new byte[16 * 1024];
      using (MemoryStream ms = new MemoryStream())
      {
        int read;
        while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
        {
          ms.Write(buffer, 0, read);
        }

        return ms.ToArray();
      }
    }

    static void _CheckMagicNumber<T>(T number, T expected)
      where T : IEquatable<T>
    {
      if (!(number.Equals(expected)))
      {
        _Error("File validation failed");
      }
    }

    static void _Error(string msg)
    {
      throw new Exception(msg);
    }
  }
}
