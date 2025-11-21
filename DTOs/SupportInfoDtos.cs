using System.ComponentModel.DataAnnotations;

namespace EnvironmentalSurvey.DTOs
{
    public class SupportInfoDto
    {
        public int SupportId { get; set; }
        public string? ContactType { get; set; }
        public string? ContactValue { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateSupportInfoDto
    {
        [StringLength(20)]
        [RegularExpression("^(phone|email|address|other)$")]
        public string? ContactType { get; set; }

        [StringLength(200)]
        public string? ContactValue { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }
    }

    public class UpdateSupportInfoDto
    {
        [StringLength(20)]
        [RegularExpression("^(phone|email|address|other)$")]
        public string? ContactType { get; set; }

        [StringLength(200)]
        public string? ContactValue { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public bool? IsActive { get; set; }
    }
}
