using AseImport.Importer;

namespace AseImport.MetaLayer
{
  public interface IMetaLayerProcessor
  {
    string ActionName { get; }

    // The order of execution when importing an ase file. Higher order gets executed later.
    int ExecutionOrder { get; }

    void Process(MetaProcessingContext ctx, Layer layer);
  }
}
