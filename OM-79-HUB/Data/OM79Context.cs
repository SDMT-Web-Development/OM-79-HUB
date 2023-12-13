using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore;
using OM79.Models;

namespace OM79.Models.DB;

public partial class OM79Context : DbContext
{
    public OM79Context()
    {
    }

    public OM79Context(DbContextOptions<OM79Context> options)
        : base(options)
    {
    }
    public bool HasChanges()
    {
        return ChangeTracker.HasChanges();
    }
    public virtual DbSet<OMTable> OMTable { get; set; }
    public virtual DbSet<Attachments> Attachments { get; set; }

  //  protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
    //    => optionsBuilder.UseSqlServer("Data Source=T20DOHB05L06416\\SQLEXPRESS;Initial Catalog=OM79;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False");

   // protected override void OnModelCreating(ModelBuilder modelBuilder)
   // {
     //   modelBuilder.Entity<Omtable>(entity =>
    //    {
     //       entity.ToTable("OMTable");

      //      entity.Property(e => e.APBusinesses).HasColumnName("APBusinesses");
     //       entity.Property(e => e.APHouses).HasColumnName("APHouses");
      //      entity.Property(e => e.APOther).HasColumnName("APOther");
     //       entity.Property(e => e.APOtherIdentify).HasColumnName("APOtherIdentify");
     //       entity.Property(e => e.APSchools).HasColumnName("APSchools");
      //      entity.Property(e => e.DESignature).HasColumnName("DESignature");
      //      entity.Property(e => e.DOTAARNumber).HasColumnName("DOTAARNumber");
      //      entity.Property(e => e.SubmissionDate).HasColumnType("date");
    //    });

     //   OnModelCreatingPartial(modelBuilder);
   // }

   // partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
