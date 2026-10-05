namespace ETR.Domain.Enums;

/// <summary>
/// Loại hình cơ sở vật chất / địa điểm đào tạo theo chuẩn hàng không.
/// </summary>
public enum FacilityType
{
    /// <summary>
    /// Phòng học lý thuyết tiêu chuẩn.
    /// </summary>
    Classroom = 1,

    /// <summary>
    /// Xưởng thực hành mặt đất, bảo dưỡng tàu bay, huấn luyện khẩn nguy cabin.
    /// </summary>
    Workshop = 2,

    /// <summary>
    /// Thiết bị mô phỏng buồng lái huấn luyện bay (FSTD / FNPT / FFS).
    /// </summary>
    Simulator = 3,

    /// <summary>
    /// Sân bay huấn luyện, căn cứ bay thực hành cất hạ cánh và đường bay.
    /// </summary>
    Airfield = 4
}
