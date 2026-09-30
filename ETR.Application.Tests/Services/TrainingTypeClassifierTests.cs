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
}
