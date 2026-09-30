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

        // 3. Explicit Simulator keywords
        if (type.Contains("Simulator", StringComparison.OrdinalIgnoreCase) ||
            type.Contains("Mô phỏng", StringComparison.OrdinalIgnoreCase) ||
            type.Contains("FSTD", StringComparison.OrdinalIgnoreCase) ||
            type.Contains("FNPT", StringComparison.OrdinalIgnoreCase) ||
            type.Equals("SIM", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Simulator", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Mô phỏng", StringComparison.OrdinalIgnoreCase) ||
            code.StartsWith("SIM", StringComparison.OrdinalIgnoreCase) ||
            code.StartsWith("FSTD", StringComparison.OrdinalIgnoreCase))
        {
            return TrainingType.Simulator;
        }

        // 4. Explicit Flight Training keywords (exact or compound, avoiding single substring 'Air')
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
            code.StartsWith("FLT", StringComparison.OrdinalIgnoreCase) ||
            code.StartsWith("FLY", StringComparison.OrdinalIgnoreCase))
        {
            return TrainingType.Flight;
        }

        // 5. Practical / Thực hành / Workshop / Skill classification
        // If SubjectType is Practical/Thực hành without explicit theory precedence:
        if (type.Contains("Practical", StringComparison.OrdinalIgnoreCase) ||
            type.Contains("Thực hành", StringComparison.OrdinalIgnoreCase) ||
            type.Contains("Thực tập", StringComparison.OrdinalIgnoreCase) ||
            type.Contains("Skill", StringComparison.OrdinalIgnoreCase) ||
            type.Contains("Workshop", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Thực hành", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Practical", StringComparison.OrdinalIgnoreCase))
        {
            // If practical name/code specifies flight markers -> Flight
            if (name.Contains("Bay", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Flight", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Solo", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("PIC", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Dual", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("VFR", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Navigation", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Circuit", StringComparison.OrdinalIgnoreCase) ||
                code.StartsWith("PPL", StringComparison.OrdinalIgnoreCase) ||
                code.StartsWith("CPL", StringComparison.OrdinalIgnoreCase))
            {
                return TrainingType.Flight;
            }

            // Otherwise, default practical training (cockpit procedures, MCC, generic practical checklists) to Simulator
            return TrainingType.Simulator;
        }

        // 6. Default to Theory
        return TrainingType.Theory;
    }
}
