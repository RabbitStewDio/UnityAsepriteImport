using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace AseImport.Importer
{
  public class LayerGroup : ParameterBase, IUserDataAcceptor
  {
    public int LayerIndex { get; }
    public readonly List<LayerGroup> SubGroups;
    public readonly List<Layer> Layers;
    public bool IsSubImage { get; }

    public LayerGroup(int layerIndex, [NotNull] string name) : base(name)
    {
      LayerIndex = layerIndex;
      SubGroups = new List<LayerGroup>();
      Layers = new List<Layer>();
      IsSubImage = Name.StartsWith("@sub") || layerIndex == -1;
    }

    public string ImageName
    {
      get
      {
        LayerParam param;
        if (TryGetParam(0, LayerParamType.String, out param))
        {
          return param.StringValue;
        }

        return Name;
      }
    }

    public string UserData { get; set; }

    /// <summary>
    ///  Locates the layer instance for a given layer index. This will not dive into
    ///  sub-images as they are handled in a separate run.
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public Layer FindLayer(int index)
    {
      foreach (var layer in Layers)
      {
        if (layer.Index == index)
        {
          return layer;
        }
      }

      foreach (var subGroup in SubGroups)
      {
        if (subGroup.IsSubImage)
        {
          continue;
        }

        var result = subGroup.FindLayer(index);
        if (result != null)
        {
          return result;
        }
      }

      return null;
    }

    public IEnumerable<LayerGroup> AllSubImages
    {
      get
      {
        var layers = new List<LayerGroup>();
        foreach (var g in SubGroups)
        {
          if (g.IsSubImage)
          {
            layers.Add(g);
          }

          layers.AddRange(g.AllSubImages);
        }

        return layers;
      }
    }

    public IEnumerable<Layer> AllLayers
    {
      get
      {
        IEnumerable<Layer> layers = Layers;
        foreach (var g in SubGroups)
        {
          if (g.IsSubImage)
          {
            continue;
          }

          layers = layers.Concat(g.AllLayers);
        }

        // Aseprite stores layers in reverse order to what the UI shows (bottom to top).
        // So the layer 0 is the bottom-most layer in the editor, layer 1 is above and so on.
        // To make sure events and other meta processing happens in the right order, we
        // switch the order here.
        return layers.OrderByDescending(l => l.Index);
      }
    }

    public string ObjectPath
    {
      get
      {
        LayerParam param;
        if (TryGetParam(1, LayerParamType.String, out param))
        {
          return param.StringValue;
        }

        return "";
      }
    }
  }
}
