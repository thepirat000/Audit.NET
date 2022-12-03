#if EF_CORE_5_OR_GREATER
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Audit.EntityFramework.Core.UnitTest
{
    [AuditDbContext(IncludeEntityObjects = true)]
    public class TptConfigContext : AuditDbContext
    {
        public TptConfigContext(DbContextOptions<TptConfigContext> options)
            : base(options)
        {
            Database.EnsureCreated();
        }

        public DbSet<ReservationRequest> ReservationRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new RequestMapping());
            modelBuilder.ApplyConfiguration(new ReservationRequestMapping());
            base.OnModelCreating(modelBuilder);
        }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }
    }

    public abstract class Request
    {
        public Guid Id { get; }
        [Column("Uid")]
        public string UserId { get; set; }
        public string LocationId { get; set; }

        protected Request() { }

        internal Request(string userId)
        {
            UserId = userId;
        }
    }
    public class ReservationRequest : Request
    {
        public string ReservationComments { get; set; }
        public DateTime ReservationTo { get; set; }

        internal ReservationRequest()
        {
        }
    }

    internal sealed class RequestMapping : IEntityTypeConfiguration<Request>
    {
        public void Configure(EntityTypeBuilder<Request> builder)
        {
            builder.ToTable("Requests");

            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.Id).IsUnique();

            builder.Property(x => x.UserId);
            builder.Property(x => x.LocationId);
        }
    }
    internal sealed class ReservationRequestMapping : IEntityTypeConfiguration<ReservationRequest>
    {
        public void Configure(EntityTypeBuilder<ReservationRequest> builder)
        {
            builder.ToTable("ReservationRequests");
        }
    }

}
#endif