using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PJ103V3.Models.DB;

public partial class RouteInfo
{
    [Key]
    public int ID { get; set; }

    public int? SubmissionID { get; set; }

    public string? SurfaceType1 { get; set; }

    public int? Depth { get; set; }

    public int? SurfaceWidth { get; set; }

    public int? GradeWidth { get; set; }

    public int? YearBuilt { get; set; }

    public int? SubmissionIDUAB { get; set; }
    public decimal? SurfaceBeginMile { get; set; }
    public decimal? SurfaceEndMile { get; set; }
    public string? AccessControl { get; set; }

    public int? ThroughLanes { get; set; }

    public int? CounterPeakLanes { get; set; }

    public int? PeakLanes { get; set; }

    public string? ReverseLanes { get; set; }

    public int? LaneWidth { get; set; }
    
    public int? MedianWidth { get; set; }
    
    public int? PavementWidth { get; set; }

    public string? SpecialSys { get; set; }

    public string? FacilityType { get; set; }

    public string? FederalAid { get; set; }

    public string? FedForestHighway { get; set; }

    public string? MedianType { get; set; }

    public string? NHS { get; set; }

    public string? TruckRoute { get; set; }

    public string? GovIDOwnership { get; set; }

    public string? WVlegalClass { get; set; }

    public string? FunctionalClass { get; set; }
    public string? SurfaceTypeN { get; set; }
    [RegularExpression(@"^\d{1,4}(\.\d{1,3})?$", ErrorMessage = "Invalid format. Must have up to 4 digits before the decimal point and up to 3 digits after.")]
    [DisplayName("Starting MP")]
    [Required]
    [Column(TypeName = "decimal(10, 4)")]
    public decimal? MPSegmentStart { get; set; }
    [DisplayName("Ending MP")]
    [RegularExpression(@"^\d{1,4}(\.\d{1,3})?$", ErrorMessage = "Invalid format. Must have up to 4 digits before the decimal point and up to 3 digits after.")]
    [Required]
    [Column(TypeName = "decimal(10, 4)")]
    public decimal? MPSegmentEnd { get; set; }
}
