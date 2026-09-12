using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace JobMatchAI.Domain.Enums
{
    public enum PlacementStatus
    {
        [Display(Name = "Pending")]
        Pending = 1,

        [Display(Name = "Hired")]
        Hired = 2,

        [Display(Name = "Rejected")]
        Rejected = 3,

        [Display(Name = "Offer Accepted")]
        OfferAccepted = 4,

        [Display(Name = "Offer Declined")]
        OfferDeclined = 5,

        [Display(Name = "Internship")]
        Internship = 6
    }
}
