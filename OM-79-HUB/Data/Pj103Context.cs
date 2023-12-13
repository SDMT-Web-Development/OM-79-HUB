using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using PJ103V3.Models.DB;

namespace OM_79_HUB.Data;

public partial class Pj103Context : DbContext
{
    public Pj103Context()
    {
    }

    public Pj103Context(DbContextOptions<Pj103Context> options)
        : base(options)
    {
    }
    public bool HasChanges()
    {
        return ChangeTracker.HasChanges();
    }


    public virtual DbSet<Span> Spans { get; set; }

    public virtual DbSet<Submission> Submissions { get; set; }

    public virtual DbSet<RouteInfo> RouteInfo { get; set; }
    public virtual DbSet<BridgeRR> BridgeRR { get; set; }

    public DbSet<Attachments> Attachments { get; set; }


    //   protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    // #warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
    //     => optionsBuilder.UseSqlServer("Data Source=T20DOHB05L06416\\SQLEXPRESS;Initial Catalog=PJ103;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False");

    //  protected override void OnModelCreating(ModelBuilder modelBuilder)
    // {
    //     modelBuilder.Entity<Span>(entity =>
    //    {
    //       entity.Property(e => e.Id).HasColumnName("ID");
    //       entity.Property(e => e.LengthOfSpan1).HasColumnType("decimal(6, 2)");
    //      entity.Property(e => e.LengthOfSpan10).HasColumnType("decimal(6, 2)");
    //       entity.Property(e => e.LengthOfSpan11).HasColumnType("decimal(6, 2)");
    //      entity.Property(e => e.LengthOfSpan12).HasColumnType("decimal(6, 2)");
    //      entity.Property(e => e.LengthOfSpan13).HasColumnType("decimal(6, 2)");
    //     entity.Property(e => e.LengthOfSpan14).HasColumnType("decimal(6, 2)");
    //      entity.Property(e => e.LengthOfSpan15).HasColumnType("decimal(6, 2)");
    //      entity.Property(e => e.LengthOfSpan16).HasColumnType("decimal(6, 2)");
    //     entity.Property(e => e.LengthOfSpan17).HasColumnType("decimal(6, 2)");
    //     entity.Property(e => e.LengthOfSpan18).HasColumnType("decimal(6, 2)");
    //     entity.Property(e => e.LengthOfSpan19).HasColumnType("decimal(6, 2)");
    //     entity.Property(e => e.LengthOfSpan2).HasColumnType("decimal(6, 2)");
    //     entity.Property(e => e.LengthOfSpan20).HasColumnType("decimal(6, 2)");
    //      entity.Property(e => e.LengthOfSpan21).HasColumnType("decimal(6, 2)");
    //      entity.Property(e => e.LengthOfSpan22).HasColumnType("decimal(6, 2)");
    //      entity.Property(e => e.LengthOfSpan23).HasColumnType("decimal(6, 2)");
    //      entity.Property(e => e.LengthOfSpan24).HasColumnType("decimal(6, 2)");
    //     entity.Property(e => e.LengthOfSpan25).HasColumnType("decimal(6, 2)");
    //      entity.Property(e => e.LengthOfSpan26).HasColumnType("decimal(6, 2)");
    //      entity.Property(e => e.LengthOfSpan27).HasColumnType("decimal(6, 2)");
    //     entity.Property(e => e.LengthOfSpan28).HasColumnType("decimal(6, 2)");
    //     entity.Property(e => e.LengthOfSpan29).HasColumnType("decimal(6, 2)");
    //      entity.Property(e => e.LengthOfSpan3).HasColumnType("decimal(6, 2)");
    //     entity.Property(e => e.LengthOfSpan30).HasColumnType("decimal(6, 2)");
    //     entity.Property(e => e.LengthOfSpan4).HasColumnType("decimal(6, 2)");
    //     entity.Property(e => e.LengthOfSpan5).HasColumnType("decimal(6, 2)");
    //     entity.Property(e => e.LengthOfSpan6).HasColumnType("decimal(6, 2)");
    //     entity.Property(e => e.LengthOfSpan7).HasColumnType("decimal(6, 2)");
    //     entity.Property(e => e.LengthOfSpan8).HasColumnType("decimal(6, 2)");
    //    entity.Property(e => e.LengthOfSpan9).HasColumnType("decimal(6, 2)");
    //   entity.Property(e => e.NumberOfSpans).HasMaxLength(50);
    //     entity.Property(e => e.SubmissionId).HasColumnName("SubmissionID");
    //     entity.Property(e => e.SubmissionIdspan).HasColumnName("SubmissionIDSPAN");
    //     entity.Property(e => e.TypeOfSpan1).HasMaxLength(50);
    //    entity.Property(e => e.TypeOfSpan10).HasMaxLength(50);
    //    entity.Property(e => e.TypeOfSpan11).HasMaxLength(50);
    //     entity.Property(e => e.TypeOfSpan12).HasMaxLength(50);
    //    entity.Property(e => e.TypeOfSpan13).HasMaxLength(50);
    //    entity.Property(e => e.TypeOfSpan14).HasMaxLength(50);
    //     entity.Property(e => e.TypeOfSpan15).HasMaxLength(50);
    //     entity.Property(e => e.TypeOfSpan16).HasMaxLength(50);
    //     entity.Property(e => e.TypeOfSpan17).HasMaxLength(50);
    //     entity.Property(e => e.TypeOfSpan18).HasMaxLength(50);
    //    entity.Property(e => e.TypeOfSpan19).HasMaxLength(50);
    //     entity.Property(e => e.TypeOfSpan2).HasMaxLength(50);
    //      entity.Property(e => e.TypeOfSpan20).HasMaxLength(50);
    //     entity.Property(e => e.TypeOfSpan21).HasMaxLength(50);
    //     entity.Property(e => e.TypeOfSpan22).HasMaxLength(50);
    //    entity.Property(e => e.TypeOfSpan23).HasMaxLength(50);
    //      entity.Property(e => e.TypeOfSpan24).HasMaxLength(50);
    //    entity.Property(e => e.TypeOfSpan25).HasMaxLength(50);
    //      entity.Property(e => e.TypeOfSpan26).HasMaxLength(50);
    //      entity.Property(e => e.TypeOfSpan27).HasMaxLength(50);
    //     entity.Property(e => e.TypeOfSpan28).HasMaxLength(50);
    //     entity.Property(e => e.TypeOfSpan29).HasMaxLength(50);
    //     entity.Property(e => e.TypeOfSpan3).HasMaxLength(50);
    //     entity.Property(e => e.TypeOfSpan30).HasMaxLength(50);
    //     entity.Property(e => e.TypeOfSpan4).HasMaxLength(50);
    //     entity.Property(e => e.TypeOfSpan5).HasMaxLength(50);
    //   entity.Property(e => e.TypeOfSpan6).HasMaxLength(50);
    //    entity.Property(e => e.TypeOfSpan7).HasMaxLength(50);
    //    entity.Property(e => e.TypeOfSpan8).HasMaxLength(50);
    //     entity.Property(e => e.TypeOfSpan9).HasMaxLength(50);
    //   });

