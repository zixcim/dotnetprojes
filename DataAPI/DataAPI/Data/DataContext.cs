using Microsoft.EntityFrameworkCore;

namespace DataAPI
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }

        public DbSet<ExchangeRate> ExchangeRates { get; set; }
    }
}
