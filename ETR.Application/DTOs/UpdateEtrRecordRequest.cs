namespace ETR.Application.DTOs;

public record UpdateEtrRecordRequest(
    string Status,
    bool IsLocked
);