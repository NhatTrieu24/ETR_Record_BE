using ETR.Domain.Enums;

namespace ETR.Domain.Entities;

public class AttendanceRecord : BaseEntity
{
    public int AttendanceRecordId { get; set; }
    public int SessionId { get; set; }

    /// <summary>FK to CourseEnrollment.EnrollmentId. Used to point directly at the Class-Enrollment
    /// relationship instead of through the (now removed) ClassStudent indirection table — see
    /// mục #10, docs/todo/9.todo_to_complete_system.md.</summary>
    public int EnrollmentId { get; set; }
    public AttendanceStatus Status { get; set; }
    public string? Remarks { get; set; }
    public int RecordedByAccountId { get; set; }
    public DateTime RecordedAt { get; set; }

    // === Phase 2: Flight / Simulator Competency & Logbook Extensions ===

    /// <summary>
    /// Đánh giá năng lực độc lập với trạng thái điểm danh: Satisfactory, Unsatisfactory, Incomplete.
    /// </summary>
    public PerformanceGrade? PerformanceGrade { get; set; }

    /// <summary>Tổng số giờ bay thực tế (chỉ áp dụng cho Flight).</summary>
    public decimal? FlightHours { get; set; }

    /// <summary>Tổng số giờ buồng lái mô phỏng (chỉ áp dụng cho Simulator, tách biệt hoàn toàn với FlightHours).</summary>
    public decimal? SimulatorHours { get; set; }

    /// <summary>Giờ bay có giáo viên kèm (Dual instruction hours).</summary>
    public decimal? DualHours { get; set; }

    /// <summary>Giờ bay đơn (Solo hours).</summary>
    public decimal? SoloHours { get; set; }

    /// <summary>Giờ chỉ huy tàu bay (Pilot-in-Command hours).</summary>
    public decimal? PicHours { get; set; }

    /// <summary>Giờ bay ban đêm (Night hours - có thể giao với Dual/Solo/PIC).</summary>
    public decimal? NightHours { get; set; }

    /// <summary>Giờ bay bằng thiết bị / khí quyển (Instrument flight hours - có thể giao với Dual/Solo/PIC).</summary>
    public decimal? InstrumentHours { get; set; }

    /// <summary>Giờ bay đường dài / chuyển sân (Cross-country flight hours - có thể giao với Dual/Solo/PIC).</summary>
    public decimal? CrossCountryHours { get; set; }

    /// <summary>Số lần hạ cánh ban ngày.</summary>
    public int? DayLandings { get; set; }

    /// <summary>Số lần hạ cánh ban đêm.</summary>
    public int? NightLandings { get; set; }

    /// <summary>Số hiệu đăng ký máy bay thực tế (ví dụ "VN-C172").</summary>
    public string? AircraftRegistration { get; set; }

    /// <summary>Tên hoặc mã thiết bị mô phỏng (ví dụ "ALX-FNPT-II").</summary>
    public string? SimulatorDevice { get; set; }

    /// <summary>Mã sân bay khởi hành ICAO (ví dụ "VVTS").</summary>
    public string? DepartureIcao { get; set; }

    /// <summary>Mã sân bay đến ICAO (ví dụ "VVVT").</summary>
    public string? ArrivalIcao { get; set; }

    /// <summary>Hành trình bay (ví dụ "VVTS-VVVT").</summary>
    public string? Route { get; set; }

    /// <summary>Nhận xét / phản hồi từ giảng viên.</summary>
    public string? InstructorComments { get; set; }

    /// <summary>Ý kiến / phản hồi từ học viên.</summary>
    public string? StudentComments { get; set; }

    /// <summary>Thời điểm giảng viên ký số xác nhận bài huấn luyện.</summary>
    public DateTime? InstructorSignedAt { get; set; }

    /// <summary>ID tài khoản giảng viên đã ký.</summary>
    public int? InstructorSignedByAccountId { get; set; }

    /// <summary>Thời điểm học viên ký số xác nhận bài huấn luyện.</summary>
    public DateTime? StudentSignedAt { get; set; }

    /// <summary>ID tài khoản học viên đã ký.</summary>
    public int? StudentSignedByAccountId { get; set; }
}

