using Microsoft.EntityFrameworkCore;
using MyBackend.Model;
using MyBackend.Models;

namespace MyBackend.DataBase;
public class DBContext : DbContext
{
    public DBContext(DbContextOptions<DBContext> options)
        :base(options)
    {}

    public required DbSet<User> Users { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .Property(u => u.EsConsultor)
            .HasDefaultValue(false);
    }
}