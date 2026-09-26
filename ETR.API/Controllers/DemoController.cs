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
            return NotFound(new { message = $"ETR Course Record #{id} not found." });

        var enrollment = await _context.CourseEnrollments.FirstOrDefaultAsync(en => en.EnrollmentId == etr.EnrollmentId, ct);
        if (enrollment == null)
            return BadRequest(new { message = "Enrollment record not found for this ETR." });

        var classId = enrollment.ClassId;
        var accountId = enrollment.AccountId;
        var enrollmentId = enrollment.EnrollmentId;

        // 1. Attendance: Mark Present for all class sessions for this learner
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
                    Remarks = "Full Attendance (Fast-forward)",
                    IsDeleted = false
                });
            }
            else
            {
                att.Status = AttendanceStatus.Present;
                att.Remarks = "Full Attendance (Fast-forward)";
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
                        Remark = "Excellent Grade (Fast-forward)",
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

            // Attach training evidence for subject and QA verify
            var evidence = await _context.EvidenceFiles
                .FirstOrDefaultAsync(ev => ev.SubjectResultId == sr.SubjectResultId && !ev.IsDeleted, ct);
            if (evidence == null)
            {
                evidence = new EvidenceFile
                {
                    SubjectResultId = sr.SubjectResultId,
                    AccountId = accountId,
                    EvidenceTypeId = 1,
                    UploadedByAccountId = 2,
                    UploadedAt = DateTime.UtcNow,
                    VerificationStatus = "Verified",
                    VerifiedByAccountId = 3, // QA Staff
                    VerifiedAt = DateTime.UtcNow,
                    VerificationComment = "Valid training evidence verified and approved by QA (Fast-forward)",
                    IsDeleted = false
                };
                _context.EvidenceFiles.Add(evidence);
                await _context.SaveChangesAsync(ct);

                // Attach Attachment record for UI preview
                _context.Attachments.Add(new Attachment
                {
                    OwnerType = nameof(EvidenceFile),
                    OwnerId = evidence.EvidenceFileId,
                    Url = "https://res.cloudinary.com/demo/image/upload/sample_practical_checklist.pdf",
                    PublicId = "sample_practical_checklist",
                    FileName = $"Evidence_SR_{sr.SubjectResultId}_Approved.pdf",
                    MimeType = "application/pdf",
                    FileSize = 1048576,
                    UploadedByAccountId = 2,
                    UploadedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByAccountId = 2,
                    IsDeleted = false
                });
            }
            else
            {
                evidence.VerificationStatus = "Verified";
                evidence.VerifiedByAccountId = 3;
                evidence.VerifiedAt = DateTime.UtcNow;
                evidence.VerificationComment = "Valid training evidence verified and approved by QA (Fast-forward)";

                var hasAttachment = await _context.Attachments
                    .AnyAsync(a => a.OwnerType == nameof(EvidenceFile) && a.OwnerId == evidence.EvidenceFileId && !a.IsDeleted, ct);
                if (!hasAttachment)
                {
                    _context.Attachments.Add(new Attachment
                    {
                        OwnerType = nameof(EvidenceFile),
                        OwnerId = evidence.EvidenceFileId,
                        Url = "https://res.cloudinary.com/demo/image/upload/sample_practical_checklist.pdf",
                        PublicId = "sample_practical_checklist",
                        FileName = $"Evidence_SR_{sr.SubjectResultId}_Approved.pdf",
                        MimeType = "application/pdf",
                        FileSize = 1048576,
                        UploadedByAccountId = 2,
                        UploadedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        CreatedByAccountId = 2,
                        IsDeleted = false
                    });
                }
            }

            // Instructor Sign-off
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
                    Comment = "Instructor confirmed subject completion and sign-off",
                    IsDeleted = false
                });
            }
        }

        // 3. Update ETR status
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
            message = $"ETR Course Record #{id} has been fast-forwarded to status [{targetStatus}] successfully!",
            etrId = id,
            status = etr.Status.ToString(),
            isLocked = etr.IsLocked
        });
    }

    /// <summary>
    /// Unlock Deep Freeze and reset ETR record back to Draft or Verified for repeat demonstrations.
    /// </summary>
    [HttpPost("etr/{id}/reset")]
    public async Task<IActionResult> ResetETR(int id, [FromQuery] string toStatus = "Draft", CancellationToken ct = default)
    {
        var etr = await _context.ETRCourseRecords.FirstOrDefaultAsync(e => e.ETRCourseRecordId == id, ct);
        if (etr == null)
            return NotFound(new { message = $"ETR Course Record #{id} not found." });

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
            message = $"ETR Course Record #{id} has been unlocked and reset to status [{toStatus}] successfully!",
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

            // Generate 2 sample sessions for this subject
            for (int s = 1; s <= 2; s++)
            {
                _context.Sessions.Add(new Session
                {
                    ClassId = newClass.ClassId,
                    SubjectId = cs.SubjectId,
                    SessionTitle = $"Session {s}: Specialized Ground Instruction - Class {classCode}",
                    SessionDate = DateTime.UtcNow.Date.AddDays(s * 2),
                    IsConfirmed = false,
                    IsDeleted = false
                });
            }
        }
        await _context.SaveChangesAsync(ct);

        // 4. Randomly pick learners to enroll
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

        // 5. Enroll & automatically generate initial ETR in Draft status
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

            // Auto generate ETR Course Record
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

            // Generate associated SubjectResults
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
            message = $"Successfully created new Class [{classCode}] with {selectedStudents.Count} automated ETR Draft records!",
            classId = newClass.ClassId,
            classCode = newClass.ClassCode,
            className = newClass.ClassName,
            courseCode = course.CourseCode,
            courseName = course.CourseName,
            createdETRs = createdEtrList
        });
    }

    /// <summary>
    /// Fast attendance: Mark 100% Present for a session.
    /// </summary>
    [HttpPost("session/{id}/quick-attendance")]
    public async Task<IActionResult> QuickAttendance(int id, CancellationToken ct = default)
    {
        var session = await _context.Sessions.FirstOrDefaultAsync(s => s.SessionId == id, ct);
        if (session == null)
            return NotFound(new { message = $"Session #{id} not found." });

        var enrollments = await (from en in _context.CourseEnrollments
                                 join e in _context.ETRCourseRecords on en.EnrollmentId equals e.EnrollmentId into etrs
                                 from e in etrs.DefaultIfEmpty()
                                 where en.ClassId == session.ClassId && !en.IsDeleted && (e == null || (!e.IsLocked && e.Status != EtrStatus.Completed))
                                 select en)
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
                    Remarks = "Quick Attendance (100% Present)",
                    IsDeleted = false
                });
            }
            else
            {
                att.Status = AttendanceStatus.Present;
                att.Remarks = "Quick Attendance (100% Present)";
            }
            updatedCount++;
        }
        await _context.SaveChangesAsync(ct);

        return Ok(new
        {
            message = $"Marked 100% Present successfully for {updatedCount} students in session #{id}!",
            sessionId = id,
            updatedCount
        });
    }

    /// <summary>
    /// Fast grading: Assign passing score (88/100) for all students in an assessment and publish results.
    /// </summary>
    [HttpPost("assessment/{id}/quick-grade")]
    public async Task<IActionResult> QuickGrade(int id, [FromQuery] decimal score = 88.0m, CancellationToken ct = default)
    {
        var asm = await _context.Assessments.FirstOrDefaultAsync(a => a.AssessmentId == id, ct);
        if (asm == null)
            return NotFound(new { message = $"Assessment #{id} not found." });

        var subjectResults = await (from sr in _context.SubjectResults
                                    join e in _context.ETRCourseRecords on sr.EtrId equals e.ETRCourseRecordId
                                    join en in _context.CourseEnrollments on e.EnrollmentId equals en.EnrollmentId
                                    where sr.SubjectId == asm.SubjectId && !sr.IsDeleted && !e.IsLocked && e.Status != EtrStatus.Completed
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
                    Remark = "Standard Passing Score (Quick Grade)",
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
            message = $"Assigned score {score} (Passed) and published successfully for {updatedCount} students in assessment #{id}!",
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
