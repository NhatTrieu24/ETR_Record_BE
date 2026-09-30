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
    [InlineData("PRAC-01", "Thực hành quy trình buồng lái", "Practical", TrainingType.Simulator)]
    [InlineData("PRAC-02", "Thực hành buồng lái mô phỏng", "Thực hành", TrainingType.Simulator)]
    [InlineData("CHK-01", "Thực hành tình huống khẩn cấp", "Thực tập", TrainingType.Simulator)]
    [InlineData("SKL-01", "Kỹ năng buồng lái nhiều người", "Skill", TrainingType.Simulator)]
    [InlineData("WKS-01", "Xử lý hệ thống điều khiển", "Workshop", TrainingType.Simulator)]
    [InlineData("PRAC-FLT", "Thực hành bay vòng kín", "Practical", TrainingType.Flight)]
    [InlineData("PRAC-SOLO", "Thực hành bay Solo đơn", "Thực hành", TrainingType.Flight)]
    [InlineData("PPL-NAV", "Thực hành bay định chuẩn VFR", "Practical", TrainingType.Flight)]
    public void Classify_PracticalSubjects_ShouldBeMappedAccuratelyWithoutDefaultingToTheory(
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
