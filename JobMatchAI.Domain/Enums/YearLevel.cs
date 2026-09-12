using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace JobMatchAI.Domain.Enums
{
    public enum YearLevel
    {
        [Display(Name = "Graduating")]
        Graduating = 1,

        [Display(Name = "Alumni")]
        Alumni = 2
    }
}
