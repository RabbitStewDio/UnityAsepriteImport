using AseImport.Importer;

namespace AseImport.MetaLayer
{
  // syntax: @sub(string subImageName)
  public class MetaLayerSub : IMetaLayerProcessor
  {
    public int ExecutionOrder => 0;

    public string ActionName
    {
      get { return "sub"; }
    }

    public void Process(MetaProcessingContext context, Layer layer)
    {
    }
  }
}
