using ETR.Domain.Enums;

namespace ETR.Application.Services;

/// <summary>
/// Deterministic classification of training session types (Theory, Flight, Simulator)
/// eliminating fragile string heuristics and preventing false classifications (e.g., Air Law as Flight).
/// </summary>
public static class TrainingTypeClassifier
{
    private static readonly HashSet<string> ExplicitTheoryCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "ALW", "AIR_LAW", "AIRLAW", "LAW", "MET", "METEOROLOGY", "AGK", "NAV_THEORY",
        "HPL", "POF", "COM", "RADIO", "OPS", "FPP", "PERF", "MASS_BAL", "INSTR_THEORY"
    };

    public static TrainingType Classify(string? subjectCode, string? subjectName, string? subjectType)
    {
        string code = (subjectCode ?? string.Empty).Trim();
        string name = (subjectName ?? string.Empty).Trim();
        string type = (subjectType ?? string.Empty).Trim();

        // 1. Explicit Theory subject codes take absolute precedence
        if (!string.IsNullOrEmpty(code) && ExplicitTheoryCodes.Contains(code))
        {
            return TrainingType.Theory;
        }

        // 2. Explicit Theory keywords in name / type
        if (name.Contains("Air Law", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Luật hàng không", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Meteorology", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Khí tượng", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Lý thuyết", StringComparison.OrdinalIgnoreCase) ||
            type.Equals("Air Law", StringComparison.OrdinalIgnoreCase) ||
            type.Equals("Theory", StringComparison.OrdinalIgnoreCase) ||
            type.Equals("Lý thuyết", StringComparison.OrdinalIgnoreCase) ||
            type.Equals("Ground", StringComparison.OrdinalIgnoreCase))
        {
            return TrainingType.Theory;
        }

        // 3. Explicit Non-FSTD practical disciplines (Maintenance, Workshop, Cabin Emergency, Ground Safety Drills) -> Theory
        if (code.StartsWith("MAINT", StringComparison.OrdinalIgnoreCase) ||
            code.StartsWith("CABIN", StringComparison.OrdinalIgnoreCase) ||
            code.StartsWith("AVIONICS", StringComparison.OrdinalIgnoreCase) ||
            code.StartsWith("ENG-LAB", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Maintenance", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Bảo dưỡng", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Cabin", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Evacuation", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Fire Drill", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Smoke", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("First Aid", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Sơ cấp cứu", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Cứu hỏa", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Thoát hiểm", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Xưởng", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Workshop", StringComparison.OrdinalIgnoreCase) ||
            type.Contains("Maintenance", StringComparison.OrdinalIgnoreCase) ||
            type.Contains("Bảo dưỡng", StringComparison.OrdinalIgnoreCase) ||
            type.Contains("Cabin", StringComparison.OrdinalIgnoreCase) ||
            type.Contains("Workshop", StringComparison.OrdinalIgnoreCase) ||
            type.Contains("Xưởng", StringComparison.OrdinalIgnoreCase))
        {
            return TrainingType.Theory;
        }

        // 4. Explicit Simulator keywords (FSTD, FNPT, FFS, Simulator, Buồng lái mô phỏng, MCC)
        if (type.Contains("Simulator", StringComparison.OrdinalIgnoreCase) ||
            type.Contains("Mô phỏng", StringComparison.OrdinalIgnoreCase) ||
            type.Contains("FSTD", StringComparison.OrdinalIgnoreCase) ||
            type.Contains("FNPT", StringComparison.OrdinalIgnoreCase) ||
            type.Contains("FFS", StringComparison.OrdinalIgnoreCase) ||
            type.Equals("SIM", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Simulator", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Mô phỏng", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("FSTD", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("FNPT", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("FFS", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Buồng lái mô phỏng", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Full Flight Sim", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("MCC", StringComparison.OrdinalIgnoreCase) ||
            code.StartsWith("SIM", StringComparison.OrdinalIgnoreCase) ||
            code.StartsWith("FSTD", StringComparison.OrdinalIgnoreCase) ||
            code.StartsWith("FNPT", StringComparison.OrdinalIgnoreCase) ||
            code.Contains("-SIM", StringComparison.OrdinalIgnoreCase))
        {
            return TrainingType.Simulator;
        }

        // 5. Explicit Flight Training keywords (Aircraft Flight, Circuits, Touch-and-Go, Solo, Dual, Cross-Country)
        if (type.Equals("Flight", StringComparison.OrdinalIgnoreCase) ||
            type.Equals("Bay", StringComparison.OrdinalIgnoreCase) ||
            type.Contains("Flight Training", StringComparison.OrdinalIgnoreCase) ||
            type.Contains("Flight Practice", StringComparison.OrdinalIgnoreCase) ||
            type.Contains("Thực hành bay", StringComparison.OrdinalIgnoreCase) ||
            type.Contains("Huấn luyện bay", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Flight Training", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Flight Practice", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Thực hành bay", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Huấn luyện bay", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Bay vòng kín", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Bay đường dài", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Bay đêm", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Touch-and-Go", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Base Flight Training", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Solo", StringComparison.OrdinalIgnoreCase) ||
            code.StartsWith("FLT", StringComparison.OrdinalIgnoreCase) ||
            code.StartsWith("FLY", StringComparison.OrdinalIgnoreCase) ||
            code.Contains("-FLT", StringComparison.OrdinalIgnoreCase))
        {
            return TrainingType.Flight;
        }

        // 6. Generic Practical / Thực hành / Skill without explicit Flight/SIM keywords
        // Non-flight, non-simulator practical training (workshops, classroom labs) defaults safely to Theory
        return TrainingType.Theory;
    }
}
