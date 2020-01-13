using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Ztm.Data.Entity.Postgres
{
    public static class DbContextOptionsBuilderExtensions
    {
        public static void UseUInt256TypeMappingSource(this DbContextOptionsBuilder builder)
        {
            builder.ReplaceService<IRelationalTypeMappingSource, TypeMappingSource>();
        }
    }
}