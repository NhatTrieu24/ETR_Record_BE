namespace ETR.Application.DTOs.Import;

public record ImportRowError(int Row, string Column, string Message);
