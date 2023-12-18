
using PJ103V3.Models.DB;

namespace OM_79_HUB.DTOs
{
    public class AclvlDTOs
    {
        public int ID { get; set; }

        public int? SubmissionID { get; set; }

   //     public string? NumberOfSpans { get; set; }

    //    public decimal? LengthOfSpan1 { get; set; }

    //    public decimal? LengthOfSpan2 { get; set; }

    //    public decimal? LengthOfSpan3 { get; set; }

   //     public decimal? LengthOfSpan4 { get; set; }

     //   public decimal? LengthOfSpan5 { get; set; }

    //    public decimal? LengthOfSpan6 { get; set; }

     //   public decimal? LengthOfSpan7 { get; set; }

    //    public decimal? LengthOfSpan8 { get; set; }

    //    public decimal? LengthOfSpan9 { get; set; }

    //    public decimal? LengthOfSpan10 { get; set; }

    //    public decimal? LengthOfSpan11 { get; set; }

     //   public decimal? LengthOfSpan12 { get; set; }

      //  public decimal? LengthOfSpan13 { get; set; }

    //    public decimal? LengthOfSpan14 { get; set; }

     //   public decimal? LengthOfSpan15 { get; set; }

     //   public decimal? LengthOfSpan16 { get; set; }

     //   public decimal? LengthOfSpan17 { get; set; }

     //   public decimal? LengthOfSpan18 { get; set; }

    //    public decimal? LengthOfSpan19 { get; set; }

      //  public decimal? LengthOfSpan20 { get; set; }

     //   public decimal? LengthOfSpan21 { get; set; }

     //   public decimal? LengthOfSpan22 { get; set; }

    //    public decimal? LengthOfSpan23 { get; set; }

    //    public decimal? LengthOfSpan24 { get; set; }

     //   public decimal? LengthOfSpan25 { get; set; }

     //   public decimal? LengthOfSpan26 { get; set; }

     //   public decimal? LengthOfSpan27 { get; set; }

     //   public decimal? LengthOfSpan28 { get; set; }

     //   public decimal? LengthOfSpan29 { get; set; }

    //    public decimal? LengthOfSpan30 { get; set; }

     //   public string? TypeOfSpan1 { get; set; }

     //   public string? TypeOfSpan2 { get; set; }

    //    public string? TypeOfSpan3 { get; set; }

    //    public string? TypeOfSpan4 { get; set; }

     //   public string? TypeOfSpan5 { get; set; }

     //   public string? TypeOfSpan6 { get; set; }

    //    public string? TypeOfSpan7 { get; set; }

     //   public string? TypeOfSpan8 { get; set; }

     //   public string? TypeOfSpan9 { get; set; }

     //   public string? TypeOfSpan10 { get; set; }

     //   public string? TypeOfSpan11 { get; set; }

     //   public string? TypeOfSpan12 { get; set; }

     //   public string? TypeOfSpan13 { get; set; }

     //   public string? TypeOfSpan14 { get; set; }

     //   public string? TypeOfSpan15 { get; set; }

     //   public string? TypeOfSpan16 { get; set; }

     //   public string? TypeOfSpan17 { get; set; }

     //   public string? TypeOfSpan18 { get; set; }

     //   public string? TypeOfSpan19 { get; set; }

      //  public string? TypeOfSpan20 { get; set; }

     //   public string? TypeOfSpan21 { get; set; }

     //   public string? TypeOfSpan22 { get; set; }

      //  public string? TypeOfSpan23 { get; set; }

      //  public string? TypeOfSpan24 { get; set; }

     //   public string? TypeOfSpan25 { get; set; }

      //  public string? TypeOfSpan26 { get; set; }

     //   public string? TypeOfSpan27 { get; set; }

      //  public string? TypeOfSpan28 { get; set; }

     //   public string? TypeOfSpan29 { get; set; }

      //  public string? TypeOfSpan30 { get; set; }

