using LogisticsApp.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LogisticsApp.Api.Data
{
    public class LogisticsContext : DbContext
    {
        public LogisticsContext(DbContextOptions<LogisticsContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Shipment> Shipments { get; set; }
        public DbSet<ShipmentLog> ShipmentLogs { get; set; }
    }
}
