using ETR.Application.Compliance;
using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using ETR.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace ETR.Application.Services;

public class EtrService : IEtrService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public EtrService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    private async Task<HashSet<int>> GetInstructorClassIdsAsync(int instructorAccountId, CancellationToken cancellationToken)
    {
        return _unitOfWork.ClassSubjectRepository.GetQueryable()
            .Where(cs => cs.InstructorAccountId == instructorAccountId)
            .Select(cs => cs.ClassId)
            .ToHashSet();
    }

    private async Task<HashSet<int>> GetInstructorEnrollmentIdsAsync(int instructorAccountId, CancellationToken cancellationToken)
    {
        var classIds = await GetInstructorClassIdsAsync(instructorAccountId, cancellationToken);
        var enrollments = await _unitOfWork.CourseEnrollmentRepository.GetAllAsync(cancellationToken);
        return enrollments.Where(e => classIds.Contains(e.ClassId)).Select(e => e.EnrollmentId).ToHashSet();
    }

    private async Task<(decimal qualifiedFlightHours, decimal qualifiedSimHours)> GetQualifiedTrainingHoursAsync(int enrollmentId, CancellationToken cancellationToken)
    {
        var allSessions = (await _unitOfWork.SessionRepository.GetAllAsync(cancellationToken) ?? Enumerable.Empty<Session>())
            .Where(s => !s.IsDeleted)
            .ToDictionary(s => s.SessionId);

        var allAttendanceRecords = await _unitOfWork.AttendanceRecordRepository.GetAllAsync(cancellationToken) ?? Enumerable.Empty<AttendanceRecord>();
        var qualifiedRecords = allAttendanceRecords
            .Where(ar => !ar.IsDeleted &&
                         ar.EnrollmentId == enrollmentId &&
                         ar.Status == AttendanceStatus.Present &&
                         allSessions.ContainsKey(ar.SessionId))
            .Where(ar =>
            {
                var session = allSessions[ar.SessionId];
                bool isSignedByInstructor = ar.InstructorSignedAt.HasValue || ar.InstructorSignedByAccountId.HasValue;
                bool isSessionConfirmed = session.IsConfirmed;
                return isSignedByInstructor || isSessionConfirmed;
            })
            .ToList();

        decimal flightH = qualifiedRecords.Sum(ar => ar.FlightHours ?? 0m);
        decimal simH = qualifiedRecords.Sum(ar => ar.SimulatorHours ?? 0m);
        return (flightH, simH);
    }

    public async Task<IEnumerable<EtrRecordResponse>> GetAllEtrsAsync(CancellationToken cancellationToken = default)
    {
        var etrs = await _unitOfWork.ETRCourseRecordRepository.GetAllAsync(cancellationToken);
        
        if (_currentUserService.RoleName == "Instructor" && _currentUserService.AccountId.HasValue)
        {
            var myEnrollmentIds = await GetInstructorEnrollmentIdsAsync(_currentUserService.AccountId.Value, cancellationToken);
            etrs = etrs.Where(e => myEnrollmentIds.Contains(e.EnrollmentId)).ToList();
        }

        return etrs.Select(e => new EtrRecordResponse(
            e.ETRCourseRecordId,
            e.EnrollmentId,
            e.Status,
            e.IsLocked,
            e.SubmittedAt,
            e.VerifiedAt,
            e.CompletedAt,
            e.IssuedDate,
            e.ExpiryDate,
            e.PreviousRecordId));
    }

    public async Task<IEnumerable<EtrRecordResponse>> GetMyEtrsAsync(int accountId, CancellationToken cancellationToken = default)
    {
        var enrollments = await _unitOfWork.CourseEnrollmentRepository.GetAllAsync(cancellationToken);
        var myEnrollmentIds = enrollments.Where(e => e.AccountId == accountId).Select(e => e.EnrollmentId).ToList();

        var etrs = await _unitOfWork.ETRCourseRecordRepository.GetAllAsync(cancellationToken);
        var myEtrs = etrs.Where(e => myEnrollmentIds.Contains(e.EnrollmentId));

        return myEtrs.Select(e => new EtrRecordResponse(
            e.ETRCourseRecordId,
            e.EnrollmentId,
            e.Status,
            e.IsLocked,
            e.SubmittedAt,
            e.VerifiedAt,
            e.CompletedAt,
            e.IssuedDate,
            e.ExpiryDate,
            e.PreviousRecordId));
    }

    public async Task<EtrDetailsResponse> GetEtrByIdAsync(int etrCourseRecordId, CancellationToken cancellationToken = default)
    {
        var e = await _unitOfWork.ETRCourseRecordRepository.GetWithSubjectResultsAsync(etrCourseRecordId, cancellationToken)
            ?? throw new KeyNotFoundException($"ETRCourseRecord not found.");

        if (_currentUserService.RoleName == "Instructor" && _currentUserService.AccountId.HasValue)
        {
            var myEnrollmentIds = await GetInstructorEnrollmentIdsAsync(_currentUserService.AccountId.Value, cancellationToken);
            if (!myEnrollmentIds.Contains(e.EnrollmentId))
            {
                throw new KeyNotFoundException($"ETRCourseRecord not found.");
            }
        }

        var subjectResultIds = e.SubjectResults?.Select(sr => sr.SubjectResultId).ToList() ?? new List<int>();

        var allAssessmentResults = await _unitOfWork.AssessmentResultRepository.GetAllAsync(cancellationToken);
        var assessmentResults = allAssessmentResults.Where(ar => subjectResultIds.Contains(ar.SubjectResultId)).ToList();

        var allPracticalResults = await _unitOfWork.PracticalChecklistResultRepository.GetAllAsync(cancellationToken);
        var practicalResults = allPracticalResults.Where(pr => subjectResultIds.Contains(pr.SubjectResultId)).ToList();

        var allSignoffs = await _unitOfWork.SubjectSignoffRepository.GetAllAsync(cancellationToken);
        var signoffs = allSignoffs.Where(s => subjectResultIds.Contains(s.SubjectResultId)).ToList();

        var allApprovalRequests = await _unitOfWork.ApprovalRequestRepository.GetAllAsync(cancellationToken);
        var approvalRequest = allApprovalRequests.FirstOrDefault(ar => ar.ETRCourseRecordId == etrCourseRecordId);

        var approvalHistories = new List<ApprovalHistory>();
        if (approvalRequest != null)
        {
            var allApprovalHistories = await _unitOfWork.ApprovalHistoryRepository.GetAllAsync(cancellationToken);
            approvalHistories = allApprovalHistories.Where(ah => ah.ApprovalRequestId == approvalRequest.ApprovalRequestId).ToList();
        }

        var allEvidences = await _unitOfWork.EvidenceFileRepository.GetAllAsync(cancellationToken);
        var evidences = allEvidences.Where(ev => subjectResultIds.Contains(ev.SubjectResultId) && !ev.IsDeleted).ToList();
        var evidenceAttachments = (await _unitOfWork.AttachmentRepository.GetAllAsync(cancellationToken))
            .Where(a => a.OwnerType == nameof(EvidenceFile) && evidences.Select(ev => ev.EvidenceFileId).Contains(a.OwnerId))
            .GroupBy(a => a.OwnerId)
            .ToDictionary(g => g.Key, g => g.First());

        var subjectResultResponses = e.SubjectResults?.Select(sr => {
            var signoff = signoffs.FirstOrDefault(s => s.SubjectResultId == sr.SubjectResultId);
            return new EtrSubjectDetailResponse(
                sr.SubjectResultId,
                sr.SubjectId,
                sr.Status,
                sr.CreatedAt,
                sr.AttendanceRate,
                sr.Score,
                signoff != null,
                signoff?.SignoffAt,
                assessmentResults.Where(ar => ar.SubjectResultId == sr.SubjectResultId).Select(ar => new EtrAssessmentResultResponse(
                    ar.AssessmentResultId, ar.AssessmentId, ar.Score, ar.ResultStatus, ar.AttemptNo, ar.IsPublished
                )).ToList(),
                practicalResults.Where(pr => pr.SubjectResultId == sr.SubjectResultId).Select(pr => new EtrPracticalChecklistResultResponse(
                    pr.PracticalChecklistResultId, pr.PracticalChecklistId, pr.ResultStatus, pr.IsPublished
                )).ToList(),
                sr.CarriedOverFromSubjectResultId.HasValue
            );
        }).ToList() ?? new List<EtrSubjectDetailResponse>();

        var approvalHistoryResponses = approvalHistories.Select(ah => new EtrApprovalHistoryResponse(
            ah.ApprovalHistoryId, ah.ApprovalRequestId, ah.ActionType, ah.Comments, ah.ActionByAccountId, ah.ActionAt
        )).ToList();

        var evidenceResponses = evidences.Select(ev => {
            var attachment = evidenceAttachments.GetValueOrDefault(ev.EvidenceFileId);
            return new EtrEvidenceFileResponse(
                ev.EvidenceFileId, attachment?.FileName ?? string.Empty, attachment?.Url ?? string.Empty,
                attachment?.MimeType ?? "unknown", ev.UploadedByAccountId, ev.UploadedAt,
                ev.VerificationStatus, ev.VerificationComment, ev.VerifiedByAccountId, ev.VerifiedAt
            );
        }).ToList();

        return new EtrDetailsResponse(
            e.ETRCourseRecordId,
            e.EnrollmentId,
            e.Status,
            e.IsLocked,
            e.SubmittedAt,
            e.VerifiedAt,
            e.CompletedAt,
            subjectResultResponses,
            approvalHistoryResponses,
            evidenceResponses);
    }

    public async Task<EtrDossierResponse> GetEtrDossierAsync(int etrCourseRecordId, int currentAccountId, string roleName, CancellationToken cancellationToken = default)
    {
        var etr = await _unitOfWork.ETRCourseRecordRepository.GetWithSubjectResultsAsync(etrCourseRecordId, cancellationToken)
            ?? throw new KeyNotFoundException($"ETRCourseRecord not found.");

        var enrollment = await _unitOfWork.CourseEnrollmentRepository.GetByIdAsync(etr.EnrollmentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Enrollment for ETR #{etrCourseRecordId} not found.");

        // 1. Phân quyền truy cập chính dossier (Object-Level Authorization / Scope Check)
        if (roleName == "Student")
        {
            if (enrollment.AccountId != currentAccountId)
            {
                throw new ForbiddenAccessException("Bạn chỉ được phép xem hồ sơ ETR của chính mình.");
            }
        }
        else if (roleName == "Instructor")
        {
            var myEnrollmentIds = await GetInstructorEnrollmentIdsAsync(currentAccountId, cancellationToken);
            if (!myEnrollmentIds.Contains(etr.EnrollmentId))
            {
                throw new ForbiddenAccessException("Bạn không được phân công giảng dạy học viên này.");
            }
        }

        // 2. Load Core Info (Student, Course, Class)
        var allProfiles = await _unitOfWork.UserProfileRepository.GetAllAsync(cancellationToken);
        var learnerProfile = allProfiles.FirstOrDefault(p => p.AccountId == enrollment.AccountId);

        var allAccounts = await _unitOfWork.AccountRepository.GetAllAsync(cancellationToken);
        var accountMap = allAccounts.ToDictionary(a => a.AccountId, a => a.Username ?? $"Account #{a.AccountId}");
        var profileMap = allProfiles.Where(p => !string.IsNullOrWhiteSpace(p.FullName)).ToDictionary(p => p.AccountId, p => p.FullName);

        string GetAccountDisplayName(int? accId)
        {
            if (!accId.HasValue) return "Hệ thống";
            if (profileMap.TryGetValue(accId.Value, out var pName) && !string.IsNullOrWhiteSpace(pName)) return pName;
            if (accountMap.TryGetValue(accId.Value, out var aName) && !string.IsNullOrWhiteSpace(aName)) return aName;
            return $"Account #{accId.Value}";
        }

        var trainingClass = await _unitOfWork.ClassRepository.GetByIdAsync(enrollment.ClassId, cancellationToken);
        var course = trainingClass != null ? await _unitOfWork.CourseRepository.GetByIdAsync(trainingClass.CourseId, cancellationToken) : null;

        // Privacy filtering for Student Profile
        bool canViewSensitiveLearnerDetails = roleName is "Student" or "Admin" or "Academic";
        string? emailDisplay = canViewSensitiveLearnerDetails ? learnerProfile?.Email : null;
        string? phoneDisplay = canViewSensitiveLearnerDetails ? learnerProfile?.Phone : null;

        var studentInfo = new EtrDossierStudentInfo(
            enrollment.AccountId,
            learnerProfile?.UserCode ?? $"HV-{enrollment.AccountId}",
            learnerProfile?.FullName ?? $"Học viên #{enrollment.AccountId}",
            emailDisplay,
            phoneDisplay
        );

        var courseInfo = new EtrDossierCourseInfo(
            course?.CourseId ?? 0,
            course?.CourseCode ?? "N/A",
            course?.CourseName ?? "Chưa rõ",
            etr.CourseVersionNo > 0 ? etr.CourseVersionNo : (course?.VersionNo ?? 1)
        );

        var classInfo = new EtrDossierClassInfo(
            trainingClass?.ClassId ?? 0,
            trainingClass?.ClassCode ?? "N/A",
            trainingClass?.ClassName ?? "Chưa rõ",
            trainingClass?.StartDate,
            trainingClass?.EndDate,
            trainingClass?.Status.ToString() ?? "Active"
        );

        // 3. Subjects & Components Drill-Down
        var subjectResults = etr.SubjectResults ?? new List<SubjectResult>();
        var subjectResultIds = subjectResults.Select(sr => sr.SubjectResultId).ToList();

        var allSubjects = await _unitOfWork.SubjectRepository.GetAllAsync(cancellationToken);
        var subjectMap = allSubjects.ToDictionary(s => s.SubjectId);

        var allCourseSubjects = course != null 
            ? (await _unitOfWork.CourseSubjectRepository.GetAllAsync(cancellationToken)).Where(cs => cs.CourseId == course.CourseId).ToDictionary(cs => cs.SubjectId)
            : new Dictionary<int, CourseSubject>();

        // Assessments & Checklist Results
        var allAssessmentResults = await _unitOfWork.AssessmentResultRepository.GetAllAsync(cancellationToken);
        var assessmentResults = allAssessmentResults.Where(ar => subjectResultIds.Contains(ar.SubjectResultId)).ToList();
        var allAssessments = await _unitOfWork.AssessmentRepository.GetAllAsync(cancellationToken);
        var assessmentMap = allAssessments.ToDictionary(a => a.AssessmentId);

        var allPracticalResults = await _unitOfWork.PracticalChecklistResultRepository.GetAllAsync(cancellationToken);
        var practicalResults = allPracticalResults.Where(pr => subjectResultIds.Contains(pr.SubjectResultId)).ToList();
        var allPracticalChecklists = await _unitOfWork.PracticalChecklistRepository.GetAllAsync(cancellationToken);
        var practicalChecklistMap = allPracticalChecklists.ToDictionary(pc => pc.PracticalChecklistId);

        // Signoffs
        var allSignoffs = await _unitOfWork.SubjectSignoffRepository.GetAllAsync(cancellationToken);
        var signoffs = allSignoffs.Where(s => subjectResultIds.Contains(s.SubjectResultId) && !s.IsDeleted).ToList();

        // Evidences
        var allEvidences = await _unitOfWork.EvidenceFileRepository.GetAllAsync(cancellationToken);
        var evidences = allEvidences.Where(ev => subjectResultIds.Contains(ev.SubjectResultId) && !ev.IsDeleted).ToList();
        var allAttachments = await _unitOfWork.AttachmentRepository.GetAllAsync(cancellationToken);
        var evidenceAttachments = allAttachments
            .Where(a => a.OwnerType == nameof(EvidenceFile) && evidences.Select(ev => ev.EvidenceFileId).Contains(a.OwnerId) && !a.IsDeleted)
            .GroupBy(a => a.OwnerId)
            .ToDictionary(g => g.Key, g => g.First());

        // Sessions & Attendance
        var allSessions = trainingClass != null
            ? (await _unitOfWork.SessionRepository.GetAllAsync(cancellationToken)).Where(s => s.ClassId == trainingClass.ClassId && !s.IsDeleted).ToList()
            : new List<Session>();

        var allClassSubjects = trainingClass != null
            ? (await _unitOfWork.ClassSubjectRepository.GetAllAsync(cancellationToken)).Where(cs => cs.ClassId == trainingClass.ClassId).ToList()
            : new List<ClassSubject>();

        var allAttendanceRecords = await _unitOfWork.AttendanceRecordRepository.GetAllAsync(cancellationToken);
        var attendanceRecords = allAttendanceRecords.Where(ar => ar.EnrollmentId == etr.EnrollmentId && !ar.IsDeleted).ToList();
        var attendanceBySession = attendanceRecords.GroupBy(ar => ar.SessionId).ToDictionary(g => g.Key, g => g.First());

        var subjectItems = new List<EtrDossierSubjectItem>();
        foreach (var sr in subjectResults.OrderBy(sr => sr.SequenceNoSnapshot ?? sr.SubjectResultId))
        {
            var subj = subjectMap.GetValueOrDefault(sr.SubjectId);
            var cs = allCourseSubjects.GetValueOrDefault(sr.SubjectId);
            var signoff = signoffs.FirstOrDefault(s => s.SubjectResultId == sr.SubjectResultId);

            string subCode = sr.SubjectCodeSnapshot ?? subj?.SubjectCode ?? $"SUB-{sr.SubjectId}";
            string subName = sr.SubjectNameSnapshot ?? subj?.SubjectName ?? $"Môn học #{sr.SubjectId}";
            string subType = sr.SubjectTypeSnapshot ?? subj?.SubjectType ?? "Theory";
            int reqHours = sr.RequiredHoursSnapshot ?? cs?.RequiredHours ?? subj?.DefaultHours ?? 0;
            decimal passScore = sr.PassingScoreSnapshot ?? cs?.PassingScore ?? 70m;
            bool isMandatory = sr.IsMandatorySnapshot ?? cs?.IsMandatory ?? true;

            // Assessments for this subject
            var subAssessments = assessmentResults
                .Where(ar => ar.SubjectResultId == sr.SubjectResultId)
                .OrderBy(ar => ar.AttemptNo)
                .Select(ar => {
                    var def = assessmentMap.GetValueOrDefault(ar.AssessmentId);
                    return new EtrDossierAssessmentItem(
                        ar.AssessmentResultId,
                        ar.AssessmentId,
                        def?.ComponentName ?? $"Assessment #{ar.AssessmentId}",
                        def?.AssessmentType.ToString() ?? "Theory",
                        ar.WeightSnapshot ?? def?.Weight ?? 0m,
                        ar.PassingScoreSnapshot ?? def?.PassingScore ?? passScore,
                        ar.Score,
                        ar.ResultStatus,
                        ar.AttemptNo,
                        ar.TakenAt ?? ar.RecordedAt,
                        ar.Remark,
                        GetAccountDisplayName(ar.GradedByAccountId)
                    );
                }).ToList();

            // Practical Checklists
            var subChecklists = practicalResults
                .Where(pr => pr.SubjectResultId == sr.SubjectResultId)
                .Select(pr => {
                    var def = practicalChecklistMap.GetValueOrDefault(pr.PracticalChecklistId);
                    return new EtrDossierChecklistItem(
                        pr.PracticalChecklistResultId,
                        pr.PracticalChecklistId,
                        def?.ItemName ?? $"Checklist #{pr.PracticalChecklistId}",
                        pr.ResultStatus,
                        GetAccountDisplayName(pr.VerifiedByAccountId),
                        pr.CompletedAt,
                        pr.VerificationComment
                    );
                }).ToList();

            // Sessions for this subject in the class
            var subSessions = allSessions
                .Where(s => s.SubjectId == sr.SubjectId)
                .OrderBy(s => s.SessionDate ?? DateTime.MinValue)
                .Select(s => {
                    var ar = attendanceBySession.GetValueOrDefault(s.SessionId);
                    var assignedCs = allClassSubjects.FirstOrDefault(csub => csub.SubjectId == s.SubjectId);
                    int? assignedInstructorId = assignedCs?.InstructorAccountId;
                    int? signedInstructorId = ar?.InstructorSignedByAccountId ?? s.ConfirmedByAccountId;
                    DateTime? signedAt = ar?.InstructorSignedAt ?? s.ConfirmedAt;

                    return new EtrDossierSessionItem(
                        s.SessionId,
                        s.SessionTitle,
                        s.SessionDate,
                        s.TrainingType.ToString(),
                        s.LessonCode,
                        s.Location,
                        ar?.Status.ToString() ?? "NotRecorded",
                        ar?.FlightHours,
                        ar?.SimulatorHours,
                        ar?.DualHours,
                        ar?.SoloHours,
                        ar?.PicHours,
                        ar?.CrossCountryHours,
                        ar?.NightHours,
                        ar?.InstrumentHours,
                        ar?.DepartureIcao,
                        ar?.ArrivalIcao,
                        ar?.Route,
                        ar?.AircraftRegistration,
                        ar?.SimulatorDevice,
                        assignedInstructorId,
                        GetAccountDisplayName(assignedInstructorId),
                        signedInstructorId,
                        GetAccountDisplayName(signedInstructorId),
                        signedAt,
                        "Chưa có dữ liệu hệ thống ghi nhận người thực dạy riêng biệt",
                        "Chưa có bản chụp lịch sử hiệu lực chứng chỉ tại ngày dạy (Historical Snapshot); không suy đoán năng lực từ chữ ký.",
                        ar?.InstructorComments,
                        ar?.StudentComments
                    );
                }).ToList();

            // Evidence files for this subject
            var subEvidences = evidences
                .Where(ev => ev.SubjectResultId == sr.SubjectResultId)
                .Select(ev => {
                    var att = evidenceAttachments.GetValueOrDefault(ev.EvidenceFileId);

                    // Lọc URL attachment theo role và quyền sở hữu (TrainingManager chỉ xem metadata)
                    bool canViewEvidenceContent = roleName switch
                    {
                        "Admin" or "Academic" or "QA" or "Audit" or "Instructor" => true,
                        "Student" => enrollment.AccountId == currentAccountId,
                        "TrainingManager" => false,
                        _ => false
                    };

                    string evidenceUrl = canViewEvidenceContent ? (att?.Url ?? string.Empty) : string.Empty;

                    return new EtrDossierEvidenceItem(
                        ev.EvidenceFileId,
                        att?.FileName ?? $"Evidence-{ev.EvidenceFileId}",
                        evidenceUrl,
                        att?.MimeType ?? "application/octet-stream",
                        ev.VerificationStatus,
                        ev.VerificationComment,
                        GetAccountDisplayName(ev.VerifiedByAccountId),
                        ev.VerifiedAt,
                        GetAccountDisplayName(ev.UploadedByAccountId),
                        ev.UploadedAt
                    );
                }).ToList();

            subjectItems.Add(new EtrDossierSubjectItem(
                sr.SubjectResultId,
                sr.SubjectId,
                subCode,
                subName,
                subType,
                reqHours,
                passScore,
                isMandatory,
                sr.Status,
                sr.Score,
                sr.AttendanceRate,
                signoff != null,
                signoff?.SignoffAt,
                GetAccountDisplayName(signoff?.SignoffByAccountId),
                signoff?.Role,
                signoff?.Comment,
                sr.CarriedOverFromSubjectResultId.HasValue,
                subAssessments,
                subChecklists,
                subSessions,
                subEvidences
            ));
        }

        // 4. Credentials & Medical Summary (Strict Role Filtering)
        EtrDossierCredentialSummary? credentialsSummary = null;
        if (roleName != "Instructor" && learnerProfile != null)
        {
            // Masking LicenseNumber for TrainingManager, QA, Audit
            string? licenseNumberDisplay = learnerProfile.LicenseNumber;
            if (roleName is "QA" or "Audit" or "TrainingManager" && !string.IsNullOrEmpty(licenseNumberDisplay))
            {
                licenseNumberDisplay = licenseNumberDisplay.Length > 4 
                    ? $"***{licenseNumberDisplay[^4..]}" 
                    : "***";
            }

            // Credential Attachments (Only for Student self, Admin, Academic, QA, Audit)
            var credentialAttachments = new List<CredentialAttachmentDto>();
            bool canViewCredentialFiles = roleName is "Admin" or "Academic" or "QA" or "Audit" || (roleName == "Student" && enrollment.AccountId == currentAccountId);
            if (canViewCredentialFiles)
            {
                var userDocAttachments = allAttachments
                    .Where(a => a.OwnerType == nameof(UserProfile) && a.OwnerId == enrollment.AccountId && !a.IsDeleted)
                    .OrderByDescending(a => a.UploadedAt)
                    .Select(a => new CredentialAttachmentDto(
                        a.AttachmentId, a.OwnerId, a.DocType ?? "General", a.FileName, a.Url, a.MimeType, a.FileSize, a.UploadedAt, a.UploadedByAccountId
                    )).ToList();
                credentialAttachments = userDocAttachments;
            }

            credentialsSummary = new EtrDossierCredentialSummary(
                learnerProfile.IsCredentialsVerified,
                learnerProfile.LicenseType,
                licenseNumberDisplay,
                learnerProfile.LicenseExpiryDate,
                learnerProfile.MedicalClass,
                learnerProfile.MedicalExpiryDate,
                learnerProfile.IcaoElpLevel,
                learnerProfile.IcaoElpExpiryDate,
                learnerProfile.TypeRatings,
                credentialAttachments
            );
        }

        // 5. Approval History (Excluding internal technical audit log)
        var allApprovalRequests = await _unitOfWork.ApprovalRequestRepository.GetAllAsync(cancellationToken);
        var approvalRequest = allApprovalRequests.FirstOrDefault(ar => ar.ETRCourseRecordId == etrCourseRecordId);
        var approvalHistoryItems = new List<EtrDossierApprovalHistoryItem>();
        if (approvalRequest != null)
        {
            var allHistories = await _unitOfWork.ApprovalHistoryRepository.GetAllAsync(cancellationToken);
            approvalHistoryItems = allHistories
                .Where(ah => ah.ApprovalRequestId == approvalRequest.ApprovalRequestId)
                .OrderBy(ah => ah.ActionAt)
                .Select(ah => new EtrDossierApprovalHistoryItem(
                    ah.ApprovalHistoryId,
                    ah.ApprovalRequestId,
                    ah.ActionType,
                    ah.PreviousStatus,
                    ah.NewStatus,
                    ah.Comments,
                    ah.ActionByAccountId,
                    GetAccountDisplayName(ah.ActionByAccountId),
                    ah.ActionAt
                )).ToList();
        }

        // 6. Readiness Summary
        var (flightHours, simHours) = await GetQualifiedTrainingHoursAsync(etr.EnrollmentId, cancellationToken);
        int totalSub = subjectItems.Count;
        int passedSub = subjectItems.Count(s => s.Status == SubjectResultStatus.Passed || s.Status == SubjectResultStatus.Exempted);
        decimal avgAtt = subjectItems.Any(s => s.AttendanceRate.HasValue) 
            ? subjectItems.Where(s => s.AttendanceRate.HasValue).Average(s => s.AttendanceRate!.Value) 
            : 0m;

        var pendingConditions = new List<string>();
        var mandatoryPending = subjectItems.Where(s => s.IsMandatory && s.Status != SubjectResultStatus.Passed && s.Status != SubjectResultStatus.Exempted).ToList();
        if (mandatoryPending.Any())
        {
            pendingConditions.Add($"Còn {mandatoryPending.Count} môn bắt buộc chưa đạt ({string.Join(", ", mandatoryPending.Select(m => m.SubjectCode))})");
        }

        string overallStatus = (pendingConditions.Count == 0 && (etr.Status == EtrStatus.Verified || etr.Status == EtrStatus.Completed)) ? "Met" : "NotMet";

        var readiness = new EtrDossierReadinessSummary(
            totalSub,
            passedSub,
            Math.Round(avgAtt, 1),
            Math.Round(flightHours, 1),
            Math.Round(simHours, 1),
            overallStatus,
            pendingConditions
        );

        // 7. Allowed Actions for this caller
        var allowedActions = new List<string>();
        if (roleName is "Instructor" or "Academic" or "Admin" && (etr.Status == EtrStatus.Draft || etr.Status == EtrStatus.ReturnedForCorrection) && !etr.IsLocked)
        {
            allowedActions.Add("Submit");
        }
        if (roleName is "QA" or "Admin" && etr.Status == EtrStatus.Submitted)
        {
            allowedActions.Add("Verify");
            allowedActions.Add("Return");
        }
        if (roleName is "TrainingManager" or "Admin" && etr.Status == EtrStatus.Verified)
        {
            allowedActions.Add("Complete");
            allowedActions.Add("Return");
        }
        if (roleName is "Admin" && etr.Status == EtrStatus.Completed)
        {
            allowedActions.Add("Reopen");
        }
        if (roleName is "Admin" or "Audit" or "Academic" or "TrainingManager")
        {
            allowedActions.Add("ExportPdf");
        }

        return new EtrDossierResponse(
            etr.ETRCourseRecordId,
            etr.EnrollmentId,
            etr.Status,
            etr.IsLocked,
            courseInfo.VersionNo,
            etr.SubmittedAt,
            etr.VerifiedAt,
            etr.CompletedAt,
            etr.IssuedDate,
            etr.ExpiryDate,
            studentInfo,
            courseInfo,
            classInfo,
            readiness,
            subjectItems,
            credentialsSummary,
            approvalHistoryItems,
            allowedActions
        );
    }

    public async Task DeleteEtrAsync(int id, int deletedByAccountId, CancellationToken cancellationToken = default)
    {
        var etr = await _unitOfWork.ETRCourseRecordRepository.GetByIdAsync(id, cancellationToken);
        if (etr == null) throw new KeyNotFoundException("ETRCourseRecord not found.");

        if (etr.IsLocked)
        {
            throw new BusinessRuleViolationException("Không thể xóa hồ sơ ETR đã bị khóa.");
        }

        if (etr.Status != EtrStatus.Draft && etr.Status != EtrStatus.Cancelled)
        {
            throw new BusinessRuleViolationException($"Không thể xóa hồ sơ ETR ở trạng thái '{etr.Status}'. Chỉ có thể xóa hồ sơ ở trạng thái Bản nháp (Draft) hoặc Đã hủy (Cancelled).");
        }

        etr.IsDeleted = true;
        etr.DeletedAt = DateTime.UtcNow;
        etr.UpdatedAt = DateTime.UtcNow;
        etr.UpdatedByAccountId = deletedByAccountId;

        _unitOfWork.ETRCourseRecordRepository.Update(etr);

        await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            AccountId = deletedByAccountId,
            ActionType = AuditActionType.DELETE.ToString(),
            EntityName = nameof(ETRCourseRecord),
            RecordId = etr.ETRCourseRecordId,
            OldValue = etr.Status.ToString(),
            NewValue = "Deleted",
            Description = $"ETRCourseRecord #{etr.ETRCourseRecordId} soft-deleted"
        }, cancellationToken);

        await _unitOfWork.SaveAsync(cancellationToken);
    }

    public async Task<EtrRecordResponse> SubmitEtrAsync(int etrCourseRecordId, int accountId, CancellationToken cancellationToken = default)
    {
        var etr = await _unitOfWork.ETRCourseRecordRepository.GetWithSubjectResultsAsync(etrCourseRecordId, cancellationToken)
            ?? throw new KeyNotFoundException($"ETRCourseRecord not found.");

        if (etr.IsLocked) throw new BusinessRuleViolationException("ETR is locked.");

        // Check Instructor Scope (chỉ cho phép giảng viên nộp ETR cho học viên thuộc các lớp mình phân công)
        if (_currentUserService.RoleName == "Instructor")
        {
            var myEnrollmentIds = await GetInstructorEnrollmentIdsAsync(accountId, cancellationToken);
            if (!myEnrollmentIds.Contains(etr.EnrollmentId))
            {
                throw new ForbiddenAccessException("Bạn không được phân công giảng dạy học viên này.");
            }
        }

        var enrollment = await _unitOfWork.CourseEnrollmentRepository.GetByIdAsync(etr.EnrollmentId, cancellationToken)
            ?? throw new BusinessRuleViolationException("Enrollment not found.");

        var trainingClass = await _unitOfWork.ClassRepository.GetByIdAsync(enrollment.ClassId, cancellationToken)
            ?? throw new BusinessRuleViolationException("Class not found.");

        var courseSubjects = (await _unitOfWork.CourseSubjectRepository.GetAllAsync(cancellationToken))
            .Where(cs => cs.CourseId == trainingClass.CourseId && cs.IsMandatory).ToList();

        var subjectResults = etr.SubjectResults ?? Enumerable.Empty<SubjectResult>();
        bool hasSnapshots = subjectResults.Any(sr => sr.IsMandatorySnapshot.HasValue);

        // === PRE-VALIDATION ===

        // 1. Check all mandatory subjects are Passed or Exempted
        if (hasSnapshots)
        {
            var mandatorySubjectResults = subjectResults.Where(sr => sr.IsMandatorySnapshot == true).ToList();
            foreach (var sr in mandatorySubjectResults)
            {
                if (sr.Status != SubjectResultStatus.Passed && sr.Status != SubjectResultStatus.Exempted)
                {
                    var subName = sr.SubjectNameSnapshot ?? $"ID: {sr.SubjectId}";
                    throw new BusinessRuleViolationException($"Cannot submit ETR. Mandatory subject ({subName}) is not Passed or Exempted.");
                }
            }
        }
        else
        {
            foreach (var cs in courseSubjects)
            {
                var sr = subjectResults.FirstOrDefault(s => s.SubjectId == cs.SubjectId);
                if (sr == null || (sr.Status != SubjectResultStatus.Passed && sr.Status != SubjectResultStatus.Exempted))
                {
                    throw new BusinessRuleViolationException($"Cannot submit ETR. Mandatory subject (ID: {cs.SubjectId}) is not Passed or Exempted.");
                }
            }
        }

        // 2. Check attendance rate >= minimum threshold
        if (etr.SubjectResults != null)
        {
            foreach (var sr in etr.SubjectResults)
            {
                if ((sr.AttendanceRate ?? 0) < BusinessRuleEngine.MinimumAttendanceThreshold)
                {
                    throw new BusinessRuleViolationException($"Cannot submit ETR. Subject (ID: {sr.SubjectId}) attendance rate ({sr.AttendanceRate}%) is below minimum threshold ({BusinessRuleEngine.MinimumAttendanceThreshold}%).");
                }
            }
        }

        // 3. Check all evidence is Verified
        var allEvidences = (await _unitOfWork.EvidenceFileRepository.GetAllAsync(cancellationToken) ?? Enumerable.Empty<EvidenceFile>()).ToList();
        var etrSubjectIds = etr.SubjectResults?.Select(sr => sr.SubjectResultId).ToList() ?? new List<int>();
        var pendingEvidences = allEvidences
            .Where(e => etrSubjectIds.Contains(e.SubjectResultId) && e.VerificationStatus != "Verified" && !e.IsDeleted)
            .ToList();

        if (pendingEvidences.Any())
        {
            throw new BusinessRuleViolationException($"Cannot submit ETR. {pendingEvidences.Count} evidence file(s) are not yet Verified.");
        }

        // 4. Check all subject signoffs exist
        var allSignoffs = (await _unitOfWork.SubjectSignoffRepository.GetAllAsync(cancellationToken) ?? Enumerable.Empty<SubjectSignoff>()).ToList();
        foreach (var sr in etr.SubjectResults ?? Enumerable.Empty<SubjectResult>())
        {
            var hasSignoff = allSignoffs.Any(s => s.SubjectResultId == sr.SubjectResultId && !s.IsDeleted);
            if (!hasSignoff)
            {
                throw new BusinessRuleViolationException($"Cannot submit ETR. Subject (ID: {sr.SubjectId}) has not been signed off by instructor.");
            }
        }
        
        // 5. Check mandatory CompletionRequirements configured for the course
        // Filtered by CourseVersionNo (snapshotted at Enroll time), NOT "whatever requirements exist
        // right now" — so a mid-course rule change never retroactively re-evaluates this learner
        // against a threshold that didn't exist when they enrolled. See Course/CompletionRequirement
        // VersionNo docs.
        var completionRequirements = (await _unitOfWork.CompletionRequirementRepository.GetAllAsync(cancellationToken) ?? Enumerable.Empty<CompletionRequirement>())
            .Where(cr => cr.CourseId == trainingClass.CourseId && cr.IsMandatory && cr.VersionNo == etr.CourseVersionNo).ToList();

        var (qualifiedFlightHours, qualifiedSimHours) = await GetQualifiedTrainingHoursAsync(etr.EnrollmentId, cancellationToken);

        foreach (var requirement in completionRequirements)
        {
            switch (requirement.RequirementType)
            {
                case "MinAttendance":
                    var minAttendance = requirement.ThresholdValue ?? BusinessRuleEngine.MinimumAttendanceThreshold;
                    if (etr.SubjectResults != null && etr.SubjectResults.Any(sr => (sr.AttendanceRate ?? 0) < minAttendance))
                    {
                        throw new BusinessRuleViolationException($"Cannot submit ETR. Completion requirement '{requirement.RequirementName}' not met: attendance below {minAttendance}%.");
                    }
                    break;

                case "MinFlightHours":
                    var minFlightHours = requirement.ThresholdValue ?? 0m;
                    if (qualifiedFlightHours < minFlightHours)
                    {
                        throw new BusinessRuleViolationException($"Cannot submit ETR. Completion requirement '{requirement.RequirementName}' not met: qualified flight hours ({qualifiedFlightHours:0.##}h) below required {minFlightHours:0.##}h.");
                    }
                    break;

                case "MinSimulatorHours":
                    var minSimHours = requirement.ThresholdValue ?? 0m;
                    if (qualifiedSimHours < minSimHours)
                    {
                        throw new BusinessRuleViolationException($"Cannot submit ETR. Completion requirement '{requirement.RequirementName}' not met: qualified simulator hours ({qualifiedSimHours:0.##}h) below required {minSimHours:0.##}h.");
                    }
                    break;

                case "AllAssessmentsPassed":
                    if (hasSnapshots)
                    {
                        var mandatorySRs = subjectResults.Where(sr => sr.IsMandatorySnapshot == true).ToList();
                        if (mandatorySRs.Any(sr => sr.Status != SubjectResultStatus.Passed && sr.Status != SubjectResultStatus.Exempted))
                        {
                            throw new BusinessRuleViolationException($"Cannot submit ETR. Completion requirement '{requirement.RequirementName}' not met: not all mandatory subjects are Passed or Exempted.");
                        }
                    }
                    else
                    {
                        foreach (var cs in courseSubjects)
                        {
                            var sr = subjectResults.FirstOrDefault(s => s.SubjectId == cs.SubjectId);
                            if (sr == null || (sr.Status != SubjectResultStatus.Passed && sr.Status != SubjectResultStatus.Exempted))
                            {
                                throw new BusinessRuleViolationException($"Cannot submit ETR. Completion requirement '{requirement.RequirementName}' not met: not all mandatory subjects are Passed or Exempted.");
                            }
                        }
                    }
                    break;

                case "AllChecklistsSignedOff":
                    var subjectResultIds = subjectResults.Select(sr => sr.SubjectResultId).ToList();
                    var checklistResults = (await _unitOfWork.PracticalChecklistResultRepository.GetAllAsync(cancellationToken) ?? Enumerable.Empty<PracticalChecklistResult>())
                        .Where(r => subjectResultIds.Contains(r.SubjectResultId) && !r.IsDeleted).ToList();
                    var latestChecklistResults = checklistResults
                        .GroupBy(r => r.PracticalChecklistId)
                        .Select(g => g.OrderByDescending(r => r.CompletedAt ?? r.CreatedAt).ThenByDescending(r => r.PracticalChecklistResultId).First())
                        .ToList();
                    bool hasChecklistSnapshots = latestChecklistResults.Any(r => r.IsMandatorySnapshot.HasValue);

                    if (hasChecklistSnapshots)
                    {
                        var mandatoryChecklistResults = latestChecklistResults.Where(r => r.IsMandatorySnapshot == true).ToList();
                        if (mandatoryChecklistResults.Any(r => r.ResultStatus != "Passed"))
                        {
                            throw new BusinessRuleViolationException($"Cannot submit ETR. Completion requirement '{requirement.RequirementName}' not met: not all mandatory practical checklists are signed off.");
                        }
                    }
                    else
                    {
                        var mandatoryChecklists = (await _unitOfWork.PracticalChecklistRepository.GetAllAsync(cancellationToken) ?? Enumerable.Empty<PracticalChecklist>())
                            .Where(pc => pc.CourseId == trainingClass.CourseId && pc.IsRequired && !pc.IsDeleted).ToList();

                        var hasUnpassedMandatoryChecklist = latestChecklistResults.Any(r =>
                            mandatoryChecklists.Any(c => c.PracticalChecklistId == r.PracticalChecklistId)
                            && r.ResultStatus != "Passed");

                        if (hasUnpassedMandatoryChecklist || mandatoryChecklists.Any(c => !latestChecklistResults.Any(r => r.PracticalChecklistId == c.PracticalChecklistId && r.ResultStatus == "Passed")))
                        {
                            throw new BusinessRuleViolationException($"Cannot submit ETR. Completion requirement '{requirement.RequirementName}' not met: not all mandatory practical checklists are signed off.");
                        }
                    }
                    break;

                default:
                    // Free-text/advisory requirement (RequirementType not set) — not machine-enforced.
                    break;
            }
        }

        return await _unitOfWork.ExecuteInStrategyAsync(async (ct) =>
        {
            await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                // === AUDIT LOG ===
                var auditLog = new AuditLog
                {
                    ETRRecordId = etrCourseRecordId,
                    AccountId = accountId,
                    ActionType = AuditActionType.SUBMIT.ToString(),
                    EntityName = nameof(ETRCourseRecord),
                    RecordId = etrCourseRecordId,
                    OldValue = etr.Status.ToString(),
                    NewValue = "Submitted",
                    Description = $"ETR #{etrCourseRecordId} submitted for QA verification"
                };
                await _unitOfWork.AuditLogRepository.AddAsync(auditLog, ct);

                etr.Status = EtrStatus.Submitted;
                etr.SubmittedAt = DateTime.UtcNow;
                etr.UpdatedAt = DateTime.UtcNow;
                etr.UpdatedByAccountId = accountId;

                _unitOfWork.ETRCourseRecordRepository.Update(etr);

                // Auto-provision ApprovalRequest for QA / TrainingManager workflow in the same transaction
                var existingApproval = (await _unitOfWork.ApprovalRequestRepository.GetAllAsync(ct))
                    .FirstOrDefault(a => a.ETRCourseRecordId == etrCourseRecordId && a.CurrentStatus == "Pending");
                if (existingApproval == null)
                {
                    var approvalRequest = new ApprovalRequest
                    {
                        ETRCourseRecordId = etrCourseRecordId,
                        CurrentStatus = "Pending",
                        SubmittedByAccountId = accountId,
                        SubmittedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        CreatedByAccountId = accountId
                    };
                    await _unitOfWork.ApprovalRequestRepository.AddAsync(approvalRequest, ct);
                }

                await _unitOfWork.CommitTransactionAsync(ct);
                return new EtrRecordResponse(etr.ETRCourseRecordId, etr.EnrollmentId, etr.Status, etr.IsLocked, etr.SubmittedAt, etr.VerifiedAt, etr.CompletedAt, etr.IssuedDate, etr.ExpiryDate, etr.PreviousRecordId);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }, cancellationToken);
    }

    public async Task<EtrCompletionProgressResponse> GetCompletionProgressAsync(int etrCourseRecordId, CancellationToken cancellationToken = default)
    {
        var etr = await _unitOfWork.ETRCourseRecordRepository.GetWithSubjectResultsAsync(etrCourseRecordId, cancellationToken)
            ?? throw new KeyNotFoundException($"ETRCourseRecord not found.");

        var enrollment = await _unitOfWork.CourseEnrollmentRepository.GetByIdAsync(etr.EnrollmentId, cancellationToken)
            ?? throw new BusinessRuleViolationException("Enrollment not found.");
        var trainingClass = await _unitOfWork.ClassRepository.GetByIdAsync(enrollment.ClassId, cancellationToken)
            ?? throw new BusinessRuleViolationException("Class not found.");

        var courseSubjects = (await _unitOfWork.CourseSubjectRepository.GetAllAsync(cancellationToken))
            .Where(cs => cs.CourseId == trainingClass.CourseId && cs.IsMandatory).ToList();
        var subjects = (await _unitOfWork.SubjectRepository.GetAllAsync(cancellationToken)).ToDictionary(s => s.SubjectId, s => s);

        var checks = new List<CompletionCheckItem>();

        // Mirrors SubmitEtrAsync's pre-validation checks exactly, but records pass/fail instead
        // of throwing on the first failure — read-only, does not change ETR state.

        // 1. Mandatory subjects Passed/Exempted (one check per subject)
        var subjectResultsList = etr.SubjectResults?.ToList() ?? new List<SubjectResult>();
        bool hasSnapshots = subjectResultsList.Any(sr => sr.IsMandatorySnapshot.HasValue);

        if (hasSnapshots)
        {
            var mandatorySubjectsList = subjectResultsList.Where(sr => sr.IsMandatorySnapshot == true).ToList();
            foreach (var sr in mandatorySubjectsList)
            {
                var subjectName = sr.SubjectNameSnapshot ?? subjects.GetValueOrDefault(sr.SubjectId)?.SubjectName ?? $"Subject #{sr.SubjectId}";
                var isMet = sr.Status == SubjectResultStatus.Passed || sr.Status == SubjectResultStatus.Exempted;
                checks.Add(new CompletionCheckItem($"Subject Passed/Exempted: {subjectName}", true, isMet, sr.Status.ToString()));
            }
        }
        else
        {
            foreach (var cs in courseSubjects)
            {
                var sr = subjectResultsList.FirstOrDefault(s => s.SubjectId == cs.SubjectId);
                var subjectName = subjects.GetValueOrDefault(cs.SubjectId)?.SubjectName ?? $"Subject #{cs.SubjectId}";
                var isMet = sr != null && (sr.Status == SubjectResultStatus.Passed || sr.Status == SubjectResultStatus.Exempted);
                checks.Add(new CompletionCheckItem($"Subject Passed/Exempted: {subjectName}", true, isMet, sr?.Status.ToString() ?? "(no result yet)"));
            }
        }

        // 2. Attendance rate >= minimum threshold (one check per subject result)
        foreach (var sr in etr.SubjectResults ?? Enumerable.Empty<SubjectResult>())
        {
            var subjectName = sr.SubjectNameSnapshot ?? subjects.GetValueOrDefault(sr.SubjectId)?.SubjectName ?? $"Subject #{sr.SubjectId}";
            var isMet = (sr.AttendanceRate ?? 0) >= BusinessRuleEngine.MinimumAttendanceThreshold;
            checks.Add(new CompletionCheckItem($"Attendance >= {BusinessRuleEngine.MinimumAttendanceThreshold}%: {subjectName}", true, isMet, $"{sr.AttendanceRate ?? 0}%"));
        }

        // 3. All evidence Verified (single aggregate check)
        var allEvidences = (await _unitOfWork.EvidenceFileRepository.GetAllAsync(cancellationToken) ?? Enumerable.Empty<EvidenceFile>()).ToList();
        var etrSubjectIds = etr.SubjectResults?.Select(sr => sr.SubjectResultId).ToList() ?? new List<int>();
        var pendingEvidenceCount = allEvidences
            .Count(e => etrSubjectIds.Contains(e.SubjectResultId) && e.VerificationStatus != "Verified" && !e.IsDeleted);
        checks.Add(new CompletionCheckItem("All evidence Verified", true, pendingEvidenceCount == 0, $"{pendingEvidenceCount} pending"));

        // 4. Subject signoffs (one check per subject result)
        var allSignoffs = (await _unitOfWork.SubjectSignoffRepository.GetAllAsync(cancellationToken) ?? Enumerable.Empty<SubjectSignoff>()).ToList();
        foreach (var sr in etr.SubjectResults ?? Enumerable.Empty<SubjectResult>())
        {
            var subjectName = sr.SubjectNameSnapshot ?? subjects.GetValueOrDefault(sr.SubjectId)?.SubjectName ?? $"Subject #{sr.SubjectId}";
            var isMet = allSignoffs.Any(s => s.SubjectResultId == sr.SubjectResultId && !s.IsDeleted);
            checks.Add(new CompletionCheckItem($"Instructor Signoff: {subjectName}", true, isMet, isMet ? "Signed off" : "Not signed off"));
        }

        // 5. Mandatory CompletionRequirements configured for the course
        // Filtered by CourseVersionNo (snapshotted at Enroll time), NOT "whatever requirements exist
        // right now" — so a mid-course rule change never retroactively re-evaluates this learner
        // against a threshold that didn't exist when they enrolled. See Course/CompletionRequirement
        // VersionNo docs.
        var completionRequirements = (await _unitOfWork.CompletionRequirementRepository.GetAllAsync(cancellationToken) ?? Enumerable.Empty<CompletionRequirement>())
            .Where(cr => cr.CourseId == trainingClass.CourseId && cr.IsMandatory && cr.VersionNo == etr.CourseVersionNo).ToList();

        var (qualifiedFlightHours, qualifiedSimHours) = await GetQualifiedTrainingHoursAsync(etr.EnrollmentId, cancellationToken);

        foreach (var requirement in completionRequirements)
        {
            bool isMet;
            string? detail = null;
            switch (requirement.RequirementType)
            {
                case "MinAttendance":
                    var minAttendance = requirement.ThresholdValue ?? BusinessRuleEngine.MinimumAttendanceThreshold;
                    isMet = etr.SubjectResults == null || !etr.SubjectResults.Any(sr => (sr.AttendanceRate ?? 0) < minAttendance);
                    detail = $"{minAttendance}% min";
                    break;

                case "MinFlightHours":
                    var minFlight = requirement.ThresholdValue ?? 0m;
                    isMet = qualifiedFlightHours >= minFlight;
                    detail = $"{qualifiedFlightHours:0.##} / {minFlight:0.##} hours";
                    break;

                case "MinSimulatorHours":
                    var minSim = requirement.ThresholdValue ?? 0m;
                    isMet = qualifiedSimHours >= minSim;
                    detail = $"{qualifiedSimHours:0.##} / {minSim:0.##} hours";
                    break;

                case "AllAssessmentsPassed":
                    if (hasSnapshots)
                    {
                        var mandatorySRs = subjectResultsList.Where(sr => sr.IsMandatorySnapshot == true).ToList();
                        isMet = mandatorySRs.All(sr => sr.Status == SubjectResultStatus.Passed || sr.Status == SubjectResultStatus.Exempted);
                    }
                    else
                    {
                        isMet = courseSubjects.All(cs =>
                        {
                            var sr = subjectResultsList.FirstOrDefault(s => s.SubjectId == cs.SubjectId);
                            return sr != null && (sr.Status == SubjectResultStatus.Passed || sr.Status == SubjectResultStatus.Exempted);
                        });
                    }
                    break;

                case "AllChecklistsSignedOff":
                    var subjectResultIds = subjectResultsList.Select(sr => sr.SubjectResultId).ToList();
                    var checklistResults = (await _unitOfWork.PracticalChecklistResultRepository.GetAllAsync(cancellationToken) ?? Enumerable.Empty<PracticalChecklistResult>())
                        .Where(r => subjectResultIds.Contains(r.SubjectResultId) && !r.IsDeleted).ToList();
                    var latestChecklistResults = checklistResults
                        .GroupBy(r => r.PracticalChecklistId)
                        .Select(g => g.OrderByDescending(r => r.CompletedAt ?? r.CreatedAt).ThenByDescending(r => r.PracticalChecklistResultId).First())
                        .ToList();
                    bool hasChecklistSnapshots = latestChecklistResults.Any(r => r.IsMandatorySnapshot.HasValue);

                    if (hasChecklistSnapshots)
                    {
                        var mandatoryChecklistResults = latestChecklistResults.Where(r => r.IsMandatorySnapshot == true).ToList();
                        isMet = mandatoryChecklistResults.All(r => r.ResultStatus == "Passed");
                    }
                    else
                    {
                        var mandatoryChecklists = (await _unitOfWork.PracticalChecklistRepository.GetAllAsync(cancellationToken) ?? Enumerable.Empty<PracticalChecklist>())
                            .Where(pc => pc.CourseId == trainingClass.CourseId && pc.IsRequired && !pc.IsDeleted).ToList();
                        isMet = mandatoryChecklists.All(c => latestChecklistResults.Any(r => r.PracticalChecklistId == c.PracticalChecklistId && r.ResultStatus == "Passed"));
                    }
                    break;

                default:
                    // Free-text/advisory requirement — not machine-evaluated, always shown as met.
                    isMet = true;
                    break;
            }

            checks.Add(new CompletionCheckItem($"Completion Requirement: {requirement.RequirementName}", true, isMet, detail));
        }

        var metCount = checks.Count(c => c.IsMet);
        var percent = checks.Count == 0 ? 100m : Math.Round((decimal)metCount / checks.Count * 100, 2);

        return new EtrCompletionProgressResponse(etr.ETRCourseRecordId, checks.Count, metCount, percent, checks);
    }

    public async Task<EtrReadinessResponse> GetReadinessByEnrollmentAsync(int enrollmentId, CancellationToken cancellationToken = default)
    {
        var allEtrs = await _unitOfWork.ETRCourseRecordRepository.GetAllAsync(cancellationToken);
        var etr = allEtrs.FirstOrDefault(e => e.EnrollmentId == enrollmentId && !e.IsDeleted)
            ?? throw new KeyNotFoundException($"Không tìm thấy hồ sơ ETR cho Enrollment #{enrollmentId}.");

        return await GetReadinessAssessmentAsync(etr.ETRCourseRecordId, cancellationToken);
    }

    public async Task<EtrReadinessResponse> GetReadinessAssessmentAsync(int etrCourseRecordId, CancellationToken cancellationToken = default)
    {
        var etr = await _unitOfWork.ETRCourseRecordRepository.GetWithSubjectResultsAsync(etrCourseRecordId, cancellationToken)
            ?? throw new KeyNotFoundException($"ETRCourseRecord #{etrCourseRecordId} not found.");

        var enrollment = await _unitOfWork.CourseEnrollmentRepository.GetByIdAsync(etr.EnrollmentId, cancellationToken)
            ?? throw new KeyNotFoundException("Enrollment not found.");

        var trainingClass = await _unitOfWork.ClassRepository.GetByIdAsync(enrollment.ClassId, cancellationToken)
            ?? throw new KeyNotFoundException("Class not found.");

        var course = await _unitOfWork.CourseRepository.GetByIdAsync(trainingClass.CourseId, cancellationToken)
            ?? throw new KeyNotFoundException("Course not found.");

        var allProfiles = await _unitOfWork.UserProfileRepository.GetAllIncludingDeletedAsync(cancellationToken);
        var studentProfile = allProfiles.FirstOrDefault(p => p.AccountId == enrollment.AccountId);
        string studentName = studentProfile?.FullName ?? $"Student #{enrollment.AccountId}";

        // 1. Phân quyền truy cập (Access Control)
        bool isPrivilegedRole = _currentUserService.RoleName is "Admin" or "Academic" or "TrainingManager" or "QA" or "Audit";
        bool isStudent = _currentUserService.RoleName is "Student" or "Learner";
        bool isInstructor = _currentUserService.RoleName == "Instructor";
        bool isOwn = _currentUserService.AccountId.HasValue && _currentUserService.AccountId.Value == enrollment.AccountId;

        if (isStudent && !isOwn)
        {
            throw new UnauthorizedAccessException("Học viên chỉ có quyền xem đánh giá mức độ sẵn sàng của chính mình.");
        }
        else if (isInstructor && !isOwn)
        {
            var myEnrollmentIds = await GetInstructorEnrollmentIdsAsync(_currentUserService.AccountId!.Value, cancellationToken);
            if (!myEnrollmentIds.Contains(etr.EnrollmentId))
            {
                throw new KeyNotFoundException($"Không tìm thấy dữ liệu học viên thuộc các lớp được phân công.");
            }
        }
        else if (!isPrivilegedRole && !isOwn)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền xem thông tin sẵn sàng của hồ sơ này.");
        }

        // 2. Lấy dữ liệu môn học, kết quả, chuyên cần, giờ bay/SIM
        var courseSubjects = (await _unitOfWork.CourseSubjectRepository.GetAllAsync(cancellationToken) ?? Enumerable.Empty<CourseSubject>())
            .Where(cs => cs.CourseId == trainingClass.CourseId).ToList();
        var subjects = (await _unitOfWork.SubjectRepository.GetAllAsync(cancellationToken) ?? Enumerable.Empty<Subject>())
            .ToDictionary(s => s.SubjectId, s => s);

        var (qualifiedFlightHours, qualifiedSimHours) = await GetQualifiedTrainingHoursAsync(etr.EnrollmentId, cancellationToken);

        var conditions = new List<ReadinessItemDto>();
        var warnings = new List<ReadinessWarningDto>();

        // 3. Đánh giá Môn học Bắt buộc (Mandatory Subjects Assessment)
        var subjectResultsList = etr.SubjectResults?.ToList() ?? new List<SubjectResult>();
        bool hasSnapshots = subjectResultsList.Any(sr => sr.IsMandatorySnapshot.HasValue);

        int mandatoryCount = 0;
        int passedCount = 0;
        int failedCount = 0;
        int pendingCount = 0;

        if (hasSnapshots)
        {
            var mandatorySRs = subjectResultsList.Where(sr => sr.IsMandatorySnapshot == true).ToList();
            mandatoryCount = mandatorySRs.Count;
            passedCount = mandatorySRs.Count(sr => sr.Status == SubjectResultStatus.Passed || sr.Status == SubjectResultStatus.Exempted);
            failedCount = mandatorySRs.Count(sr => sr.Status == SubjectResultStatus.Failed);
            pendingCount = mandatorySRs.Count(sr => sr.Status == SubjectResultStatus.Pending);
        }
        else
        {
            var mandatoryCourseSubjects = courseSubjects.Where(cs => cs.IsMandatory).ToList();
            mandatoryCount = mandatoryCourseSubjects.Count;
            foreach (var cs in mandatoryCourseSubjects)
            {
                var sr = subjectResultsList.FirstOrDefault(s => s.SubjectId == cs.SubjectId);
                if (sr == null || sr.Status == SubjectResultStatus.Pending)
                    pendingCount++;
                else if (sr.Status == SubjectResultStatus.Passed || sr.Status == SubjectResultStatus.Exempted)
                    passedCount++;
                else if (sr.Status == SubjectResultStatus.Failed)
                    failedCount++;
            }
        }

        ReadinessStatus subjectStatus = ReadinessStatus.Met;
        string subjectExpl = $"Đã hoàn thành {passedCount}/{mandatoryCount} môn học bắt buộc.";
        if (mandatoryCount == 0)
        {
            subjectStatus = ReadinessStatus.Met;
            subjectExpl = "Không có môn học bắt buộc nào được cấu hình.";
        }
        else if (passedCount == mandatoryCount)
        {
            subjectStatus = ReadinessStatus.Met;
            subjectExpl = $"Tất cả {mandatoryCount} môn học bắt buộc đã Đạt (Passed) hoặc Miễn (Exempted).";
        }
        else if (failedCount > 0)
        {
            subjectStatus = ReadinessStatus.NotMet;
            subjectExpl = $"Có {failedCount} môn học bắt buộc chưa đạt (Failed). Cần thi lại hoặc học lại.";
        }
        else if (pendingCount > 0)
        {
            subjectStatus = (passedCount == 0) ? ReadinessStatus.NoData : ReadinessStatus.NotMet;
            subjectExpl = $"Còn {pendingCount}/{mandatoryCount} môn học bắt buộc đang học hoặc chưa có kết quả thi.";
        }

        conditions.Add(new ReadinessItemDto(
            ConditionCode: "MANDATORY_SUBJECTS",
            ConditionName: "Môn học bắt buộc",
            Status: subjectStatus,
            CurrentValue: passedCount,
            ThresholdValue: mandatoryCount,
            Unit: "môn",
            IsMandatory: true,
            Explanation: subjectExpl,
            Category: "Academic"
        ));

        // 4. Đánh giá Chuyên cần Tổng thể (Overall Attendance)
        var subjectResultsWithAttendance = subjectResultsList.Where(sr => sr.AttendanceRate.HasValue).ToList();
        if (subjectResultsWithAttendance.Count == 0)
        {
            conditions.Add(new ReadinessItemDto(
                ConditionCode: "MIN_ATTENDANCE",
                ConditionName: "Tỷ lệ chuyên cần tối thiểu",
                Status: ReadinessStatus.NoData,
                CurrentValue: null,
                ThresholdValue: BusinessRuleEngine.MinimumAttendanceThreshold,
                Unit: "%",
                IsMandatory: true,
                Explanation: $"Chưa có buổi học nào được xác nhận điểm danh (ngưỡng tối thiểu {BusinessRuleEngine.MinimumAttendanceThreshold}%).",
                Category: "Attendance"
            ));
        }
        else
        {
            decimal minAtt = subjectResultsWithAttendance.Min(sr => sr.AttendanceRate!.Value);
            bool attMet = minAtt >= BusinessRuleEngine.MinimumAttendanceThreshold;
            conditions.Add(new ReadinessItemDto(
                ConditionCode: "MIN_ATTENDANCE",
                ConditionName: "Tỷ lệ chuyên cần tối thiểu",
                Status: attMet ? ReadinessStatus.Met : ReadinessStatus.NotMet,
                CurrentValue: minAtt,
                ThresholdValue: BusinessRuleEngine.MinimumAttendanceThreshold,
                Unit: "%",
                IsMandatory: true,
                Explanation: attMet
                    ? $"Tất cả các môn đều đạt chuyên cần tối thiểu (môn thấp nhất: {minAtt}% >= {BusinessRuleEngine.MinimumAttendanceThreshold}%)."
                    : $"Có môn học có chuyên cần ({minAtt}%) dưới ngưỡng tối thiểu {BusinessRuleEngine.MinimumAttendanceThreshold}%.",
                Category: "Attendance"
            ));
        }

        // 5. Đánh giá Checklist Thực hành (Practical Checklists)
        var subjectResultIds = subjectResultsList.Select(sr => sr.SubjectResultId).ToList();
        var checklistResults = (await _unitOfWork.PracticalChecklistResultRepository.GetAllAsync(cancellationToken) ?? Enumerable.Empty<PracticalChecklistResult>())
            .Where(r => subjectResultIds.Contains(r.SubjectResultId) && !r.IsDeleted).ToList();
        var latestChecklistResults = checklistResults
            .GroupBy(r => r.PracticalChecklistId)
            .Select(g => g.OrderByDescending(r => r.CompletedAt ?? r.CreatedAt).ThenByDescending(r => r.PracticalChecklistResultId).First())
            .ToList();

        var mandatoryChecklists = (await _unitOfWork.PracticalChecklistRepository.GetAllAsync(cancellationToken) ?? Enumerable.Empty<PracticalChecklist>())
            .Where(pc => pc.CourseId == trainingClass.CourseId && pc.IsRequired && !pc.IsDeleted).ToList();

        if (mandatoryChecklists.Count > 0)
        {
            int chkPassed = mandatoryChecklists.Count(c => latestChecklistResults.Any(r => r.PracticalChecklistId == c.PracticalChecklistId && r.ResultStatus == "Passed"));
            ReadinessStatus chkStatus = (chkPassed == mandatoryChecklists.Count)
                ? ReadinessStatus.Met
                : (chkPassed == 0 && latestChecklistResults.Count == 0 ? ReadinessStatus.NoData : ReadinessStatus.NotMet);

            conditions.Add(new ReadinessItemDto(
                ConditionCode: "PRACTICAL_CHECKLISTS",
                ConditionName: "Checklist thực hành kỹ năng",
                Status: chkStatus,
                CurrentValue: chkPassed,
                ThresholdValue: mandatoryChecklists.Count,
                Unit: "checklist",
                IsMandatory: true,
                Explanation: (chkStatus == ReadinessStatus.Met)
                    ? $"Đã hoàn thành và ký duyệt toàn bộ {mandatoryChecklists.Count} checklist thực hành bắt buộc."
                    : $"Đã hoàn thành {chkPassed}/{mandatoryChecklists.Count} checklist thực hành bắt buộc.",
                Category: "Practical"
            ));
        }

        // 6. Đánh giá Minh chứng Đào tạo (Evidence Files)
        var allEvidences = (await _unitOfWork.EvidenceFileRepository.GetAllAsync(cancellationToken) ?? Enumerable.Empty<EvidenceFile>())
            .Where(e => subjectResultIds.Contains(e.SubjectResultId) && !e.IsDeleted).ToList();
        if (allEvidences.Count > 0)
        {
            int pendingEv = allEvidences.Count(e => e.VerificationStatus != "Verified");
            bool evMet = pendingEv == 0;
            conditions.Add(new ReadinessItemDto(
                ConditionCode: "EVIDENCE_FILES",
                ConditionName: "Minh chứng đào tạo",
                Status: evMet ? ReadinessStatus.Met : ReadinessStatus.ReviewRequired,
                CurrentValue: allEvidences.Count - pendingEv,
                ThresholdValue: allEvidences.Count,
                Unit: "tệp",
                IsMandatory: true,
                Explanation: evMet
                    ? $"Tất cả {allEvidences.Count} tệp minh chứng đã được xác minh hợp lệ (Verified)."
                    : $"Còn {pendingEv}/{allEvidences.Count} tệp minh chứng đang chờ QA thẩm định.",
                Category: "Evidence"
            ));
        }

        // 7. Đánh giá Chữ ký Giảng viên (Instructor Signoffs)
        var allSignoffs = (await _unitOfWork.SubjectSignoffRepository.GetAllAsync(cancellationToken) ?? Enumerable.Empty<SubjectSignoff>()).ToList();
        int signedSubjects = subjectResultsList.Count(sr => allSignoffs.Any(s => s.SubjectResultId == sr.SubjectResultId && !s.IsDeleted));
        int totalSubjects = subjectResultsList.Count;
        if (totalSubjects > 0)
        {
            bool signMet = signedSubjects == totalSubjects;
            conditions.Add(new ReadinessItemDto(
                ConditionCode: "INSTRUCTOR_SIGNOFFS",
                ConditionName: "Chữ ký xác nhận môn học của Giảng viên",
                Status: signMet ? ReadinessStatus.Met : ReadinessStatus.NotMet,
                CurrentValue: signedSubjects,
                ThresholdValue: totalSubjects,
                Unit: "môn",
                IsMandatory: true,
                Explanation: signMet
                    ? $"Toàn bộ {totalSubjects} môn học đã được giảng viên ký xác nhận hoàn thành."
                    : $"Còn {totalSubjects - signedSubjects}/{totalSubjects} môn học chưa có chữ ký xác nhận của giảng viên.",
                Category: "Signoff"
            ));
        }

        // 8. Đánh giá CompletionRequirements theo CourseVersionNo (Flight/Sim Hours & Custom Requirements)
        var completionRequirements = (await _unitOfWork.CompletionRequirementRepository.GetAllAsync(cancellationToken) ?? Enumerable.Empty<CompletionRequirement>())
            .Where(cr => cr.CourseId == trainingClass.CourseId && cr.VersionNo == etr.CourseVersionNo)
            .OrderBy(cr => cr.DisplayOrder)
            .ToList();

        foreach (var req in completionRequirements)
        {
            switch (req.RequirementType)
            {
                case "MinFlightHours":
                    var minFlight = req.ThresholdValue ?? 0m;
                    ReadinessStatus flightStatus;
                    string flightExpl;
                    if (qualifiedFlightHours >= minFlight)
                    {
                        flightStatus = ReadinessStatus.Met;
                        flightExpl = $"Đã tích lũy {qualifiedFlightHours:0.##} / {minFlight:0.##} giờ bay thực tế hợp lệ.";
                    }
                    else if (qualifiedFlightHours == 0)
                    {
                        flightStatus = (minFlight == 0) ? ReadinessStatus.Met : ReadinessStatus.NoData;
                        flightExpl = $"Chưa có giờ bay thực tế hợp lệ nào được ghi nhận (0.00 / {minFlight:0.##} giờ).";
                    }
                    else
                    {
                        flightStatus = ReadinessStatus.NotMet;
                        flightExpl = $"Còn thiếu {(minFlight - qualifiedFlightHours):0.##} giờ bay thực tế ({qualifiedFlightHours:0.##} / {minFlight:0.##} giờ).";
                    }

                    conditions.Add(new ReadinessItemDto(
                        ConditionCode: "MIN_FLIGHT_HOURS",
                        ConditionName: req.RequirementName,
                        Status: flightStatus,
                        CurrentValue: qualifiedFlightHours,
                        ThresholdValue: minFlight,
                        Unit: "giờ",
                        IsMandatory: req.IsMandatory,
                        Explanation: flightExpl,
                        Category: "FlightTraining"
                    ));
                    break;

                case "MinSimulatorHours":
                    var minSim = req.ThresholdValue ?? 0m;
                    ReadinessStatus simStatus;
                    string simExpl;
                    if (qualifiedSimHours >= minSim)
                    {
                        simStatus = ReadinessStatus.Met;
                        simExpl = $"Đã tích lũy {qualifiedSimHours:0.##} / {minSim:0.##} giờ buồng lái mô phỏng (FSTD) hợp lệ.";
                    }
                    else if (qualifiedSimHours == 0)
                    {
                        simStatus = (minSim == 0) ? ReadinessStatus.Met : ReadinessStatus.NoData;
                        simExpl = $"Chưa có giờ mô phỏng FSTD hợp lệ nào được ghi nhận (0.00 / {minSim:0.##} giờ).";
                    }
                    else
                    {
                        simStatus = ReadinessStatus.NotMet;
                        simExpl = $"Còn thiếu {(minSim - qualifiedSimHours):0.##} giờ mô phỏng FSTD ({qualifiedSimHours:0.##} / {minSim:0.##} giờ).";
                    }

                    conditions.Add(new ReadinessItemDto(
                        ConditionCode: "MIN_SIMULATOR_HOURS",
                        ConditionName: req.RequirementName,
                        Status: simStatus,
                        CurrentValue: qualifiedSimHours,
                        ThresholdValue: minSim,
                        Unit: "giờ",
                        IsMandatory: req.IsMandatory,
                        Explanation: simExpl,
                        Category: "SimulatorTraining"
                    ));
                    break;

                case "MinAttendance":
                case "AllAssessmentsPassed":
                case "AllChecklistsSignedOff":
                    // Already covered in standard checks above, unless specific threshold overrides
                    break;

                default:
                    // Advisory / Custom requirement
                    conditions.Add(new ReadinessItemDto(
                        ConditionCode: $"CUSTOM_REQ_{req.RequirementId}",
                        ConditionName: req.RequirementName,
                        Status: ReadinessStatus.Met,
                        CurrentValue: null,
                        ThresholdValue: req.ThresholdValue,
                        Unit: null,
                        IsMandatory: req.IsMandatory,
                        Explanation: req.Description ?? "Yêu cầu hoàn thành bổ sung theo giáo trình.",
                        Category: "Custom"
                    ));
                    break;
            }
        }

        // 9. Rà soát Hồ sơ Năng định & Cảnh báo (Pilot Credentials & Health Warnings - Non-blocking signals)
        if (studentProfile != null)
        {
            if (!studentProfile.IsCredentialsVerified)
            {
                warnings.Add(new ReadinessWarningDto(
                    WarningCode: "CREDENTIALS_UNVERIFIED",
                    Message: "Hồ sơ năng định và tài liệu văn bằng của học viên chưa được xác minh bởi Phòng Đào tạo.",
                    Severity: "ReviewRequired",
                    Category: "Credentials"
                ));
            }

            if (studentProfile.MedicalExpiryDate.HasValue)
            {
                if (studentProfile.MedicalExpiryDate.Value < DateTime.UtcNow)
                {
                    string msg = (isInstructor && !isOwn)
                        ? "Giấy chứng nhận sức khỏe của học viên đã hết hạn. Cần kiểm tra hiệu lực trước khi xếp lịch huấn luyện."
                        : $"Giấy chứng nhận sức khỏe (Hạng {studentProfile.MedicalClass ?? "N/A"}) đã hết hạn vào ngày {studentProfile.MedicalExpiryDate.Value:dd/MM/yyyy}.";

                    warnings.Add(new ReadinessWarningDto(
                        WarningCode: "MEDICAL_EXPIRED",
                        Message: msg,
                        Severity: "Warning",
                        Category: "Medical"
                    ));
                }
                else if (studentProfile.MedicalExpiryDate.Value <= DateTime.UtcNow.AddDays(30))
                {
                    string msg = (isInstructor && !isOwn)
                        ? "Giấy chứng nhận sức khỏe của học viên sắp hết hạn trong vòng 30 ngày."
                        : $"Giấy chứng nhận sức khỏe sẽ hết hạn vào ngày {studentProfile.MedicalExpiryDate.Value:dd/MM/yyyy} (còn dưới 30 ngày).";

                    warnings.Add(new ReadinessWarningDto(
                        WarningCode: "MEDICAL_EXPIRING_SOON",
                        Message: msg,
                        Severity: "Info",
                        Category: "Medical"
                    ));
                }
            }

            if (studentProfile.LicenseExpiryDate.HasValue && studentProfile.LicenseExpiryDate.Value < DateTime.UtcNow)
            {
                string msg = (isInstructor && !isOwn)
                    ? "Bằng lái phi công của học viên đã hết hạn. Cần rà soát trước khi thực hiện bay."
                    : $"Bằng lái phi công ({studentProfile.LicenseType ?? "Pilot License"}) đã hết hạn vào ngày {studentProfile.LicenseExpiryDate.Value:dd/MM/yyyy}.";

                warnings.Add(new ReadinessWarningDto(
                    WarningCode: "LICENSE_EXPIRED",
                    Message: msg,
                    Severity: "Warning",
                    Category: "License"
                ));
            }

            if (studentProfile.IcaoElpExpiryDate.HasValue && studentProfile.IcaoElpExpiryDate.Value < DateTime.UtcNow)
            {
                string msg = (isInstructor && !isOwn)
                    ? "Chứng chỉ tiếng Anh hàng không (ICAO ELP) của học viên đã hết hạn."
                    : $"Chứng chỉ ICAO ELP (Level {studentProfile.IcaoElpLevel?.ToString() ?? "N/A"}) đã hết hạn vào ngày {studentProfile.IcaoElpExpiryDate.Value:dd/MM/yyyy}.";

                warnings.Add(new ReadinessWarningDto(
                    WarningCode: "ICAO_ELP_EXPIRED",
                    Message: msg,
                    Severity: "Warning",
                    Category: "LanguageProficiency"
                ));
            }
        }

        // 10. Tính toán OverallStatus
        bool hasMandatoryNotMet = conditions.Any(c => c.IsMandatory && (c.Status == ReadinessStatus.NotMet || c.Status == ReadinessStatus.NoData));
        bool hasReviewRequired = conditions.Any(c => c.Status == ReadinessStatus.ReviewRequired) ||
                                 warnings.Any(w => w.Severity == "ReviewRequired");

        ReadinessStatus overallStatus;
        if (hasMandatoryNotMet)
        {
            overallStatus = conditions.All(c => c.Status == ReadinessStatus.NoData) ? ReadinessStatus.NoData : ReadinessStatus.NotMet;
        }
        else if (hasReviewRequired)
        {
            overallStatus = ReadinessStatus.ReviewRequired;
        }
        else
        {
            overallStatus = ReadinessStatus.Met;
        }

        return new EtrReadinessResponse(
            ETRCourseRecordId: etr.ETRCourseRecordId,
            EnrollmentId: etr.EnrollmentId,
            AccountId: enrollment.AccountId,
            StudentName: studentName,
            CourseId: trainingClass.CourseId,
            CourseName: course.CourseName,
            CourseVersionNo: etr.CourseVersionNo,
            ClassId: trainingClass.ClassId,
            ClassName: trainingClass.ClassName,
            OverallStatus: overallStatus,
            TotalFlightHours: qualifiedFlightHours,
            TotalSimulatorHours: qualifiedSimHours,
            EvaluatedAt: DateTime.UtcNow,
            Conditions: conditions,
            Warnings: warnings
        );
    }

    public async Task<EtrRecordResponse> VerifyEtrAsync(int etrCourseRecordId, int accountId, CancellationToken cancellationToken = default)
    {
        var etr = await _unitOfWork.ETRCourseRecordRepository.GetWithSubjectResultsAsync(etrCourseRecordId, cancellationToken)
            ?? throw new KeyNotFoundException($"ETRCourseRecord not found.");

        if (etr.Status != EtrStatus.Submitted)
            throw new BusinessRuleViolationException("Cannot verify ETR that is not in Submitted status.");

        var enrollment = await _unitOfWork.CourseEnrollmentRepository.GetByIdAsync(etr.EnrollmentId, cancellationToken)
            ?? throw new BusinessRuleViolationException("Enrollment not found.");

        var trainingClass = await _unitOfWork.ClassRepository.GetByIdAsync(enrollment.ClassId, cancellationToken)
            ?? throw new BusinessRuleViolationException("Class not found.");

        var courseSubjects = (await _unitOfWork.CourseSubjectRepository.GetAllAsync(cancellationToken) ?? Enumerable.Empty<CourseSubject>())
            .Where(cs => cs.CourseId == trainingClass.CourseId && cs.IsMandatory).ToList();

        var subjectResults = etr.SubjectResults ?? Enumerable.Empty<SubjectResult>();
        bool hasSnapshots = subjectResults.Any(sr => sr.IsMandatorySnapshot.HasValue);

        // 1. Check all mandatory subjects are Passed or Exempted
        if (hasSnapshots)
        {
            var mandatorySubjectResults = subjectResults.Where(sr => sr.IsMandatorySnapshot == true).ToList();
            foreach (var sr in mandatorySubjectResults)
            {
                if (sr.Status != SubjectResultStatus.Passed && sr.Status != SubjectResultStatus.Exempted)
                {
                    var subName = sr.SubjectNameSnapshot ?? $"ID: {sr.SubjectId}";
                    throw new BusinessRuleViolationException($"Cannot verify ETR. Mandatory subject ({subName}) is not Passed or Exempted.");
                }
            }
        }
        else
        {
            foreach (var cs in courseSubjects)
            {
                var sr = subjectResults.FirstOrDefault(s => s.SubjectId == cs.SubjectId);
                if (sr == null || (sr.Status != SubjectResultStatus.Passed && sr.Status != SubjectResultStatus.Exempted))
                {
                    throw new BusinessRuleViolationException($"Cannot verify ETR. Mandatory subject (ID: {cs.SubjectId}) is not Passed or Exempted.");
                }
            }
        }

        // 2. Check all evidence is Verified
        var allEvidences = (await _unitOfWork.EvidenceFileRepository.GetAllAsync(cancellationToken) ?? Enumerable.Empty<EvidenceFile>()).ToList();
        var etrSubjectIds = subjectResults.Select(sr => sr.SubjectResultId).ToList();
        var pendingEvidences = allEvidences
            .Where(e => etrSubjectIds.Contains(e.SubjectResultId) && e.VerificationStatus != "Verified" && !e.IsDeleted)
            .ToList();

        if (pendingEvidences.Any())
        {
            throw new BusinessRuleViolationException($"Cannot verify ETR. {pendingEvidences.Count} evidence file(s) are not yet Verified.");
        }

        // 3. Check all subject signoffs exist
        var allSignoffs = (await _unitOfWork.SubjectSignoffRepository.GetAllAsync(cancellationToken) ?? Enumerable.Empty<SubjectSignoff>()).ToList();
        foreach (var sr in subjectResults)
        {
            var hasSignoff = allSignoffs.Any(s => s.SubjectResultId == sr.SubjectResultId && !s.IsDeleted);
            if (!hasSignoff)
            {
                throw new BusinessRuleViolationException($"Cannot verify ETR. Subject (ID: {sr.SubjectId}) has not been signed off by instructor.");
            }
        }

        // === AUDIT LOG ===
        var auditLog = new AuditLog
        {
            ETRRecordId = etrCourseRecordId,
            AccountId = accountId,
            ActionType = AuditActionType.VERIFY.ToString(),
            EntityName = nameof(ETRCourseRecord),
            RecordId = etrCourseRecordId,
            OldValue = etr.Status.ToString(),
            NewValue = "Verified",
            Description = $"ETR #{etrCourseRecordId} verified by QA"
        };
        await _unitOfWork.AuditLogRepository.AddAsync(auditLog, cancellationToken);

        etr.Status = EtrStatus.Verified;
        etr.VerifiedAt = DateTime.UtcNow;
        etr.UpdatedAt = DateTime.UtcNow;
        etr.UpdatedByAccountId = accountId;

        _unitOfWork.ETRCourseRecordRepository.Update(etr);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new EtrRecordResponse(etr.ETRCourseRecordId, etr.EnrollmentId, etr.Status, etr.IsLocked, etr.SubmittedAt, etr.VerifiedAt, etr.CompletedAt, etr.IssuedDate, etr.ExpiryDate, etr.PreviousRecordId);
    }

    public async Task<EtrRecordResponse> ReturnEtrAsync(int etrCourseRecordId, int accountId, string? comment, CancellationToken cancellationToken = default)
    {
        var etr = await _unitOfWork.ETRCourseRecordRepository.GetByIdAsync(etrCourseRecordId, cancellationToken)
            ?? throw new KeyNotFoundException($"ETRCourseRecord not found.");

        if (etr.Status != EtrStatus.Submitted && etr.Status != EtrStatus.Verified)
            throw new BusinessRuleViolationException("Cannot return ETR that is not in Submitted or Verified status.");

        if (string.IsNullOrWhiteSpace(comment))
            throw new ValidationException("A comment is required when returning an ETR for correction.");

        // === AUDIT LOG ===
        var auditLog = new AuditLog
        {
            ETRRecordId = etrCourseRecordId,
            AccountId = accountId,
            ActionType = AuditActionType.RETURN.ToString(),
            EntityName = nameof(ETRCourseRecord),
            RecordId = etrCourseRecordId,
            OldValue = etr.Status.ToString(),
            NewValue = "ReturnedForCorrection",
            Description = $"ETR #{etrCourseRecordId} returned for correction by QA. Comment: {comment ?? "N/A"}"
        };
        await _unitOfWork.AuditLogRepository.AddAsync(auditLog, cancellationToken);

        etr.Status = EtrStatus.ReturnedForCorrection;
        etr.UpdatedAt = DateTime.UtcNow;
        etr.UpdatedByAccountId = accountId;

        _unitOfWork.ETRCourseRecordRepository.Update(etr);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new EtrRecordResponse(etr.ETRCourseRecordId, etr.EnrollmentId, etr.Status, etr.IsLocked, etr.SubmittedAt, etr.VerifiedAt, etr.CompletedAt, etr.IssuedDate, etr.ExpiryDate, etr.PreviousRecordId);
    }

    public async Task<EtrRecordResponse> CompleteEtrAsync(int etrCourseRecordId, int accountId, CancellationToken cancellationToken = default)
    {
        var etr = await _unitOfWork.ETRCourseRecordRepository.GetWithSubjectResultsAsync(etrCourseRecordId, cancellationToken)
            ?? throw new KeyNotFoundException($"ETRCourseRecord not found.");

        if (etr.Status != EtrStatus.Verified)
            throw new BusinessRuleViolationException("Cannot complete ETR that is not in Verified status.");

        var enrollment = await _unitOfWork.CourseEnrollmentRepository.GetByIdAsync(etr.EnrollmentId, cancellationToken);
        if (enrollment == null) throw new BusinessRuleViolationException("Enrollment not found.");

        var trainingClass = await _unitOfWork.ClassRepository.GetByIdAsync(enrollment.ClassId, cancellationToken);
        if (trainingClass == null) throw new BusinessRuleViolationException("Class not found.");

        var courseSubjects = (await _unitOfWork.CourseSubjectRepository.GetAllAsync(cancellationToken) ?? Enumerable.Empty<CourseSubject>())
            .Where(cs => cs.CourseId == trainingClass.CourseId && cs.IsMandatory).ToList();

        var subjectResults = etr.SubjectResults ?? Enumerable.Empty<SubjectResult>();
        bool hasSnapshots = subjectResults.Any(sr => sr.IsMandatorySnapshot.HasValue);

        // === PRE-VALIDATION ===

        // 1. Check all mandatory subjects are Passed or Exempted
        if (hasSnapshots)
        {
            var mandatorySubjectResults = subjectResults.Where(sr => sr.IsMandatorySnapshot == true).ToList();
            foreach (var sr in mandatorySubjectResults)
            {
                if (sr.Status != SubjectResultStatus.Passed && sr.Status != SubjectResultStatus.Exempted)
                {
                    var subName = sr.SubjectNameSnapshot ?? $"ID: {sr.SubjectId}";
                    throw new BusinessRuleViolationException($"Cannot complete ETR. Mandatory subject ({subName}) is not Passed or Exempted.");
                }
            }
        }
        else
        {
            foreach (var cs in courseSubjects)
            {
                var sr = subjectResults.FirstOrDefault(s => s.SubjectId == cs.SubjectId);
                if (sr == null || (sr.Status != SubjectResultStatus.Passed && sr.Status != SubjectResultStatus.Exempted))
                {
                    throw new BusinessRuleViolationException($"Cannot complete ETR. Mandatory subject (ID: {cs.SubjectId}) is not Passed or Exempted.");
                }
            }
        }

        // 2. Check all evidence is Verified
        var allEvidences = await _unitOfWork.EvidenceFileRepository.GetAllAsync(cancellationToken);
        var etrSubjectIds = etr.SubjectResults?.Select(sr => sr.SubjectResultId).ToList() ?? new List<int>();
        var pendingEvidences = allEvidences
            .Where(e => etrSubjectIds.Contains(e.SubjectResultId) && e.VerificationStatus != "Verified" && !e.IsDeleted)
            .ToList();

        if (pendingEvidences.Any())
        {
            throw new BusinessRuleViolationException($"Cannot complete ETR. {pendingEvidences.Count} evidence file(s) are not yet Verified.");
        }

        // === AUDIT LOG ===
        var auditLog = new AuditLog
        {
            ETRRecordId = etrCourseRecordId,
            AccountId = accountId,
            ActionType = AuditActionType.APPROVE.ToString(),
            EntityName = nameof(ETRCourseRecord),
            RecordId = etrCourseRecordId,
            OldValue = etr.Status.ToString(),
            NewValue = "Completed",
            Description = $"ETR #{etrCourseRecordId} completed and locked by Training Manager"
        };
        await _unitOfWork.AuditLogRepository.AddAsync(auditLog, cancellationToken);

        var course = await _unitOfWork.CourseRepository.GetByIdAsync(trainingClass.CourseId, cancellationToken);

        etr.Status = EtrStatus.Completed;
        etr.CompletedAt = DateTime.UtcNow;
        etr.IsLocked = true;
        etr.IssuedDate = DateTime.UtcNow;
        if (course != null && course.ValidityMonths.HasValue)
        {
            etr.ExpiryDate = DateTime.UtcNow.AddMonths(course.ValidityMonths.Value);
        }
        etr.UpdatedAt = DateTime.UtcNow;
        etr.UpdatedByAccountId = accountId;

        // Update enrollment completion date
        enrollment.ActualCompletionDate = DateTime.UtcNow;
        _unitOfWork.CourseEnrollmentRepository.Update(enrollment);

        // Keep ApprovalHistory in sync even when completion happens via this direct route rather than
        // ApprovalService.ProcessApprovalActionAsync(action:"Approve") — ExportService.BuildAuditHistoryPdf
        // (the CAA audit export) reads exclusively from ApprovalHistory, so without this the exported
        // Audit_History.pdf would silently be missing the final approval entry.
        var approvalRequest = (await _unitOfWork.ApprovalRequestRepository.GetAllAsync(cancellationToken))
            .FirstOrDefault(a => a.ETRCourseRecordId == etrCourseRecordId);
        if (approvalRequest != null && approvalRequest.CurrentStatus != "Approved")
        {
            var previousApprovalStatus = approvalRequest.CurrentStatus;
            approvalRequest.CurrentStatus = "Approved";
            approvalRequest.CompletedAt = DateTime.UtcNow;
            approvalRequest.UpdatedAt = DateTime.UtcNow;
            approvalRequest.UpdatedByAccountId = accountId;
            _unitOfWork.ApprovalRequestRepository.Update(approvalRequest);

            await _unitOfWork.ApprovalHistoryRepository.AddAsync(new ApprovalHistory
            {
                ApprovalRequestId = approvalRequest.ApprovalRequestId,
                ActionByAccountId = accountId,
                ActionType = ApprovalHistoryActionType.Approve.ToString(),
                PreviousStatus = previousApprovalStatus,
                NewStatus = "Approved",
                Comments = "ETR completed directly via /api/etr/{id}/complete",
                ActionAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                CreatedByAccountId = accountId
            }, cancellationToken);
        }

        _unitOfWork.ETRCourseRecordRepository.Update(etr);
        await _unitOfWork.SaveAsync(cancellationToken);

        // Grounded is only cleared once a learner's ETR for the affected course is actually
        // Completed — not merely upon re-enrolling (see EnrollmentService.CreateEnrollmentAsync).
        // This check runs AFTER the save above so CertificateValidityCalculator reads this ETR's
        // just-committed Completed status/ExpiryDate, not its pre-completion state.
        var learnerProfile = await _unitOfWork.UserProfileRepository.GetByIdAsync(enrollment.AccountId, cancellationToken);
        if (learnerProfile != null && learnerProfile.Status == LearnerStatus.Grounded)
        {
            var stillHasExpired = await CertificateValidityCalculator.HasAnyExpiredCompletedEtrAsync(_unitOfWork, enrollment.AccountId, cancellationToken);
            if (!stillHasExpired)
            {
                await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
                {
                    AccountId = accountId,
                    ActionType = AuditActionType.UPDATE.ToString(),
                    EntityName = nameof(UserProfile),
                    RecordId = learnerProfile.AccountId,
                    OldValue = LearnerStatus.Grounded.ToString(),
                    NewValue = LearnerStatus.Active.ToString(),
                    Description = $"UserProfile for Account #{learnerProfile.AccountId} auto-cleared from Grounded: ETR #{etrCourseRecordId} just Completed and no other course has an expired completed ETR."
                }, cancellationToken);

                learnerProfile.Status = LearnerStatus.Active;
                learnerProfile.UpdatedAt = DateTime.UtcNow;
                learnerProfile.UpdatedByAccountId = accountId;
                _unitOfWork.UserProfileRepository.Update(learnerProfile);
                await _unitOfWork.SaveAsync(cancellationToken);
            }
        }

        // Tự động hoàn thành lớp (Completed) khi toàn bộ học viên trong lớp đã hoàn tất hồ sơ ETR và được Training Manager phê duyệt
        if (trainingClass != null && trainingClass.Status == ClassStatus.InProgress && !trainingClass.IsDeleted)
        {
            var classEnrollments = (await _unitOfWork.CourseEnrollmentRepository.GetAllAsync(cancellationToken))
                ?.Where(e => e.ClassId == trainingClass.ClassId && !e.IsDeleted).ToList() ?? new List<CourseEnrollment>();

            if (classEnrollments.Count > 0)
            {
                var classEnrollmentIds = classEnrollments.Select(e => e.EnrollmentId).ToList();
                var classEtrs = (await _unitOfWork.ETRCourseRecordRepository.GetAllAsync(cancellationToken))
                    ?.Where(e => classEnrollmentIds.Contains(e.EnrollmentId) && !e.IsDeleted).ToList() ?? new List<ETRCourseRecord>();

                bool allEtrsCompleted = classEnrollments.All(en => classEtrs.Any(e => e.EnrollmentId == en.EnrollmentId && 
                    (e.ETRCourseRecordId == etr.ETRCourseRecordId || e.Status == EtrStatus.Completed || e.Status == EtrStatus.Approved)));

                if (allEtrsCompleted)
                {
                    trainingClass.Status = ClassStatus.Completed;
                    trainingClass.UpdatedAt = DateTime.UtcNow;
                    trainingClass.UpdatedByAccountId = accountId;
                    _unitOfWork.ClassRepository.Update(trainingClass);

                    await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
                    {
                        AccountId = accountId,
                        ActionType = AuditActionType.UPDATE.ToString(),
                        EntityName = nameof(Class),
                        RecordId = trainingClass.ClassId,
                        OldValue = $"Status: {ClassStatus.InProgress}",
                        NewValue = $"Status: {ClassStatus.Completed}",
                        Description = $"Lớp học #{trainingClass.ClassId} ('{trainingClass.ClassName}') đã kết thúc đào tạo: toàn bộ học viên trong lớp đã hoàn thành và được phê duyệt hồ sơ ETR."
                    }, cancellationToken);

                    await _unitOfWork.SaveAsync(cancellationToken);
                }
            }
        }

        return new EtrRecordResponse(etr.ETRCourseRecordId, etr.EnrollmentId, etr.Status, etr.IsLocked, etr.SubmittedAt, etr.VerifiedAt, etr.CompletedAt, etr.IssuedDate, etr.ExpiryDate, etr.PreviousRecordId);
    }

    public async Task<EtrRecordResponse> LockEtrAsync(int etrCourseRecordId, int accountId, string? reason, CancellationToken cancellationToken = default)
    {
        var etr = await _unitOfWork.ETRCourseRecordRepository.GetByIdAsync(etrCourseRecordId, cancellationToken)
            ?? throw new KeyNotFoundException($"ETRCourseRecord not found.");

        await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            ETRRecordId = etrCourseRecordId,
            AccountId = accountId,
            ActionType = AuditActionType.LOCK.ToString(),
            EntityName = nameof(ETRCourseRecord),
            RecordId = etrCourseRecordId,
            OldValue = etr.IsLocked.ToString(),
            NewValue = "True",
            Description = $"ETR #{etrCourseRecordId} manually locked. Reason: {reason ?? "N/A"}"
        }, cancellationToken);

        // No explicit Update(etr) call here: GetByIdAsync above already tracks this entity in the
        // same DbContext, so EF Core's own change detection picks up the mutation automatically.
        // Calling Update() (DbSet.Update) would force EVERY property to IsModified=true, not just
        // IsLocked — which breaks ImmutabilityValidator's fine-grained IsBeingUnlocked check below.
        etr.IsLocked = true;
        etr.UpdatedAt = DateTime.UtcNow;
        etr.UpdatedByAccountId = accountId;

        await _unitOfWork.SaveAsync(cancellationToken);

        return new EtrRecordResponse(etr.ETRCourseRecordId, etr.EnrollmentId, etr.Status, etr.IsLocked, etr.SubmittedAt, etr.VerifiedAt, etr.CompletedAt, etr.IssuedDate, etr.ExpiryDate, etr.PreviousRecordId);
    }

    public async Task<EtrRecordResponse> UnlockEtrAsync(int etrCourseRecordId, int accountId, string? reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ValidationException("A reason is required to re-open a locked ETR.");

        var etr = await _unitOfWork.ETRCourseRecordRepository.GetByIdAsync(etrCourseRecordId, cancellationToken)
            ?? throw new KeyNotFoundException($"ETRCourseRecord not found.");

        if (!etr.IsLocked)
            throw new BusinessRuleViolationException("ETR is not locked.");

        // Re-opening a Completed/Locked ETR is the FRD's explicitly-named exception to absolute
        // immutability — it must always leave a full audit trail, since it's the one path that lets
        // previously-frozen data become editable again.
        var wasCompleted = etr.Status == EtrStatus.Completed;

        await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            ETRRecordId = etrCourseRecordId,
            AccountId = accountId,
            ActionType = AuditActionType.UNLOCK.ToString(),
            EntityName = nameof(ETRCourseRecord),
            RecordId = etrCourseRecordId,
            OldValue = wasCompleted ? "True / Completed" : "True",
            NewValue = wasCompleted ? "False / Verified" : "False",
            Description = wasCompleted
                ? $"ETR #{etrCourseRecordId} re-opened (unlocked, status reverted to Verified so child evidence/attendance/assessment data can be amended). Reason: {reason}"
                : $"ETR #{etrCourseRecordId} re-opened (unlocked). Reason: {reason}"
        }, cancellationToken);

        // No explicit Update(etr) call — see LockEtrAsync above for why: it would mark every
        // property Modified and defeat ImmutabilityValidator's IsBeingUnlocked check. That check now
        // also permits ONE specific status change alongside IsLocked: Completed -> Verified (see
        // AppDbContext.Compliance.cs) — this is what actually unblocks child-entity edits after
        // Reopen (H13): once Status is no longer "Completed", ValidateEtrChildEntity's own
        // IsEtrImmutable check naturally allows Evidence/Attendance/AssessmentResult edits again,
        // with no separate exception needed for child entities. Re-completing goes through the
        // normal CompleteEtrAsync flow again (it already requires Status == "Verified").
        etr.IsLocked = false;
        if (wasCompleted)
        {
            etr.Status = EtrStatus.Verified;
        }
        etr.UpdatedAt = DateTime.UtcNow;
        etr.UpdatedByAccountId = accountId;

        await _unitOfWork.SaveAsync(cancellationToken);

        return new EtrRecordResponse(etr.ETRCourseRecordId, etr.EnrollmentId, etr.Status, etr.IsLocked, etr.SubmittedAt, etr.VerifiedAt, etr.CompletedAt, etr.IssuedDate, etr.ExpiryDate, etr.PreviousRecordId);
    }

    public async Task<IEnumerable<EtrRecordResponse>> GetStudentEtrHistoryAsync(int studentId, CancellationToken cancellationToken = default)
    {
        var enrollments = await _unitOfWork.CourseEnrollmentRepository.GetAllAsync(cancellationToken);
        var studentEnrollmentIds = enrollments.Where(e => e.AccountId == studentId).Select(e => e.EnrollmentId).ToList();

        if (_currentUserService.RoleName == "Instructor" && _currentUserService.AccountId.HasValue)
        {
            var myEnrollmentIds = await GetInstructorEnrollmentIdsAsync(_currentUserService.AccountId.Value, cancellationToken);
            studentEnrollmentIds = studentEnrollmentIds.Intersect(myEnrollmentIds).ToList();
        }

        var etrs = await _unitOfWork.ETRCourseRecordRepository.GetAllAsync(cancellationToken);
        return etrs
            .Where(e => studentEnrollmentIds.Contains(e.EnrollmentId))
            .OrderByDescending(e => e.IssuedDate ?? e.CreatedAt)
            .Select(e => new EtrRecordResponse(
                e.ETRCourseRecordId,
                e.EnrollmentId,
                e.Status,
                e.IsLocked,
                e.SubmittedAt,
                e.VerifiedAt,
                e.CompletedAt,
                e.IssuedDate,
                e.ExpiryDate,
                e.PreviousRecordId));
    }

    public async Task<IEnumerable<StudentEtrStatusResponse>> GetStudentEtrCurrentStatusAsync(int studentId, CancellationToken cancellationToken = default)
    {
        var enrollments = await _unitOfWork.CourseEnrollmentRepository.GetAllAsync(cancellationToken);
        var studentEnrollments = enrollments.Where(e => e.AccountId == studentId).ToList();
        
        if (_currentUserService.RoleName == "Instructor" && _currentUserService.AccountId.HasValue)
        {
            var myClassIds = await GetInstructorClassIdsAsync(_currentUserService.AccountId.Value, cancellationToken);
            studentEnrollments = studentEnrollments.Where(e => myClassIds.Contains(e.ClassId)).ToList();
        }
        
        var classes = await _unitOfWork.ClassRepository.GetAllAsync(cancellationToken);
        var courses = await _unitOfWork.CourseRepository.GetAllAsync(cancellationToken);
        
        var etrs = await _unitOfWork.ETRCourseRecordRepository.GetAllAsync(cancellationToken);
        
        // Chỉ những hồ sơ ETR đã Completed hoặc Approved, đã có IssuedDate mới được xem là chứng chỉ đã cấp
        var completedEtrs = etrs.Where(etr =>
            !etr.IsDeleted &&
            (etr.Status == EtrStatus.Completed || etr.Status == EtrStatus.Approved) &&
            etr.IssuedDate.HasValue).ToList();

        var result = new List<StudentEtrStatusResponse>();
        var groupedByCourse = studentEnrollments
            .Join(classes, e => e.ClassId, c => c.ClassId, (e, c) => new { e.EnrollmentId, c.CourseId })
            .Join(completedEtrs, ec => ec.EnrollmentId, etr => etr.EnrollmentId, (ec, etr) => new { ec.CourseId, Etr = etr })
            .GroupBy(x => x.CourseId);

        foreach (var group in groupedByCourse)
        {
            var latestEtr = group.OrderByDescending(x => x.Etr.IssuedDate ?? x.Etr.CreatedAt).First().Etr;
            var course = courses.FirstOrDefault(c => c.CourseId == group.Key);
            
            string validityStatus = "Valid";
            if (latestEtr.ExpiryDate.HasValue)
            {
                if (latestEtr.ExpiryDate.Value < DateTime.UtcNow)
                {
                    validityStatus = "Expired";
                }
                else if ((latestEtr.ExpiryDate.Value - DateTime.UtcNow).TotalDays <= 30)
                {
                    validityStatus = "ExpiringSoon";
                }
            }
            
            result.Add(new StudentEtrStatusResponse(
                group.Key,
                course?.CourseName ?? "Unknown",
                latestEtr.ETRCourseRecordId,
                latestEtr.IssuedDate,
                latestEtr.ExpiryDate,
                validityStatus
            ));
        }

        return result;
    }

    public async Task<IEnumerable<ExpiringStudentResponse>> GetExpiringStudentsAsync(int courseId, int daysThreshold, CancellationToken cancellationToken = default)
    {
        var enrollments = await _unitOfWork.CourseEnrollmentRepository.GetAllAsync(cancellationToken);
        var classes = (await _unitOfWork.ClassRepository.GetAllAsync(cancellationToken)).Where(c => c.CourseId == courseId).ToList();
        
        if (_currentUserService.RoleName == "Instructor" && _currentUserService.AccountId.HasValue)
        {
            var myClassIds = await GetInstructorClassIdsAsync(_currentUserService.AccountId.Value, cancellationToken);
            classes = classes.Where(c => myClassIds.Contains(c.ClassId)).ToList();
        }
        
        var classIds = classes.Select(c => c.ClassId).ToList();
        var courseEnrollments = enrollments.Where(e => classIds.Contains(e.ClassId)).ToList();
        
        var etrs = await _unitOfWork.ETRCourseRecordRepository.GetAllAsync(cancellationToken);
        var completedEtrs = etrs.Where(etr =>
            !etr.IsDeleted &&
            (etr.Status == EtrStatus.Completed || etr.Status == EtrStatus.Approved) &&
            etr.IssuedDate.HasValue).ToList();

        var accounts = await _unitOfWork.AccountRepository.GetAllAsync(cancellationToken);
        var userProfiles = await _unitOfWork.UserProfileRepository.GetAllAsync(cancellationToken);
        
        var result = new List<ExpiringStudentResponse>();
        
        var groupedByStudent = courseEnrollments
            .Join(completedEtrs, e => e.EnrollmentId, etr => etr.EnrollmentId, (e, etr) => new { e.AccountId, Etr = etr })
            .GroupBy(x => x.AccountId);

        foreach (var group in groupedByStudent)
        {
            var latestEtr = group.OrderByDescending(x => x.Etr.IssuedDate ?? x.Etr.CreatedAt).First().Etr;
            if (latestEtr.ExpiryDate.HasValue)
            {
                var daysUntilExpiry = (latestEtr.ExpiryDate.Value - DateTime.UtcNow).TotalDays;
                if (daysUntilExpiry <= daysThreshold)
                {
                    var account = accounts.FirstOrDefault(a => a.AccountId == group.Key);
                    var profile = userProfiles.FirstOrDefault(p => p.AccountId == group.Key);
                    
                    string validityStatus = daysUntilExpiry < 0 ? "Expired" : "ExpiringSoon";
                    
                    result.Add(new ExpiringStudentResponse(
                        group.Key,
                        account?.Username ?? "Unknown",
                        profile?.FullName ?? "Unknown",
                        courseId,
                        latestEtr.ETRCourseRecordId,
                        latestEtr.ExpiryDate,
                        validityStatus
                    ));
                }
            }
        }

        return result;
    }

    public async Task<IEnumerable<ExpiringStudentResponse>> GetDueForTrainingAsync(int? courseId, int daysThreshold, CancellationToken cancellationToken = default)
    {
        // "Due for training" is GetExpiringStudentsAsync run across every course instead of one —
        // reuses its existing per-course role filtering (Instructor sees only their own classes) and
        // Expired/ExpiringSoon logic as-is rather than re-implementing it.
        var courseIds = courseId.HasValue
            ? new List<int> { courseId.Value }
            : (await _unitOfWork.CourseRepository.GetAllAsync(cancellationToken)).Select(c => c.CourseId).ToList();

        var result = new List<ExpiringStudentResponse>();
        foreach (var id in courseIds)
        {
            result.AddRange(await GetExpiringStudentsAsync(id, daysThreshold, cancellationToken));
        }

        return result;
    }

    public async Task<GroundedStatusRefreshResponse> RefreshGroundedStatusAsync(int actorAccountId, CancellationToken cancellationToken = default)
    {
        // Only Active/Grounded profiles are re-evaluated — Withdrawn/Graduated learners are no
        // longer in the training pipeline, so their status is left alone regardless of expiry.
        var profiles = (await _unitOfWork.UserProfileRepository.GetAllAsync(cancellationToken))
            .Where(p => p.Status == LearnerStatus.Active || p.Status == LearnerStatus.Grounded)
            .ToList();

        int scanned = 0, groundedCount = 0, clearedCount = 0;

        foreach (var profile in profiles)
        {
            scanned++;
            var hasExpired = await CertificateValidityCalculator.HasAnyExpiredCompletedEtrAsync(_unitOfWork, profile.AccountId, cancellationToken);

            if (hasExpired && profile.Status != LearnerStatus.Grounded)
            {
                await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
                {
                    AccountId = actorAccountId,
                    ActionType = AuditActionType.UPDATE.ToString(),
                    EntityName = nameof(UserProfile),
                    RecordId = profile.AccountId,
                    OldValue = profile.Status.ToString(),
                    NewValue = LearnerStatus.Grounded.ToString(),
                    Description = $"UserProfile for Account #{profile.AccountId} auto-grounded: a completed ETR has an expired certificate and no newer enrollment/ETR covers that course."
                }, cancellationToken);

                profile.Status = LearnerStatus.Grounded;
                profile.UpdatedAt = DateTime.UtcNow;
                profile.UpdatedByAccountId = actorAccountId;
                _unitOfWork.UserProfileRepository.Update(profile);
                groundedCount++;
            }
            else if (!hasExpired && profile.Status == LearnerStatus.Grounded)
            {
                await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
                {
                    AccountId = actorAccountId,
                    ActionType = AuditActionType.UPDATE.ToString(),
                    EntityName = nameof(UserProfile),
                    RecordId = profile.AccountId,
                    OldValue = profile.Status.ToString(),
                    NewValue = LearnerStatus.Active.ToString(),
                    Description = $"UserProfile for Account #{profile.AccountId} auto-cleared from Grounded: no course has an expired completed ETR anymore."
                }, cancellationToken);

                profile.Status = LearnerStatus.Active;
                profile.UpdatedAt = DateTime.UtcNow;
                profile.UpdatedByAccountId = actorAccountId;
                _unitOfWork.UserProfileRepository.Update(profile);
                clearedCount++;
            }
        }

        if (groundedCount > 0 || clearedCount > 0)
        {
            await _unitOfWork.SaveAsync(cancellationToken);
        }

        return new GroundedStatusRefreshResponse(scanned, groundedCount, clearedCount);
    }
}