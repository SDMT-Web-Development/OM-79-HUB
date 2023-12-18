using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace PJ103V3.Models.DB;


public class BridgeRR
{
    [Key]
    public int Id { get; set; }
    
    public string? BridgeNumber { get; set; }
    public decimal? StationBeginMP { get; set; }
    public decimal? StationEndMP { get; set; }
    public string? CrossingName { get; set; }
    public int? NumberOfRR { get; set; }
    public string? KindOfCrossing { get; set; }
    public string? SubMaterial { get; set; }

    public string? SuperMaterial { get; set; }

    public string? FloorMaterial { get; set; }

    public string? ArchMaterial { get; set; }

    public int? TotalLength { get; set; }

    public int? ClearanceRoadway { get; set; }

    public int? ClearanceSidewalkRight { get; set; }

    public int? ClearanceSidewalkLeft { get; set; }

    public int? ClearanceStreambed { get; set; }

    public int? ClearancePortal { get; set; }

    public int? ClearanceAboveWater { get; set; }
    public string? PostedLoadLimits { get; set; }

    public DateTime? ConstructionDate { get; set; }

    public string? WhomBuilt { get; set; }

    public bool? HistoricalBridge { get; set; }
    public string? BridgeSurfaceType { get; set; }
    public int? BridgeWidth { get; set; }
    public decimal? RailRoadMP { get; set; }
    public string? BridgeName { get; set; }
    public int? RailKey { get; set; } = null;

}

