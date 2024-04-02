﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace OM_79_HUB.Models;

public partial class OMTable
{
    [Key]
    public int Id { get; set; }
    [DisplayName("District Number")]
    [Required]
    public int? DistrictNumber { get; set; }
    [Required]
    public string? County { get; set; }

    [DisplayName("Date")]

    public DateTime? SubmissionDate { get; set; }

    public string? Routing { get; set; }
    [DisplayName("Road Change Type")]

    public string? RoadChangeType { get; set; }
    [DisplayName("If Other Please Explain")]
    public string? Otherbox { get; set; }
    [DisplayName("Route Assignment")]
    [Required]
    public string? RouteAssignment { get; set; }
    [DisplayName("Right Of Way Width")]
    [Required]
    public string? RightOfWayWidth { get; set; }
    [DisplayName("Is there a Railroad?")]
    [Required]
    public string? Railroad { get; set; }
    [DisplayName("Railroad Crossing Number")]
    public int? DOTAARNumber { get; set; }
    [DisplayName("Requested By")]
    [Required]
    public string? RequestedBy { get; set; }
    [DisplayName("Explanation")]

    public string? Comments { get; set; }
    [DisplayName("Total Adjacent Property")]
    public int? AdjacentProperty { get; set; }
    [DisplayName("Number of Houses")]
    public int? APHouses { get; set; }
    [DisplayName("Number of Businesses")]
    public int? APBusinesses { get; set; }
    [DisplayName("Number of Schools")]
    public int? APSchools { get; set; }
    [DisplayName("Number of Other")]
    public int? APOther { get; set; }
    [DisplayName("If other adjacent properties, explain.")]
    public string? APOtherIdentify { get; set; }
    [DisplayName("Description")]
    [Required]
    public string? Attachments { get; set; }

    public string? DESignature { get; set; }

    public string? Preparer { get; set; }
    [DisplayName("Requester Name")]
    public string? RequestedByName { get; set; }
    public int? Route { get; set; }
    public int? SubRoute { get; set; }
    [DisplayName("CO Date")]
    public DateTime? CoDate { get; set; }
    [DisplayName("2nd CO Date (Optional)")]
    public DateTime? CoDateTwo { get; set; }
    [DisplayName("Addition")]
    public bool RAddition { get; set; }
    [DisplayName("Redesignation")]
    public bool RRedesignation { get; set; }
    [DisplayName("Map Correction")]
    public bool RMapCorrection { get; set; }
    [DisplayName("Abandonment")]
    public bool RAbandonment { get; set; }
    [DisplayName("Inventory Removal")]
    public bool RInventoryRemoval { get; set; }
    [DisplayName("Amend")]
    public bool RAmend { get; set; }
    [DisplayName("Rescind")]
    public bool RRescind { get; set; }
    [DisplayName("Other")]
    public bool ROther { get; set; }
    [DisplayName("Please explain other ROW")]
    public string? RightOther { get; set; }


    public int? HubId { get; set; }
    [DisplayName("Sign System")]
    public string? SignSystem { get; set; }
    [DisplayName("Project Number")]
    public string? ProjectNumber { get; set; }
    [DisplayName("Route Number")]
    public int? RouteNumber { get; set; }
    [DisplayName("Subroute Number")]
    public int? SubRouteNumber { get; set; }
    [DisplayName("Date Complete")]
    public DateTime? DateComplete { get; set; }
    [DisplayName("Starting MP")]
    public decimal? StartingMilePoint {  get; set; }
    [DisplayName("Ending MP")]
    public decimal? EndingMilePoint { get; set; }
    [DisplayName("Org Number")]
    public string? MaintOrg {  get; set; }
    [DisplayName("Year Of Survey")]
    public int? YearOfSurvey {  get; set; }


}
