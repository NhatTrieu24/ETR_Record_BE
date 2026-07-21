namespace ETR.Application.Compliance;

public static class BusinessRuleEngine
{
    public const decimal MinimumAttendanceThreshold = 80.0m;

    /// <summary>Total attempts allowed per Assessment (initial attempt + retakes).</summary>
    public const int MaxAssessmentAttempts = 3;
}
