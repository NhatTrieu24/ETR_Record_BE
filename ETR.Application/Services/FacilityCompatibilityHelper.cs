using ETR.Domain.Enums;
using ETR.Domain.Entities;

namespace ETR.Application.Services;

/// <summary>
/// Hỗ trợ kiểm tra tính tương thích giữa môn học/loại hình đào tạo và cơ sở đào tạo (Facility),
/// đồng thời kiểm tra xung đột thời gian (Time overlap) chính xác theo khoảng thời gian [StartAt, EndAt).
/// </summary>
public static class FacilityCompatibilityHelper
{
    /// <summary>
    /// Nhận diện môn học thực hành mặt đất / xưởng bảo dưỡng / cứu nguy cabin.
    /// </summary>
    public static bool IsWorkshopOrPracticalSubject(string? subjectCode, string? subjectName, string? subjectType)
    {
        string code = (subjectCode ?? string.Empty).Trim();
        string name = (subjectName ?? string.Empty).Trim();
        string type = (subjectType ?? string.Empty).Trim();

        return code.StartsWith("MAINT", StringComparison.OrdinalIgnoreCase) ||
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
               type.Contains("Xưởng", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Lấy danh sách các loại cơ sở vật chất (FacilityType) tương thích với loại hình đào tạo và môn học.
    /// </summary>
    public static IReadOnlyList<FacilityType> GetCompatibleFacilityTypes(TrainingType trainingType, bool isWorkshopOrPractical)
    {
        if (trainingType == TrainingType.Simulator)
        {
            return new[] { FacilityType.Simulator };
        }

        if (trainingType == TrainingType.Flight)
        {
            return new[] { FacilityType.Airfield };
        }

        if (isWorkshopOrPractical)
        {
            return new[] { FacilityType.Workshop, FacilityType.Classroom };
        }

        return new[] { FacilityType.Classroom };
    }

    /// <summary>
    /// Kiểm tra cơ sở vật chất có tương thích với loại hình đào tạo của buổi học hay không.
    /// </summary>
    public static bool IsCompatible(TrainingType trainingType, bool isWorkshopOrPractical, FacilityType facilityType)
    {
        var compatibleTypes = GetCompatibleFacilityTypes(trainingType, isWorkshopOrPractical);
        return compatibleTypes.Contains(facilityType);
    }

    /// <summary>
    /// Overload tiện ích kiểm tra trực tiếp qua FacilityType, TrainingType và Subject.
    /// </summary>
    public static bool IsCompatible(FacilityType facilityType, TrainingType trainingType, Subject? subject = null)
    {
        bool isWorkshopOrPractical = IsWorkshopOrPracticalSubject(
            subject?.SubjectCode, subject?.SubjectName, subject?.SubjectType);
        return IsCompatible(trainingType, isWorkshopOrPractical, facilityType);
    }

    /// <summary>
    /// Kiểm tra xem 2 khoảng thời gian [start1, end1) và [start2, end2) có đè/trùng nhau hay không.
    /// </summary>
    public static bool HasTimeOverlap(DateTime start1, DateTime end1, DateTime start2, DateTime end2)
    {
        return start1 < end2 && end1 > start2;
    }
}
