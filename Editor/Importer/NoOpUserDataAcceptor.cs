namespace AseImport.Importer
{
  class NoOpUserDataAcceptor : IUserDataAcceptor
  {
    public string UserData { get { return ""; } set { } }
  }
}