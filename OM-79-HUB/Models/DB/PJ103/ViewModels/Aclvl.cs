using PJ103V3.Models.DB;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Net.Mail;

namespace PJ103V3.Models.ViewModels
{
    public class Aclvl
    {
        public int ID { get; set; }

        public int? SubmissionID { get; set; }
        public int? OM79ID { get; set; }
        //   [DisplayName("Number of Spans")]
        //    public string? NumberOfSpans { get; set; }

        //    public decimal? LengthOfSpan1 { get; set; }

        //    public decimal? LengthOfSpan2 { get; set; }

        //    public decimal? LengthOfSpan3 { get; set; }

        //   public decimal? LengthOfSpan4 { get; set; }

        //    public decimal? LengthOfSpan5 { get; set; }

        //    public decimal? LengthOfSpan6 { get; set; }

        //   public decimal? LengthOfSpan7 { get; set; }

        //    public decimal? LengthOfSpan8 { get; set; }

        //   public decimal? LengthOfSpan9 { get; set; }

        //   public decimal? LengthOfSpan10 { get; set; }

        //   public decimal? LengthOfSpan11 { get; set; }

        //   public decimal? LengthOfSpan12 { get; set; }

        //   public decimal? LengthOfSpan13 { get; set; }

        //  public decimal? LengthOfSpan14 { get; set; }

        //   public decimal? LengthOfSpan15 { get; set; }

        //  public decimal? LengthOfSpan16 { get; set; }

        //   public decimal? LengthOfSpan17 { get; set; }

        //   public decimal? LengthOfSpan18 { get; set; }

        //  public decimal? LengthOfSpan19 { get; set; }

        //  public decimal? LengthOfSpan20 { get; set; }

        //  public decimal? LengthOfSpan21 { get; set; }

        //  public decimal? LengthOfSpan22 { get; set; }

        //   public decimal? LengthOfSpan23 { get; set; }

        //  public decimal? LengthOfSpan24 { get; set; }

        //    public decimal? LengthOfSpan25 { get; set; }

        //    public decimal? LengthOfSpan26 { get; set; }

        //    public decimal? LengthOfSpan27 { get; set; }

        //    public decimal? LengthOfSpan28 { get; set; }

        //    public decimal? LengthOfSpan29 { get; set; }

        //   public decimal? LengthOfSpan30 { get; set; }

        //    public string? TypeOfSpan1 { get; set; }

        //    public string? TypeOfSpan2 { get; set; }

        //    public string? TypeOfSpan3 { get; set; }

        //   public string? TypeOfSpan4 { get; set; }

        //    public string? TypeOfSpan5 { get; set; }

        //    public string? TypeOfSpan6 { get; set; }

        //    public string? TypeOfSpan7 { get; set; }

        //    public string? TypeOfSpan8 { get; set; }

        //    public string? TypeOfSpan9 { get; set; }

        //    public string? TypeOfSpan10 { get; set; }

        //   public string? TypeOfSpan11 { get; set; }

        //   public string? TypeOfSpan12 { get; set; }

        //    public string? TypeOfSpan13 { get; set; }

        //   public string? TypeOfSpan14 { get; set; }

        //    public string? TypeOfSpan15 { get; set; }

        //    public string? TypeOfSpan16 { get; set; }

        //    public string? TypeOfSpan17 { get; set; }

        //    public string? TypeOfSpan18 { get; set; }

        //    public string? TypeOfSpan19 { get; set; }

        //   public string? TypeOfSpan20 { get; set; }

        //     public string? TypeOfSpan21 { get; set; }

        //     public string? TypeOfSpan22 { get; set; }

        //     public string? TypeOfSpan23 { get; set; }

        //    public string? TypeOfSpan24 { get; set; }

        //     public string? TypeOfSpan25 { get; set; }

        //     public string? TypeOfSpan26 { get; set; }

        //     public string? TypeOfSpan27 { get; set; }

        //    public string? TypeOfSpan28 { get; set; }

        //     public string? TypeOfSpan29 { get; set; }

        //     public string? TypeOfSpan30 { get; set; }

        //    public int? SubmissionIDSPAN { get; set; }
        //End of Span Table

        public int SUSubmissionID { get; set; }

        public int ProjectKey { get; set; }
        [DisplayName("Report Date")]
        public DateTime? ReportDate { get; set; }
       [Required(ErrorMessage = "The County Field is Required")]
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
        [Required]
        public string? NatureOfChange { get; set; }

        public decimal? MilesOfNewRoad { get; set; }
        [DisplayName("Maintenance Org")]
        
        public string? MaintOrg { get; set; }
       // [DisplayName("Year of Survey/Change")]
        
       // public int? YearOfSurvey { get; set; }
        [DisplayName("Access Control")]
        public string? AccessControl { get; set; }
        [DisplayName("Through Lanes")]
        public int? ThroughLanes { get; set; }
        [DisplayName("Counter Peak Lanes")]
        public int? CounterPeakLanes { get; set; }
        [DisplayName("Peak Lanes")]
        public int? PeakLanes { get; set; }
        [DisplayName("Reversible Lanes")]
        public string? ReverseLanes { get; set; }
        [DisplayName("Lane Width (ft.) (round to whole number)")]
        public int? LaneWidth { get; set; }
        [DisplayName("Median Width (ft.) (round to whole number)")]
        
