using ETR.Domain.Entities;
using ETR.Domain.Enums;
using ETR.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ETR.API.Controllers;

/// <summary>
/// [God Mode / Demo Helper Controller]:
/// Cung cấp các API đặc quyền phục vụ buổi bảo vệ đồ án tốt nghiệp:
/// Tua nhanh ETR, gỡ khóa Deep Freeze, sinh ngẫu nhiên lớp học và học viên, tự động điểm danh/chấm điểm.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class DemoController : ControllerBase
{
    private readonly AppDbContext _context;

    public DemoController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lấy tổng quan tình trạng ETR và danh sách để hiển thị dropdown trên giao diện demo.
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        var etrs = await (from e in _context.ETRCourseRecords.AsNoTracking()
                          join en in _context.CourseEnrollments.AsNoTracking() on e.EnrollmentId equals en.EnrollmentId
                          join c in _context.Classes.AsNoTracking() on en.ClassId equals c.ClassId
                          join crs in _context.Courses.AsNoTracking() on c.CourseId equals crs.CourseId
                          join a in _context.Accounts.AsNoTracking() on en.AccountId equals a.AccountId
                          join p in _context.UserProfiles.AsNoTracking() on a.AccountId equals p.AccountId into profs
                          from p in profs.DefaultIfEmpty()
                          orderby e.ETRCourseRecordId descending
                          select new
                          {
                              e.ETRCourseRecordId,
                              Status = e.Status.ToString(),
                              e.IsLocked,
                              StudentName = p != null ? p.FullName : a.Username,
                              UserCode = p != null ? p.UserCode : "",
                              c.ClassCode,
                              c.ClassName,
                              crs.CourseCode,
                              crs.CourseName,
                              e.CreatedAt
                          })
                          .Take(30)
                          .ToListAsync(ct);

        var statusCounts = await _context.ETRCourseRecords
            .GroupBy(e => e.Status)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
            .ToListAsync(ct);

        var sessions = await (from s in _context.Sessions.AsNoTracking()
                              join c in _context.Classes.AsNoTracking() on s.ClassId equals c.ClassId
                              join sub in _context.Subjects.AsNoTracking() on s.SubjectId equals sub.SubjectId
                              orderby s.SessionId descending
                              select new
                              {
                                  s.SessionId,
                                  s.SessionTitle,
                                  s.ClassId,
                                  c.ClassCode,
                                  s.SubjectId,
                                  sub.SubjectName,
                                  s.SessionDate,
                                  s.IsConfirmed
                              })
                              .Take(15)
                              .ToListAsync(ct);

        var assessments = await (from a in _context.Assessments.AsNoTracking()
                                 join sub in _context.Subjects.AsNoTracking() on a.SubjectId equals sub.SubjectId
                                 join crs in _context.Courses.AsNoTracking() on a.CourseId equals crs.CourseId
                                 orderby a.AssessmentId descending
                                 select new
                                 {
                                     a.AssessmentId,
                                     AssessmentName = a.ComponentName,
                                     a.AssessmentType,
                                     a.SubjectId,
                                     sub.SubjectName,
                                     a.CourseId,
                                     crs.CourseName,
                                     a.PassingScore
                                 })
                                 .Take(15)
                                 .ToListAsync(ct);

        return Ok(new
        {
            TotalETRs = await _context.ETRCourseRecords.CountAsync(ct),
            StatusCounts = statusCounts,
            RecentETRs = etrs,
            RecentSessions = sessions,
            RecentAssessments = assessments
        });
    }

    /// <summary>
    /// Tua nhanh hồ sơ ETR đến trạng thái mong muốn: 'Submitted', 'Verified', hoặc 'Completed'.
    /// </summary>
    [HttpPost("etr/{id}/fast-forward")]
    public async Task<IActionResult> FastForwardETR(int id, [FromQuery] string targetStatus = "Verified", CancellationToken ct = default)
    {
        var etr = await _context.ETRCourseRecords.FirstOrDefaultAsync(e => e.ETRCourseRecordId == id, ct);
        if (etr == null)
            return NotFound(new { message = $"Không tìm thấy hồ sơ ETR #{id}" });

        var enrollment = await _context.CourseEnrollments.FirstOrDefaultAsync(en => en.EnrollmentId == etr.EnrollmentId, ct);
        if (enrollment == null)
            return BadRequest(new { message = "Không tìm thấy thông tin ghi danh của hồ sơ này." });

        var classId = enrollment.ClassId;
        var accountId = enrollment.AccountId;
        var enrollmentId = enrollment.EnrollmentId;

        // 1. Điểm danh: Đánh dấu Present toàn bộ buổi học của lớp cho học viên này
        var sessions = await _context.Sessions.Where(s => s.ClassId == classId).ToListAsync(ct);
        foreach (var sess in sessions)
        {
            var att = await _context.AttendanceRecords
                .FirstOrDefaultAsync(a => a.SessionId == sess.SessionId && a.EnrollmentId == enrollmentId, ct);
            if (att == null)
            {
                _context.AttendanceRecords.Add(new AttendanceRecord
                {
                    SessionId = sess.SessionId,
                    EnrollmentId = enrollmentId,
                    Status = AttendanceStatus.Present,
                    RecordedByAccountId = 2,
                    RecordedAt = DateTime.UtcNow,
                    Remarks = "Điểm danh đầy đủ (Fast-forward)",
                    IsDeleted = false
                });
            }
            else
            {
                att.Status = AttendanceStatus.Present;
                att.Remarks = "Điểm danh đầy đủ (Fast-forward)";
            }
        }

        // 2. Chấm điểm: Đảm bảo mọi SubjectResult đều có điểm thi đạt chuẩn >= 85
        var subjectResults = await _context.SubjectResults.Where(sr => sr.EtrId == id && !sr.IsDeleted).ToListAsync(ct);
        foreach (var sr in subjectResults)
        {
            sr.AttendanceRate = 100.0m;
            sr.Score = 88.0m;
            sr.Status = SubjectResultStatus.Passed;
            sr.EvaluatedAt = DateTime.UtcNow;
            sr.EvaluatedByAccountId = 2;

            // Kiểm tra điểm Assessment
            var assessments = await _context.Assessments.Where(a => a.SubjectId == sr.SubjectId && !a.IsDeleted).ToListAsync(ct);
            foreach (var asm in assessments)
            {
                var ar = await _context.AssessmentResults
                    .FirstOrDefaultAsync(r => r.AssessmentId == asm.AssessmentId && r.SubjectResultId == sr.SubjectResultId, ct);
                if (ar == null)
                {
                    _context.AssessmentResults.Add(new AssessmentResult
                    {
                        AssessmentId = asm.AssessmentId,
                        AccountId = accountId,
                        SubjectResultId = sr.SubjectResultId,
                        Score = 88.0m,
                        ResultStatus = "Passed",
                        GradedByAccountId = 2,
                        RecordedAt = DateTime.UtcNow,
                        PublishedAt = DateTime.UtcNow,
                        IsPublished = true,
                        PassingScoreSnapshot = asm.PassingScore,
                        WeightSnapshot = asm.Weight,
                        AttemptNo = 1,
                        Remark = "Đạt điểm xuất sắc (Fast-forward)",
                        IsDeleted = false
                    });
                }
                else
                {
                    ar.Score = 88.0m;
                    ar.ResultStatus = "Passed";
                    ar.IsPublished = true;
                    ar.PublishedAt ??= DateTime.UtcNow;
                }
            }

            // Đính kèm Minh chứng (Evidence) cho môn học
            var evidence = await _context.EvidenceFiles
                .FirstOrDefaultAsync(ev => ev.SubjectResultId == sr.SubjectResultId && !ev.IsDeleted, ct);
            if (evidence == null)
            {
                _context.EvidenceFiles.Add(new EvidenceFile
                {
                    SubjectResultId = sr.SubjectResultId,
                    AccountId = accountId,
                    EvidenceTypeId = 1,
                    UploadedByAccountId = 2,
                    UploadedAt = DateTime.UtcNow,
                    VerificationStatus = (targetStatus == "Verified" || targetStatus == "Completed") ? "Verified" : "Pending",
                    VerifiedByAccountId = (targetStatus == "Verified" || targetStatus == "Completed") ? 3 : null,
                    VerifiedAt = (targetStatus == "Verified" || targetStatus == "Completed") ? DateTime.UtcNow : null,
                    VerificationComment = "Minh chứng hợp lệ đã qua kiểm định QA (Fast-forward)",
                    IsDeleted = false
                });
            }
            else if (targetStatus == "Verified" || targetStatus == "Completed")
            {
                evidence.VerificationStatus = "Verified";
                evidence.VerifiedByAccountId = 3;
                evidence.VerifiedAt = DateTime.UtcNow;
                evidence.VerificationComment = "Minh chứng hợp lệ đã qua kiểm định QA (Fast-forward)";
            }

            // Ký xác nhận môn (Sign-off)
            var signoff = await _context.SubjectSignoffs
                .FirstOrDefaultAsync(so => so.SubjectResultId == sr.SubjectResultId && !so.IsDeleted, ct);
            if (signoff == null)
            {
                _context.SubjectSignoffs.Add(new SubjectSignoff
                {
                    SubjectResultId = sr.SubjectResultId,
                    SignoffByAccountId = 2,
                    Role = "Instructor",
                    SignoffAt = DateTime.UtcNow,
                    Comment = "Giảng viên xác nhận hoàn tất môn học",
                    IsDeleted = false
                });
            }
        }

        // 3. Cập nhật trạng thái ETR
        if (string.Equals(targetStatus, "Submitted", StringComparison.OrdinalIgnoreCase))
        {
            etr.Status = EtrStatus.Submitted;
            etr.SubmittedAt = DateTime.UtcNow;
            etr.IsLocked = false;
        }
        else if (string.Equals(targetStatus, "Verified", StringComparison.OrdinalIgnoreCase))
        {
            etr.Status = EtrStatus.Verified;
            etr.SubmittedAt ??= DateTime.UtcNow.AddHours(-1);
            etr.VerifiedAt = DateTime.UtcNow;
            etr.IsLocked = false;
        }
        else if (string.Equals(targetStatus, "Completed", StringComparison.OrdinalIgnoreCase))
        {
            etr.Status = EtrStatus.Completed;
            etr.SubmittedAt ??= DateTime.UtcNow.AddHours(-2);
            etr.VerifiedAt ??= DateTime.UtcNow.AddHours(-1);
            etr.CompletedAt = DateTime.UtcNow;
            etr.IsLocked = true;
            etr.IssuedDate = DateTime.UtcNow;
            etr.ExpiryDate = DateTime.UtcNow.AddYears(2);
        }

        await _context.SaveChangesAsync(ct);

        return Ok(new
        {
            message = $"Hồ sơ ETR #{id} đã được tua nhanh thành công sang trạng thái [{targetStatus}]!",
            etrId = id,
            status = etr.Status.ToString(),
            isLocked = etr.IsLocked
        });
    }

    /// <summary>
    /// Gỡ khóa Deep Freeze và khôi phục hồ sơ ETR về Draft hoặc Verified để demo lại nhiều lần.
    /// </summary>
    [HttpPost("etr/{id}/reset")]
    public async Task<IActionResult> ResetETR(int id, [FromQuery] string toStatus = "Draft", CancellationToken ct = default)
    {
        var etr = await _context.ETRCourseRecords.FirstOrDefaultAsync(e => e.ETRCourseRecordId == id, ct);
        if (etr == null)
            return NotFound(new { message = $"Không tìm thấy hồ sơ ETR #{id}" });

        etr.IsLocked = false;
        if (string.Equals(toStatus, "Draft", StringComparison.OrdinalIgnoreCase))
        {
            etr.Status = EtrStatus.Draft;
            etr.CompletedAt = null;
            etr.VerifiedAt = null;
            etr.SubmittedAt = null;
        }
        else if (string.Equals(toStatus, "Verified", StringComparison.OrdinalIgnoreCase))
        {
            etr.Status = EtrStatus.Verified;
            etr.CompletedAt = null;
        }
        else if (string.Equals(toStatus, "Submitted", StringComparison.OrdinalIgnoreCase))
        {
            etr.Status = EtrStatus.Submitted;
            etr.CompletedAt = null;
            etr.VerifiedAt = null;
        }

        await _context.SaveChangesAsync(ct);

        return Ok(new
        {
            message = $"Hồ sơ ETR #{id} đã được mở khóa và khôi phục về trạng thái [{toStatus}] thành công!",
            etrId = id,
            status = etr.Status.ToString(),
            isLocked = etr.IsLocked
        });
    }

    /// <summary>
    /// [NHÓM 5]: Tự động sinh ngẫu nhiên 1 lớp học mới toanh + phân công giáo viên + ghi danh học viên + auto sinh ETR Draft.
    /// Bấm liên tục thoải mái không bao giờ sợ trùng lặp!
    /// </summary>
    [HttpPost("cohort/quick-random")]
    public async Task<IActionResult> QuickRandomCohort([FromBody] QuickCohortRequest? request, CancellationToken ct = default)
    {
        var rnd = new Random();
        var suffix = rnd.Next(1000, 9999);

        // 1. Chọn Course: Nếu không truyền thì random 1 course có sẵn trong DB
        Course? course = null;
        if (!string.IsNullOrWhiteSpace(request?.CourseCode))
        {
            course = await _context.Courses.FirstOrDefaultAsync(c => c.CourseCode == request.CourseCode && !c.IsDeleted, ct);
        }
        if (course == null)
        {
            var courses = await _context.Courses.Where(c => !c.IsDeleted && c.Status == CourseStatus.Active).ToListAsync(ct);
            course = courses[rnd.Next(courses.Count)];
        }

        // 2. Sinh ClassCode và ClassName độc nhất ngẫu nhiên
        var classCode = !string.IsNullOrWhiteSpace(request?.ClassCode) ? request.ClassCode : $"SIM-LIVE-{suffix}";
        var className = !string.IsNullOrWhiteSpace(request?.ClassName) ? request.ClassName : $"Flight Training Cohort {suffix} (Live Demo)";

        var newClass = new Class
        {
            CourseId = course.CourseId,
            ClassCode = classCode,
            ClassName = className,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddMonths(3),
            Location = "Sim Bay 01 - Flight Training Hangar",
            Capacity = 30,
            Status = ClassStatus.InProgress,
            CreatedByAccountId = 7, // Academic Staff
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
        _context.Classes.Add(newClass);
        await _context.SaveChangesAsync(ct);

        // 3. Gán CourseSubjects vào ClassSubjects kèm giáo viên phân công
        var courseSubjects = await _context.CourseSubjects
            .Where(cs => cs.CourseId == course.CourseId && !cs.IsDeleted)
            .ToListAsync(ct);

        var instructors = await _context.Accounts
            .Where(a => a.RoleId == 2 && !a.IsDeleted && a.Status == AccountStatus.Active)
            .ToListAsync(ct);

        foreach (var cs in courseSubjects)
        {
            var assignedInstructor = instructors[rnd.Next(instructors.Count)];
            _context.ClassSubjects.Add(new ClassSubject
            {
                ClassId = newClass.ClassId,
                SubjectId = cs.SubjectId,
                InstructorAccountId = assignedInstructor.AccountId,
                IsDeleted = false
            });

            // Sinh 2 buổi học mẫu cho môn này
            for (int s = 1; s <= 2; s++)
            {
                _context.Sessions.Add(new Session
                {
                    ClassId = newClass.ClassId,
                    SubjectId = cs.SubjectId,
                    SessionTitle = $"Buổi {s}: Giảng huấn chuyên môn - Lớp {classCode}",
                    SessionDate = DateTime.UtcNow.Date.AddDays(s * 2),
                    IsConfirmed = false,
                    IsDeleted = false
                });
            }
        }
        await _context.SaveChangesAsync(ct);

        // 4. Lấy ngẫu nhiên học viên để ghi danh
        var count = request?.StudentCount ?? 3;
        var existingStudentAccounts = await (from a in _context.Accounts.AsNoTracking()
                                            join p in _context.UserProfiles.AsNoTracking() on a.AccountId equals p.AccountId into profs
                                            from p in profs.DefaultIfEmpty()
                                            where a.RoleId == 6 && !a.IsDeleted && a.Status == AccountStatus.Active
                                            select new { a.AccountId, a.Username, FullName = p != null ? p.FullName : a.Username, UserCode = p != null ? p.UserCode : "" })
                                            .Take(30)
                                            .ToListAsync(ct);

        var selectedStudents = existingStudentAccounts
            .OrderBy(_ => rnd.Next())
            .Take(count)
            .ToList();

        var createdEtrList = new List<object>();

        // 5. Ghi danh & Tự động sinh hồ sơ ETR ở trạng thái Draft
        foreach (var stu in selectedStudents)
        {
            var enrollment = new CourseEnrollment
            {
                AccountId = stu.AccountId,
                ClassId = newClass.ClassId,
                EnrolledAt = DateTime.UtcNow,
                Status = EnrollmentStatus.Active,
                CreatedByAccountId = 7,
                IsDeleted = false
            };
            _context.CourseEnrollments.Add(enrollment);
            await _context.SaveChangesAsync(ct);

            // Tự động sinh ETR Course Record
            var etr = new ETRCourseRecord
            {
                EnrollmentId = enrollment.EnrollmentId,
                Status = EtrStatus.Draft,
                IsLocked = false,
                CourseVersionNo = 1,
                CreatedBySystem = true,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
            _context.ETRCourseRecords.Add(etr);
            await _context.SaveChangesAsync(ct);

            // Sinh SubjectResults gắn kèm các môn tương ứng
            foreach (var cs in courseSubjects)
            {
                var sr = new SubjectResult
                {
                    EtrId = etr.ETRCourseRecordId,
                    CourseId = course.CourseId,
                    SubjectId = cs.SubjectId,
                    Status = SubjectResultStatus.Pending,
                    AttendanceRate = 0,
                    IsDeleted = false
                };
                _context.SubjectResults.Add(sr);
            }
            await _context.SaveChangesAsync(ct);

            createdEtrList.Add(new
            {
                etrId = etr.ETRCourseRecordId,
                studentName = stu.FullName,
                userCode = stu.UserCode,
                status = "Draft"
            });
        }

        return Ok(new
        {
            message = $"Đã tạo thành công Lớp học mới [{classCode}] kèm {selectedStudents.Count} hồ sơ ETR tự động ở trạng thái 'Draft'!",
            classId = newClass.ClassId,
            classCode = newClass.ClassCode,
            className = newClass.ClassName,
            courseCode = course.CourseCode,
            courseName = course.CourseName,
            createdETRs = createdEtrList
        });
    }

    /// <summary>
    /// Điểm danh nhanh 100% Present cho một buổi học.
    /// </summary>
    [HttpPost("session/{id}/quick-attendance")]
    public async Task<IActionResult> QuickAttendance(int id, CancellationToken ct = default)
    {
        var session = await _context.Sessions.FirstOrDefaultAsync(s => s.SessionId == id, ct);
        if (session == null)
            return NotFound(new { message = $"Không tìm thấy buổi học #{id}" });

        var enrollments = await _context.CourseEnrollments
            .Where(e => e.ClassId == session.ClassId && !e.IsDeleted)
            .ToListAsync(ct);

        int updatedCount = 0;
        foreach (var en in enrollments)
        {
            var att = await _context.AttendanceRecords
                .FirstOrDefaultAsync(a => a.SessionId == id && a.EnrollmentId == en.EnrollmentId, ct);
            if (att == null)
            {
                _context.AttendanceRecords.Add(new AttendanceRecord
                {
                    SessionId = id,
                    EnrollmentId = en.EnrollmentId,
                    Status = AttendanceStatus.Present,
                    RecordedByAccountId = 2,
                    RecordedAt = DateTime.UtcNow,
                    Remarks = "Điểm danh nhanh tự động (100% Present)",
                    IsDeleted = false
                });
            }
            else
            {
                att.Status = AttendanceStatus.Present;
                att.Remarks = "Điểm danh nhanh tự động (100% Present)";
            }
            updatedCount++;
        }
        await _context.SaveChangesAsync(ct);

        return Ok(new
        {
            message = $"Đã điểm danh Present 100% thành công cho {updatedCount} học viên trong buổi học #{id}!",
            sessionId = id,
            updatedCount
        });
    }

    /// <summary>
    /// Chấm điểm đạt chuẩn (88/100) cho toàn bộ học viên trong bài thi và chốt điểm (IsPublished = true).
    /// </summary>
    [HttpPost("assessment/{id}/quick-grade")]
    public async Task<IActionResult> QuickGrade(int id, [FromQuery] decimal score = 88.0m, CancellationToken ct = default)
    {
        var asm = await _context.Assessments.FirstOrDefaultAsync(a => a.AssessmentId == id, ct);
        if (asm == null)
            return NotFound(new { message = $"Không tìm thấy bài thi #{id}" });

        var subjectResults = await (from sr in _context.SubjectResults
                                    join e in _context.ETRCourseRecords on sr.EtrId equals e.ETRCourseRecordId
                                    join en in _context.CourseEnrollments on e.EnrollmentId equals en.EnrollmentId
                                    where sr.SubjectId == asm.SubjectId && !sr.IsDeleted
                                    select new { sr, en.AccountId })
                                    .ToListAsync(ct);

        int updatedCount = 0;
        foreach (var item in subjectResults)
        {
            var ar = await _context.AssessmentResults
                .FirstOrDefaultAsync(r => r.AssessmentId == id && r.SubjectResultId == item.sr.SubjectResultId, ct);

            if (ar == null)
            {
                _context.AssessmentResults.Add(new AssessmentResult
                {
                    AssessmentId = id,
                    AccountId = item.AccountId,
                    SubjectResultId = item.sr.SubjectResultId,
                    Score = score,
                    ResultStatus = "Passed",
                    GradedByAccountId = 2,
                    RecordedAt = DateTime.UtcNow,
                    PublishedAt = DateTime.UtcNow,
                    IsPublished = true,
                    PassingScoreSnapshot = asm.PassingScore,
                    WeightSnapshot = asm.Weight,
                    AttemptNo = 1,
                    Remark = "Chấm điểm đạt chuẩn nhanh (Quick Grade)",
                    IsDeleted = false
                });
            }
            else
            {
                ar.Score = score;
                ar.ResultStatus = "Passed";
                ar.IsPublished = true;
                ar.PublishedAt = DateTime.UtcNow;
            }
            updatedCount++;
        }
        await _context.SaveChangesAsync(ct);

        return Ok(new
        {
            message = $"Đã chấm điểm {score} (Đạt) và chốt điểm thành công cho {updatedCount} học viên trong bài thi #{id}!",
            assessmentId = id,
            updatedCount
        });
    }
}

public class QuickCohortRequest
{
    public string? CourseCode { get; set; }
    public string? ClassCode { get; set; }
    public string? ClassName { get; set; }
    public int? StudentCount { get; set; }
}
