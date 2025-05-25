using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradingAgent.Core.Config;

namespace TradingAgent.Core.Storage;

public class TradingDbContext : DbContext
{
    private readonly ILogger<TradingDbContext> _logger;
    public DbSet<TradeHistoryRecord> TradeHistoryRecords { get; set; }
    public DbSet<Position> Positions { get; set; }
    public DbSet<ReasoningRecord?> ReasoningRecords { get; set; }

    private readonly string _dbPath;

    public TradingDbContext(AppConfig config, ILogger<TradingDbContext> logger)
    {
        this._logger = logger;
        this._dbPath = config.DatabasePath;
        this._logger.LogInformation("db path: {databasePath}", this._dbPath);
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