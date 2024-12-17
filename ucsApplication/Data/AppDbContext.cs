using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;
using ucsApplication.Models;

namespace ucsApplication.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Change the DbSet property name to match what's being used in the controller
        public DbSet<TransactionTable> TransactionTable { get; set; }
        public DbSet<MasterTable> MasterTable { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // MasterTable configuration
            modelBuilder.Entity<MasterTable>(entity =>
            {
                entity.HasKey(e => e.MasterId); // Primary key
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.Username).IsRequired();
                entity.Property(e => e.FingerPrintData).IsRequired(false);
                entity.Property(e => e.LastTransactionDate).IsRequired();
            });

            // TransactionTable configuration
            modelBuilder.Entity<TransactionTable>(entity =>
            {
                entity.HasKey(e => e.Id); // Primary key
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.CheckinDateTime).IsRequired();
                entity.Property(e => e.CheckoutDateTime).IsRequired(false);
                entity.Property(e => e.CheckInMethod).IsRequired();
            });

            // Define foreign key relationship
            modelBuilder.Entity<TransactionTable>()
                .HasOne(t => t.Master)
                .WithMany(m => m.Transactions)
                .HasForeignKey(t => t.UserId) // Maps to MasterTable.MasterId
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
