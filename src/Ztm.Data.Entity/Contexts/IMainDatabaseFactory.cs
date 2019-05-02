namespace Ztm.Data.Entity.Contexts
{
    public interface IMainDatabaseFactory
    {
        MainDatabase CreateDbContext();
    }
}
