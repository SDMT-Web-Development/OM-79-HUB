using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Net.Mail;

namespace PJ103V3.Models.DB;

public partial class Submission
{
    [Key]
    public int SubmissionID { get; set; }

    public int ProjectKey { get; set; }
    [DisplayName("Report Date")]
    public DateTime? ReportDate { get; set; }

    public string County { get; set; } = null!;
    [DisplayName("Route Number")]
    public int? RouteNumber { get; set; }
    [DisplayName("Subroute Number")]
    public int? SubRouteNumber { get; set; }
    [DisplayName("Project Number")]
    public string? ProjectNumber { get; set; }
    [DisplayName("Date Complete")]
    public DateTime? DateComplete { get; set; }
    [DisplayName("Nature of Change")]
    public string? NatureOfChange { get; set; }

    [DisplayName("Maintenance Org")]
    public string? MaintOrg { get; set; }

    public int? YearOfSurvey { get; set; }
    public string? UserID { get; set; }

    public string? OtherBox { get; set; }
    public string? SignSystem { get; set; }
    public decimal? StartingMilePoint { get; set; }
    public decimal? EndingMilePoint { get; set; }
    public string? RailroadInv { get; set; }
    public string? BridgeInv { get; set; }


    public int? OM79Id { get; set; }
    public List<Attachments>? Files { get; set; }
    /*  public string? AccessControl { get; set; }

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

      public string? BridgeNumber { get; set; }

      public string? BridgeLocation { get; set; }

      public decimal? StationFrom { get; set; }

      public decimal? StationTo { get; set; }

      public string? CrossingName { get; set; }

      public int? WeightLimit { get; set; }

      public string? SubMaterial { get; set; }

      public string? SuperMaterial { get; set; }

      public string? FloorMaterial { get; set; }

      public string? ArchMaterial { get; set; }

      public string? TotalLength { get; set; }

      public string? ClearanceRoadway { get; set; }

      public string? ClearanceSidewalkRight { get; set; }

      public string? ClearanceSidewalkLeft { get; set; }

      public string? ClearanceStreamble { get; set; }

      public string? ClearancePortal { get; set; }

      public string? ClearanceAboveWater { get; set; }

      public string? PostedLoadLimits { get; set; }

      public DateTime? ConstructionDate { get; set; }

      public string? WhomBuilt { get; set; }

      public bool? HistoricalBridge { get; set; }

      */
}
