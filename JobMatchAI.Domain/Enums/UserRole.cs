using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace JobMatchAI.Domain.Enums
{
    public enum UserRole
    {
        [Display(Name = "Student")]
        Student = 1,

        [Display(Name = "Alumni")]
        Alumni = 2,

        [Display(Name = "Employer")]
        Employer = 3,

        [Display(Name = "Admin")]
        Admin = 4
        
    }
    
}
