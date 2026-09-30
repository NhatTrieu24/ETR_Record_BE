using System.ComponentModel.DataAnnotations;
using ETR.Domain.Enums;

namespace ETR.Application.DTOs;

public record UpdateAttendanceRecordRequest(
    [Required] AttendanceStatus Status,
    [MaxLength(500)] string? Remarks = null,
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
    string? StudentComments = null
);