    //    modelBuilder.Entity<Submission>(entity =>
    //   {
    //       entity.Property(e => e.SubmissionId).HasColumnName("SubmissionID");
    //       entity.Property(e => e.AccessControl).HasMaxLength(50);
    //      entity.Property(e => e.ArchMaterial).HasMaxLength(50);
    //      entity.Property(e => e.BridgeLocation).HasMaxLength(50);
    //     entity.Property(e => e.BridgeNumber).HasMaxLength(50);
    //     entity.Property(e => e.ClearanceAboveWater).HasMaxLength(50);
    //     entity.Property(e => e.ClearancePortal).HasMaxLength(50);
    //     entity.Property(e => e.ClearanceRoadway).HasMaxLength(50);
    //     entity.Property(e => e.ClearanceSidewalkLeft).HasMaxLength(50);
    //    entity.Property(e => e.ClearanceSidewalkRight).HasMaxLength(50);
    //    entity.Property(e => e.ClearanceStreamble).HasMaxLength(50);
    //   entity.Property(e => e.ConstructionDate).HasColumnType("date");
    //    entity.Property(e => e.County).HasMaxLength(50);
    //     entity.Property(e => e.CrossingName).HasMaxLength(50);
    //     entity.Property(e => e.DateComplete).HasColumnType("datetime");
    //     entity.Property(e => e.FacilityType).HasMaxLength(50);
    //    entity.Property(e => e.FedForestHighway).HasMaxLength(50);
    //    entity.Property(e => e.FederalAid).HasMaxLength(50);
    //    entity.Property(e => e.FloorMaterial).HasMaxLength(50);
    //    entity.Property(e => e.FunctionalClass).HasMaxLength(50);
    //    entity.Property(e => e.GovIdownership)
    //  .HasMaxLength(50)
    //     .HasColumnName("GovIDOwnership");
    //      entity.Property(e => e.MaintOrg).HasMaxLength(50);
    //       entity.Property(e => e.MedianType).HasMaxLength(50);
    //    entity.Property(e => e.MilesOfNewRoad).HasColumnType("decimal(6, 2)");
    //    entity.Property(e => e.NatureOfChange).HasMaxLength(50);
    //    entity.Property(e => e.Nhs)
    //        .HasMaxLength(50)
    //        .HasColumnName("NHS");
    //   entity.Property(e => e.PostedLoadLimits).HasMaxLength(50);
    //    entity.Property(e => e.ReportDate).HasColumnType("datetime");
    //    entity.Property(e => e.ReverseLanes).HasMaxLength(50);
    //   entity.Property(e => e.SpecialSys).HasMaxLength(50);
    //    entity.Property(e => e.StationFrom).HasColumnType("decimal(6, 2)");
    //   entity.Property(e => e.StationTo).HasColumnType("decimal(6, 2)");
    //   entity.Property(e => e.SubMaterial).HasMaxLength(50);
    //    entity.Property(e => e.SuperMaterial).HasMaxLength(50);
    //    entity.Property(e => e.TotalLength).HasMaxLength(50);
    //    entity.Property(e => e.TruckRoute).HasMaxLength(50);
    //   entity.Property(e => e.UserId).HasColumnName("UserID");
    //    entity.Property(e => e.WhomBuilt).HasMaxLength(50);
    //     entity.Property(e => e.WvlegalClass)
    //      .HasMaxLength(50)
    //        .HasColumnName("WVLegalClass");
    //    entity.Property(e => e.YearOfSurvey).HasColumnType("date");
    //    });

