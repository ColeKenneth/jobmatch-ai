using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace JobMatchAI.Domain.Enums
{
    public enum Category
    {
        [Display(Name = "Technical")]
        Technical = 1,

        [Display(Name = "Soft")]
        Soft = 2,

        [Display(Name = "Language")]
        Language = 3,

        [Display(Name = "Tool")]
        Tool = 4
    }
}
