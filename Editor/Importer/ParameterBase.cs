using System;
using System.Collections.Generic;

namespace AseImport.Importer
{
  public class ParameterBase
  {
    readonly List<LayerParam> parameters;
    public string Name { get; }
    /// If is metadata, the action name of the layer
    public string ActionName { get; internal set; }

    public ParameterBase(string name)
    {
      if (name == null)
      {
        throw new ArgumentNullException(nameof(name));
      }

      this.parameters = new List<LayerParam>();
      this.Name = name;
    }

    public void AddParameter(LayerParam param)
    {
      parameters.Add(param);
    }

    public int ParamCount
    {
      get { return parameters.Count; }
    }

    public int GetParamInt(int index)
    {
      return (int) CheckParamType(index, LayerParamType.Number).NumberValue;
    }

    public float GetParamFloat(int index)
    {
      return (float) CheckParamType(index, LayerParamType.Number).NumberValue;
    }

    public string GetParamString(int index)
    {
      return CheckParamType(index, LayerParamType.String).StringValue;
    }

    public bool GetParamBool(int index)
    {
      return CheckParamType(index, LayerParamType.Bool).BoolValue;
    }

    public bool TryGetParam(int index, LayerParamType type, out LayerParam param)
    {
      if (parameters.Count <= index)
      {
        param = null;
        return false;
      }

      var par = parameters[index];
      if (par.Type != type)
      {
        throw new Exception($"Type mismatch at parameter #{index}, expected {type}, got {par.Type}");
      }

      param = par;
      return true;
    }

    public LayerParamType GetParamType(int index)
    {
      if (parameters.Count <= index)
      {
        return LayerParamType.None;
      }

      return parameters[index].Type;
    }

    LayerParam CheckParamType(int index, LayerParamType type)
    {
      if (parameters.Count <= index)
      {
        throw new Exception("No parameter #" + index);
      }

      var par = parameters[index];
      if (par.Type != type)
      {
        throw new Exception($"Type mismatch at parameter #{index}, expected {type}, got {par.Type}");
      }

      return par;
    }
  }
}