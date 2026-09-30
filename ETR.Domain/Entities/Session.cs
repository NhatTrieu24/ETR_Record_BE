using ETR.Domain.Enums;

namespace ETR.Domain.Entities;

public class Session : BaseEntity
{
    public int SessionId { get; set; }
    public int ClassId { get; set; }
    public int SubjectId { get; set; }
    public string SessionTitle { get; set; } = string.Empty;
    public DateTime? SessionDate { get; set; }
    public string? Location { get; set; }
    public bool IsConfirmed { get; set; }
    public int? ConfirmedByAccountId { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public bool IsAssessmentRequired { get; set; } = false;
    public bool IsChecklistRequired { get; set; } = false;
    public int? AssessmentId { get; set; }
    public int? PracticalChecklistId { get; set; }

    /// <summary>
    /// Loại hình đào tạo của buổi học: Theory (mặc định), Flight (bay thực tế), Simulator (buồng lái mô phỏng).
    /// </summary>
    public TrainingType TrainingType { get; set; } = TrainingType.Theory;

    /// <summary>
    /// Mã bài học hoặc bài huấn luyện (ví dụ "EX-01", "SIM-03", "TH-02").
    /// </summary>
    public string? LessonCode { get; set; }
}

