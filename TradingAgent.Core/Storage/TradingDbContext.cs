using Microsoft.EntityFrameworkCore;
using TradingAgent.Core.Config;

namespace TradingAgent.Core.Storage;

public class TradingDbContext : DbContext
{
    public DbSet<TradeHistoryRecord> TradeHistoryRecords { get; set; }
    public DbSet<Position> Positions { get; set; }
    public DbSet<ReasoningRecord?> ReasoningRecords { get; set; }

    private readonly string _dbPath;

    public TradingDbContext(AppConfig config)
    {
        var folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        _dbPath = Path.Join(path, config.DatabasePath);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={_dbPath}");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TradeHistoryRecord>()
            .HasIndex(t => t.Date);

        modelBuilder.Entity<Position>()
            .HasIndex(p => p.Symbol)
            .IsUnique();

        modelBuilder.Entity<ReasoningRecord>()
            .HasIndex(r => r.Ticker)
            .IsUnique();
    }
}