using System.ComponentModel.DataAnnotations;

namespace EnvironmentalSurvey.DTOs
{
    public class ParticipationDto
    {
        public int ParticipationId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string SeminarTitle { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime DateConducted { get; set; }
        public int? NumberOfParticipants { get; set; }
        public string? Description { get; set; }
        public string ApprovalStatus { get; set; } = string.Empty;
        public int? ApprovedBy { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateParticipationDto
    {
        [Required]
        [StringLength(200)]
        public string SeminarTitle { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Location { get; set; } = string.Empty;

        [Required]
        public DateTime DateConducted { get; set; }

        public int? NumberOfParticipants { get; set; }

        public string? Description { get; set; }
    }

    public class UpdateParticipationDto
    {
        [StringLength(200)]
        public string? SeminarTitle { get; set; }

        [StringLength(200)]
        public string? Location { get; set; }

        public DateTime? DateConducted { get; set; }

        public int? NumberOfParticipants { get; set; }

        public string? Description { get; set; }
    }
}