     //   public int? SubmissionIDSPAN { get; set; }
        //End of Span Table

        public int SUSubmissionID { get; set; }

        public int ProjectKey { get; set; }

        public DateTime? ReportDate { get; set; }

        public string County { get; set; } = null!;

        public int? RouteNumber { get; set; }

        public int? SubRouteNumber { get; set; }

        public string? ProjectNumber { get; set; }

        public DateTime? DateComplete { get; set; }

        public string? NatureOfChange { get; set; }

        public decimal? MilesOfNewRoad { get; set; }

        public string? MaintOrg { get; set; }

     //   public int? YearOfSurvey { get; set; }

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

        public string? UserID { get; set; }

        public string? OtherBox { get; set; }
        //End of Submissions Table

        public int UABID { get; set; }

        public int? UABSubmissionID { get; set; }

        public string? SurfaceType1 { get; set; }

        public string? SurfaceType2 { get; set; }

        public string? SurfaceType3 { get; set; }

        public int? Depth { get; set; }

        public int? SurfaceWidth { get; set; }

        public int? GradeWidth { get; set; }

        public int? YearBuilt { get; set; }

        public int? SubmissionIDUAB { get; set; }
        //End of UnitAsBuilt Table

       public int Id { get; set; }
        public decimal? StationBeginMP { get; set; }
        public decimal? StationEndMP { get; set; }
       
        public string? SignSystem { get; set; }
        public decimal? StartingMilePoint { get; set; }
        public decimal? EndingMilePoint { get; set; }
        public string? RailroadInv { get; set; }
        public string? BridgeInv { get; set; }
        public decimal? SurfaceBeginMile { get; set; }
        public decimal? SurfaceEndMile { get; set; }
        public string? BridgeSurfaceType { get; set; }
        public int? BridgeWidth { get; set; }
        public decimal? RailRoadMP { get; set; }
        public string? BridgeName { get; set; }
        public int? RailKey { get; set; }
        public string? SurfaceTypeN { get; set; }



