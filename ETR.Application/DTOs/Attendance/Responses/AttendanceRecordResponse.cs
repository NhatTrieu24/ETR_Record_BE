using ETR.Domain.Enums;

namespace ETR.Application.DTOs;

public record AttendanceRecordResponse(
    int AttendanceRecordId,
    int SessionId,
    int EnrollmentId,
    AttendanceStatus Status,
    string? Remarks,
    int RecordedByAccountId,
    DateTime RecordedAt,
    PerformanceGrade? PerformanceGrade = null,
    decimal? FlightHours = null,
    decimal? SimulatorHours = null,
    decimal? DualHours = null,
    decimal? SoloHours = null,
    decimal? PicHours = null,
    decimal? NightHours = null,
    decimal? InstrumentHours = null,
    decimal? CrossCountryHours = null,
    int? DayLandings = null,
    int? NightLandings = null,
    string? AircraftRegistration = null,
    string? SimulatorDevice = null,
    string? DepartureIcao = null,
    string? ArrivalIcao = null,
    string? Route = null,
    string? InstructorComments = null,
    string? StudentComments = null,
    DateTime? InstructorSignedAt = null,
    int? InstructorSignedByAccountId = null,
    DateTime? StudentSignedAt = null,
    int? StudentSignedByAccountId = null,
    TrainingType? SessionTrainingType = null,
    string? SessionLessonCode = null
);
