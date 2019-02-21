using AseImport.Importer;

namespace AseImport.MetaLayer
{
  public interface ISpritePostProcessor
  {
    string ActionName { get; }

    // The order of execution when importing an ase file. Higher order gets executed later.
    int ExecutionOrder { get; }

    void Process(SpriteProcessingContext ctx, Layer layer);
  }
}
