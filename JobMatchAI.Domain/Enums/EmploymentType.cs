using System.ComponentModel.DataAnnotations;

namespace JobMatchAI.Domain.Enums
{
    public enum EmploymentType
    {
        [Display(Name = "Full Time")]
        FullTime = 1,

        [Display(Name = "Part Time")]
        PartTime = 2,

        [Display(Name = "Internship")]
        Internship = 3,

        [Display(Name = "Contract")]
        Contract = 4,

        [Display(Name = "Freelance")]
        Freelance = 5
    }
}
