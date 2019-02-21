using System;

namespace AseImport.Importer
{
  public class LayerParam
  {
    public readonly LayerParamType Type;
    public readonly double NumberValue;
    public readonly string StringValue;
    public readonly bool BoolValue;

    public LayerParam(double numberValue)
    {
      this.Type = LayerParamType.Number;
      this.NumberValue = numberValue;
    }

    public LayerParam(string stringValue)
    {
      this.Type = LayerParamType.String;
      this.StringValue = stringValue;
    }

    public LayerParam(bool boolValue)
    {
      this.Type = LayerParamType.Bool;
      this.BoolValue = boolValue;
    }
  }
}