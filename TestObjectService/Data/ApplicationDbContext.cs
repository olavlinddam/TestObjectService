using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestObjectService.Models;

namespace TestObjectService.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<TestObject> TestObjects { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Setting up cascading deletion of sniffing points when test object is deleted.
            modelBuilder.Entity<TestObject>()
                .HasMany(t => t.SniffingPoints) 
                .WithOne() 
                .HasForeignKey(sp => sp.TestObjectId) 
                .OnDelete(DeleteBehavior.Cascade); // This sets up cascade delete
        }

    }
}
