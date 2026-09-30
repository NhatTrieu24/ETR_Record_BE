using ETR.Domain.Enums;

namespace ETR.Domain.Entities;

public class UserProfile : BaseEntity
{
    public int AccountId { get; set; }
    public string UserCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string? Organization { get; set; }

    /// <summary>Overall learner status (Active/Withdrawn/Graduated) — independent of any single
    /// Enrollment.Status, which tracks a specific class enrollment rather than the person overall.</summary>
    public LearnerStatus Status { get; set; } = LearnerStatus.Active;

    #region Pilot Credentials (Phase 3)
    /// <summary>Pilot License Type (e.g. SPL, PPL, CPL, ATPL, MPL, Drone)</summary>
    public string? LicenseType { get; set; }

    /// <summary>Official Pilot License Number issued by CAAV/FAA/EASA</summary>
    public string? LicenseNumber { get; set; }

    /// <summary>Expiration date of pilot license</summary>
    public DateTime? LicenseExpiryDate { get; set; }

    /// <summary>Aviation Medical Class (e.g. Class 1, Class 2, Class 3, LAPL)</summary>
    public string? MedicalClass { get; set; }

    /// <summary>Medical Certificate Expiration Date</summary>
    public DateTime? MedicalExpiryDate { get; set; }

    /// <summary>ICAO English Language Proficiency Level (1 to 6)</summary>
    public int? IcaoElpLevel { get; set; }

    /// <summary>ICAO English Language Proficiency Expiration Date (Level 6 is permanent/null)</summary>
    public DateTime? IcaoElpExpiryDate { get; set; }

    /// <summary>Aircraft Type Ratings endorsed (e.g. "A320, B737, C172")</summary>
    public string? TypeRatings { get; set; }

    /// <summary>Indicates whether credentials have been verified by Academic/Admin staff</summary>
    public bool IsCredentialsVerified { get; set; } = false;

    public int? CredentialsVerifiedByAccountId { get; set; }
    public DateTime? CredentialsVerifiedAt { get; set; }
    #endregion

    public Account Account { get; set; } = null!;
}
