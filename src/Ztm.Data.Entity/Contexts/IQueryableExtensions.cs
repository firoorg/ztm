using System.Linq;
using Microsoft.EntityFrameworkCore;
using Ztm.Data.Entity.Contexts.Main;

namespace Ztm.Data.Entity.Contexts
{
    public static class IQueryableExtensions
    {
        public static IQueryable<Block> IncludeAll(this IQueryable<Block> query)
        {
            return query.Include(e => e.Transactions)
                .ThenInclude(e => e.Transaction)
                .ThenInclude(e => e.Outputs)
                .Include(e => e.Transactions)
                .ThenInclude(e => e.Transaction)
                .ThenInclude(e => e.Inputs);
        }
    }
}
