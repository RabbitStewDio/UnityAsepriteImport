namespace AseImport.Importer
{
  public class Layer : ParameterBase, IUserDataAcceptor
  {
    public readonly int Index;
    public readonly bool Visible;
    public readonly BlendMode BlendMode;
    public readonly float Opacity;
    public string UserData { get; set; }
    public readonly LayerType Type;

    // --- META

    public Layer(int readLayerIndex, LayerType layerType, string layerName, bool visible, BlendMode blendMode, float opacity): base(layerName)
    {
      this.Index = readLayerIndex;
      this.Type = layerType;
      this.Visible = visible;
      this.BlendMode = blendMode;
      this.Opacity = opacity;
    }
  }
}