    //    modelBuilder.Entity<UnitAsBuilt>(entity =>
    //   {
    //       entity.ToTable("UnitAsBuilt");

    //    entity.Property(e => e.Id).HasColumnName("ID");
    //    entity.Property(e => e.SubmissionId).HasColumnName("SubmissionID");
    //   entity.Property(e => e.SubmissionIduab).HasColumnName("SubmissionIDUAB");
    //    entity.Property(e => e.SurfaceType1).HasMaxLength(50);
    //   entity.Property(e => e.SurfaceType2).HasMaxLength(50);
    //   entity.Property(e => e.SurfaceType3).HasMaxLength(50);
    //   });

    // modelBuilder.Entity<UnitAsRetired>(entity =>
    //  {
    //      entity.ToTable("UnitAsRetired");

    //   entity.Property(e => e.Id).HasColumnName("ID");
    //   entity.Property(e => e.SubmissionId).HasColumnName("SubmissionID");
    //   entity.Property(e => e.SubmissionIduar).HasColumnName("SubmissionIDUAR");
    //  entity.Property(e => e.SurfaceType1).HasMaxLength(50);
    //   entity.Property(e => e.SurfaceType2).HasMaxLength(50);
    //   entity.Property(e => e.SurfaceType3).HasMaxLength(50);
    //  });

    // OnModelCreatingPartial(modelBuilder);
    // }

    // partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