        // public Span ToSpan()
        //  {
        //    return new Span
        //    {
        //SpanID = this.SpanID
        //      ID = this.ID,
        //      SubmissionID = this.SubmissionID,
        //     NumberOfSpans = this.NumberOfSpans,
        //      LengthOfSpan1 = this.LengthOfSpan1,
        //      LengthOfSpan2 = this.LengthOfSpan2,
        //     LengthOfSpan3 = this.LengthOfSpan3,
        //     LengthOfSpan4 = this.LengthOfSpan4,
        //     LengthOfSpan5 = this.LengthOfSpan5,
        //     LengthOfSpan6 = this.LengthOfSpan6,
        //    LengthOfSpan7 = this.LengthOfSpan7,
        //    LengthOfSpan8 = this.LengthOfSpan8,
        //    LengthOfSpan9 = this.LengthOfSpan9,
        //    LengthOfSpan10 = this.LengthOfSpan10,
        //    LengthOfSpan11 = this.LengthOfSpan11,
        //    LengthOfSpan12 = this.LengthOfSpan12,
        //   LengthOfSpan13 = this.LengthOfSpan13,
        //   LengthOfSpan14 = this.LengthOfSpan14,
        //   LengthOfSpan15 = this.LengthOfSpan15,
        //   LengthOfSpan16 = this.LengthOfSpan16,
        //   LengthOfSpan17 = this.LengthOfSpan17,
        //   LengthOfSpan18 = this.LengthOfSpan18,
        //    LengthOfSpan19 = this.LengthOfSpan19,
        //   LengthOfSpan20 = this.LengthOfSpan20,
        //    LengthOfSpan21 = this.LengthOfSpan21,
        //    LengthOfSpan22 = this.LengthOfSpan22,
        //    LengthOfSpan23 = this.LengthOfSpan23,
        //    LengthOfSpan24 = this.LengthOfSpan24,
        //    LengthOfSpan25 = this.LengthOfSpan25,
        //    LengthOfSpan26 = this.LengthOfSpan26,
        //     LengthOfSpan27 = this.LengthOfSpan27,
        //     LengthOfSpan28 = this.LengthOfSpan28,
        //     LengthOfSpan29 = this.LengthOfSpan29,
        //     LengthOfSpan30 = this.LengthOfSpan30,
        //    TypeOfSpan1 = this.TypeOfSpan1,
        //     TypeOfSpan2 = this.TypeOfSpan2,
        //    TypeOfSpan3 = this.TypeOfSpan3,
        //    TypeOfSpan4 = this.TypeOfSpan4,
        //     TypeOfSpan5 = this.TypeOfSpan5,
        //    TypeOfSpan6 = this.TypeOfSpan6,
        //    TypeOfSpan7 = this.TypeOfSpan7,
        //    TypeOfSpan8 = this.TypeOfSpan8,
        //    TypeOfSpan9 = this.TypeOfSpan9,
        //     TypeOfSpan10 = this.TypeOfSpan10,
        //    TypeOfSpan11 = this.TypeOfSpan11,
        //     TypeOfSpan12 = this.TypeOfSpan12,
        //     TypeOfSpan13 = this.TypeOfSpan13,
        //      TypeOfSpan14 = this.TypeOfSpan14,
        //     TypeOfSpan15 = this.TypeOfSpan15,
        //      TypeOfSpan16 = this.TypeOfSpan16,
        //       TypeOfSpan17 = this.TypeOfSpan17,
        //      TypeOfSpan18 = this.TypeOfSpan18,
        //      TypeOfSpan19 = this.TypeOfSpan19,
        //      TypeOfSpan20 = this.TypeOfSpan20,
        //      TypeOfSpan21 = this.TypeOfSpan21,
        //      TypeOfSpan22 = this.TypeOfSpan22,
        //       TypeOfSpan23 = this.TypeOfSpan23,
        //       TypeOfSpan24 = this.TypeOfSpan24,
        //       TypeOfSpan25 = this.TypeOfSpan25,
        //       TypeOfSpan26 = this.TypeOfSpan26,
        //       TypeOfSpan27 = this.TypeOfSpan27,
        //       TypeOfSpan28 = this.TypeOfSpan28,
        //       TypeOfSpan29 = this.TypeOfSpan29,
        //       TypeOfSpan30 = this.TypeOfSpan30,
        //       SubmissionIDSPAN = this.SubmissionIDSPAN,
        //   };
        //   }
        public Submission ToSubmission()
        {
                return new Submission
                {
                    SubmissionID = this.SUSubmissionID,
                    ProjectKey = this.ProjectKey,
                    ReportDate = this.ReportDate,
                    County = this.County,
                    RouteNumber = this.RouteNumber,
                    SubRouteNumber = this.SubRouteNumber,
                    ProjectNumber = this.ProjectNumber,
                    DateComplete = this.DateComplete,
                    NatureOfChange = this.NatureOfChange,
                  //  MilesOfNewRoad = this.MilesOfNewRoad,
                    MaintOrg = this.MaintOrg,
              //      YearOfSurvey = this.YearOfSurvey,
               //     AccessControl = this.AccessControl,
               //     ThroughLanes = this.ThroughLanes,
               //     CounterPeakLanes = this.CounterPeakLanes,
                //    PeakLanes = this.PeakLanes,
               //     ReverseLanes = this.ReverseLanes,
                //    LaneWidth = this.LaneWidth,
                //    MedianWidth = this.MedianWidth,
              //      PavementWidth = this.PavementWidth,
              //      SpecialSys = this.SpecialSys,
              //      FacilityType = this.FacilityType,
             //       FederalAid = this.FederalAid,
              //      FedForestHighway = this.FedForestHighway,
              //      MedianType = this.MedianType,
            //        NHS = this.NHS,
              //      TruckRoute = this.TruckRoute,
              //      GovIDOwnership = this.GovIDOwnership,
              //      WVlegalClass = this.WVlegalClass,
              //      FunctionalClass = this.FunctionalClass,
               //     BridgeNumber = this.BridgeNumber,
               //     BridgeLocation = this.BridgeLocation,
               //     StationFrom = this.StationFrom,
              //      StationTo = this.StationTo,
              //      CrossingName = this.CrossingName,
             //       WeightLimit = this.WeightLimit,
              //      SubMaterial = this.SubMaterial,
              //      SuperMaterial = this.SuperMaterial,
              //      FloorMaterial = this.FloorMaterial,
             //       ArchMaterial = this.ArchMaterial,
             //       TotalLength = this.TotalLength,
             //       ClearanceRoadway = this.ClearanceRoadway,
             //       ClearanceSidewalkRight = this.ClearanceSidewalkRight,
             //       ClearanceSidewalkLeft = this.ClearanceSidewalkLeft,
             //       ClearanceStreamble = this.ClearanceStreamble,
            //        ClearancePortal = this.ClearancePortal,
            //        ClearanceAboveWater = this.ClearanceAboveWater,
            //        PostedLoadLimits = this.PostedLoadLimits,
            //        ConstructionDate = this.ConstructionDate,
             //       WhomBuilt = this.WhomBuilt,
             //       HistoricalBridge = this.HistoricalBridge,
                    UserID = this.UserID,
                    SignSystem = this.SignSystem,
                    OtherBox = this.OtherBox,
                    StartingMilePoint = this.StartingMilePoint,
                    EndingMilePoint = this.EndingMilePoint,
                    RailroadInv = this.RailroadInv,
                    BridgeInv = this.BridgeInv,

                };
                }
        public RouteInfo ToRouteInfo()
        {
            return new RouteInfo
            {
                ID = this.UABID,
                SubmissionID = this.SubmissionID,
                SurfaceType1 = this.SurfaceType1,
                //SurfaceType2 = this.SurfaceType2,
              //  SurfaceType3 = this.SurfaceType3,
                Depth = this.Depth,
                SurfaceWidth = this.SurfaceWidth,
                GradeWidth = this.GradeWidth,
                YearBuilt = this.YearBuilt,
                SubmissionIDUAB = this.SubmissionIDUAB,
                SurfaceBeginMile  = this.SurfaceBeginMile,
                SurfaceEndMile = this.SurfaceEndMile,
                   AccessControl = this.AccessControl,
                    ThroughLanes = this.ThroughLanes,
                    CounterPeakLanes = this.CounterPeakLanes,
                   PeakLanes = this.PeakLanes,
                    ReverseLanes = this.ReverseLanes,
                  LaneWidth = this.LaneWidth,
                   MedianWidth = this.MedianWidth,
                     PavementWidth = this.PavementWidth,
                     SpecialSys = this.SpecialSys,
                     FacilityType = this.FacilityType,
                      FederalAid = this.FederalAid,
                    FedForestHighway = this.FedForestHighway,
                     MedianType = this.MedianType,
                       NHS = this.NHS,
                     TruckRoute = this.TruckRoute,
                     GovIDOwnership = this.GovIDOwnership,
                     WVlegalClass = this.WVlegalClass,
                    FunctionalClass = this.FunctionalClass,
                    SurfaceTypeN = this.SurfaceTypeN,

            };
            }
        public BridgeRR ToBridgeRR()
        {
            return new BridgeRR
            {
                Id = this.Id,
                BridgeNumber= this.BridgeNumber,
              //  UserID = this.UserID,
                StationBeginMP = this.StationBeginMP,
                StationEndMP = this.StationEndMP,
                CrossingName = this.CrossingName,
                BridgeSurfaceType = this.BridgeSurfaceType,
                BridgeWidth= this.BridgeWidth,
                RailRoadMP= this.RailRoadMP,
                BridgeName= this.BridgeName,
                RailKey= this.SubmissionID,



            };
        }
        
    }
}