        public int? MedianWidth { get; set; }
        [DisplayName("Roadway Width (ft.) (round to whole number)")]
        
        public int? PavementWidth { get; set; }
        [DisplayName("Special System")]
        public string? SpecialSys { get; set; }
        [DisplayName("Facility Type")]
        public string? FacilityType { get; set; }
        [DisplayName("Federal Aid")]
        public string? FederalAid { get; set; }
        [DisplayName("Federal Forest Highway")]
        public string? FedForestHighway { get; set; }
        [DisplayName("Median Type")]
        public string? MedianType { get; set; }

        public string? NHS { get; set; }
        [DisplayName("Truck Route")]
        public string? TruckRoute { get; set; }
        [DisplayName("Gov ID Ownership")]
        public string? GovIDOwnership { get; set; }
        [DisplayName("WV Functional Class")]
        public string? WVlegalClass { get; set; }
        [DisplayName("Federal Functional Class")]
        public string? FunctionalClass { get; set; }
        [DisplayName("Bridge Number (BARS)")]
        public string? BridgeNumber { get; set; }
        [DisplayName("Bridge Location")]
        public string? BridgeLocation { get; set; }
        [DisplayName("Bridge Starting MP")]
        public decimal? StationBeginMP { get; set; }
        [DisplayName("Bridge Ending MP")]
        public decimal? StationEndMP { get; set; }
        [DisplayName("Crossing Name")]
        public string? CrossingName { get; set; }
        [DisplayName("Weight Limit/Posted Load Limit")]
        public int? WeightLimit { get; set; }
        [DisplayName("Sub Material")]
        public string? SubMaterial { get; set; }
        [DisplayName("Super Material")]
        public string? SuperMaterial { get; set; }
        [DisplayName("Floor Material")]
        public string? FloorMaterial { get; set; }
        [DisplayName("Arch Material")]
        public string? ArchMaterial { get; set; }
        [DisplayName("Total Length")]
        public string? TotalLength { get; set; }
        [DisplayName("Clearance Roadway")]
        public string? ClearanceRoadway { get; set; }
        [DisplayName("Clearance Sidewalk Right")]
        public string? ClearanceSidewalkRight { get; set; }
        [DisplayName("Clearance Sidewalk Left")]
        public string? ClearanceSidewalkLeft { get; set; }
        [DisplayName("Clearance Surface to Streambed")]
        public string? ClearanceStreamble { get; set; }
        [DisplayName("Clearance Portal")]
        public string? ClearancePortal { get; set; }
        [DisplayName("Clearance Above Water")]
        public string? ClearanceAboveWater { get; set; }
        [DisplayName("Posted Load Limits")]
        public string? PostedLoadLimits { get; set; }
        [DisplayName("Bridge Built Date")]
        public DateTime? ConstructionDate { get; set; }
        [DisplayName("Contractor")]
        public string? WhomBuilt { get; set; }
        [DisplayName("Historical Bridge")]
        public bool? HistoricalBridge { get; set; }
        [DisplayName("Bridge Name")]
        public string? UserID { get; set; }
        [DisplayName("Additional Comments")]
        public string? OtherBox { get; set; }
        //End of Submissions Table

        public int UABID { get; set; }

        public int? UABSubmissionID { get; set; }

        public string? SurfaceType1 { get; set; }

        public string? SurfaceType2 { get; set; }

        public string? SurfaceType3 { get; set; }

        public int? Depth { get; set; }

        public int? SurfaceWidth { get; set; }
        [DisplayName("Grade Width (ft.) (round to whole number)")]
        public int? GradeWidth { get; set; }

        public int? YearBuilt { get; set; }
        public int Id { get; set; }
        

        public int? SubmissionIDUAB { get; set; }
        [DisplayName("Starting MP")]
       
        public decimal? StartingMilePoint { get; set; }
        [DisplayName("Ending MP")]
        
        public decimal? EndingMilePoint { get; set; }
        [DisplayName("Sign System")]
        
        public string? SignSystem { get; set; }
        [DisplayName("Is There a Bridge?")]
        [Required]
        public string? BridgeInv { get; set; }
        [DisplayName("Is There a Railroad Crossing?")]
        [Required]
        public string? RailroadInv { get; set; }
        [DisplayName("Bridge Surface Type")]
        public string? BridgeSurfaceType { get; set; }
        [DisplayName("Bridge Roadway Width (ft.) (round to whole number)")]
        public int? BridgeWidth { get; set; }
        [DisplayName("Crossing MP")]
        public decimal? RailRoadMP { get; set; }
        [DisplayName("Bridge Name")]
        public string? BridgeName { get; set; }
        public int? RailKey { get; set; }
        [DisplayName("Surface Type")]
        public string? SurfaceTypeN { get; set; }
        //End of UnitAsBuilt Table
        public decimal? MPSegmentStart { get; set; }
        public decimal? MPSegmentEnd { get; set; }


        public int AttachmentID { get; set; }
        public string? FileName { get; set; }
        public string? FilePath { get; set; }

       
        public virtual Span? Span { get; set; }
        public virtual Submission? Submission { get; set; }
        public virtual RouteInfo? RouteInfo { get; set; }
        public virtual BridgeRR? BridgeRR { get; set; }
        public virtual Attachments? Attachments { get; set; }




    }
}
