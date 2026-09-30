using ETR.Application.Services;
using ETR.Domain.Enums;
using Xunit;

namespace ETR.Application.Tests.Services;

public class TrainingTypeClassifierTests
{
    [Theory]
    [InlineData("ALW", "Air Law", "Theory", TrainingType.Theory)]
    [InlineData("ALW", "Luật hàng không", "Lý thuyết", TrainingType.Theory)]
    [InlineData("AIR_LAW", "Air Law Regulations", "Ground", TrainingType.Theory)]
    [InlineData("LAW101", "Aviation Regulations", "Air Law", TrainingType.Theory)]
    [InlineData("MET", "Meteorology", "Theory", TrainingType.Theory)]
    [InlineData("NAV_THEORY", "General Navigation", "Theory", TrainingType.Theory)]
    [InlineData("HPL", "Human Performance & Limitations", "Lý thuyết", TrainingType.Theory)]
    [InlineData("RADIO", "VFR Communications", "Theory", TrainingType.Theory)]
    [InlineData(null, "Luật hàng không đại cương", null, TrainingType.Theory)]
    [InlineData("GEN-01", "Meteorology 1", null, TrainingType.Theory)]
    public async Task Classify_TheorySubjectsIncludingAirLaw_ShouldBeTheory(
        string? code, string? name, string? type, TrainingType expected)
    {
        var result = TrainingTypeClassifier.Classify(code, name, type);
        Assert.Equal(expected, result);
        await Task.CompletedTask;
    }

    [Theory]
    [InlineData("SIM-01", "Basic Instrument Flight SIM", "Simulator", TrainingType.Simulator)]
    [InlineData("FSTD-A320", "A320 Type Rating Session 1", "FSTD", TrainingType.Simulator)]
    [InlineData("FNPT-II", "Multi-Engine Instrument Procedures", "FNPT", TrainingType.Simulator)]
    [InlineData("MCC-01", "Mô phỏng bay tổ lái nhiều người", "Mô phỏng", TrainingType.Simulator)]
    [InlineData("GEN-SIM", "Cockpit Resource Management", "SIM", TrainingType.Simulator)]
    public void Classify_SimulatorSubjects_ShouldBeSimulator(
        string? code, string? name, string? type, TrainingType expected)
    {
        var result = TrainingTypeClassifier.Classify(code, name, type);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("FLT-01", "Circuits & Landings", "Flight Training", TrainingType.Flight)]
    [InlineData("FLY-101", "Cross Country Navigation", "Bay", TrainingType.Flight)]
    [InlineData("PPL-AIR", "Thực hành bay đơn PPL Solo", "Thực hành bay", TrainingType.Flight)]
    [InlineData("CPL-FLT", "Huấn luyện bay đêm", "Huấn luyện bay", TrainingType.Flight)]
    public void Classify_FlightSubjects_ShouldBeFlight(
        string? code, string? name, string? type, TrainingType expected)
    {
        var result = TrainingTypeClassifier.Classify(code, name, type);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("A320-SYS", "A320 Aircraft Systems & Avionics", "Theory", TrainingType.Theory)]
    [InlineData("A320-SIM", "A320 Full Flight Simulator (FFS) Procedures", "Practical", TrainingType.Simulator)]
    [InlineData("A320-FLT", "A320 Base Flight Training & Touch-and-Go", "Practical", TrainingType.Flight)]
    [InlineData("B737-SYS", "Boeing 737 Systems Architecture", "Theory", TrainingType.Theory)]
    [InlineData("B737-SIM", "Boeing 737 FFS Emergency Maneuvers", "Practical", TrainingType.Simulator)]
    [InlineData("CABIN-EMERGENCY", "Cabin Evacuation & Smoke/Fire Drill", "Practical", TrainingType.Theory)]
    [InlineData("MAINT-01", "Aircraft Maintenance Practical Workshop", "Practical", TrainingType.Theory)]
    [InlineData("MAINT-ENG", "Thực hành bảo dưỡng động cơ phản lực", "Thực hành", TrainingType.Theory)]
    [InlineData("AVIONICS-LAB", "Avionics System Lab & Testing", "Practical", TrainingType.Theory)]
    [InlineData("FIRST-AID", "Hàng không: Sơ cấp cứu y tế & Cứu sinh", "Practical", TrainingType.Theory)]
    [InlineData("WKS-MECH", "Thực hành cơ khí hàng không", "Workshop", TrainingType.Theory)]
    public void Classify_RealWorldSystemSubjectsAndNonFstdDisciplines_ShouldMapCorrectly(
        string? code, string? name, string? type, TrainingType expected)
    {
        var result = TrainingTypeClassifier.Classify(code, name, type);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("ALW", "Thực hành phân tích Luật hàng không", "Practical", TrainingType.Theory)]
    [InlineData("AIR_LAW", "Air Law Case Studies", "Thực hành", TrainingType.Theory)]
    [InlineData("MET", "Meteorology Weather Chart Practice", "Practical", TrainingType.Theory)]
    public void Classify_ExplicitTheorySubjectWithPracticalType_PreservesTheoryPrecedence(
        string? code, string? name, string? type, TrainingType expected)
    {
        var result = TrainingTypeClassifier.Classify(code, name, type);
        Assert.Equal(expected, result);
    }
}
