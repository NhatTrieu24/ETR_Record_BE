# UML Design Specifications: ETR Management System

## PART 1: CLASS DIAGRAM SPECIFICATIONS

### Core Domain Entities

#### 1. CourseEnrollment
*   **Key Attributes**: `EnrollmentId` (PK), `LearnerId`, `ClassId`, `Status`, `EnrolledAt`, `StartDate`, `ExpectedCompletionDate`, `ActualCompletionDate`
*   **Navigation Properties**:
    *   1:1 with `ETRCourseRecord`
    *   1:N with `SubjectResult`
    *   1:N with `AttendanceRecord`

#### 2. ETRCourseRecord
*   **Key Attributes**: `ETRCourseRecordId` (PK), `EnrollmentId` (FK), `Status`, `SubmittedAt`, `VerifiedAt`, `CompletedAt`, `IsLocked`, `CreatedBySystem`
*   **Navigation Properties**:
    *   1:1 with `CourseEnrollment`
    *   1:N with `ApprovalRequest`

#### 3. SubjectResult
*   **Key Attributes**: `SubjectResultId` (PK), `EnrollmentId` (FK), `CourseId`, `SubjectId`, `AttendanceRate`, `Score`, `Status`, `EvaluatedBy`, `EvaluatedAt`
*   **Navigation Properties**:
    *   N:1 with `CourseEnrollment`
    *   N:1 with `CourseSubject`
    *   1:N with `AssessmentResult`
    *   1:N with `PracticalChecklistResult`
    *   1:N with `SubjectSignoff`
    *   1:N with `RetakeHistory`
    *   1:N with `EvidenceFile`

#### 4. Session
*   **Key Attributes**: `SessionId` (PK), `ClassId` (FK), `SubjectId` (FK), `SessionTitle`, `SessionDate`, `Location`, `IsConfirmed`, `ConfirmedBy`, `ConfirmedAt`
*   **Navigation Properties**:
    *   N:1 with `Class`
    *   N:1 with `Subject`
    *   1:N with `AttendanceRecord`

#### 5. SubjectSignoff
*   **Key Attributes**: `SubjectSignoffId` (PK), `SubjectResultId` (FK), `SignoffBy`, `Role`, `SignoffAt`, `Comment`
*   **Navigation Properties**:
    *   N:1 with `SubjectResult`

#### 6. EvidenceFile
*   **Key Attributes**: `EvidenceFileId` (PK), `EvidenceTypeId` (FK), `UploadedBy`, `LearnerId`, `SubjectResultId` (FK), `AttendanceRecordId`, `AssessmentResultId`, `FileName`, `FilePath`, `FileExtension`, `MimeType`, `FileSize`, `VerificationStatus`, `VerifiedBy`, `VerifiedAt`, `VerificationComment`, `UploadedAt`
*   **Navigation Properties**:
    *   N:1 with `SubjectResult`
    *   N:1 with `Learner`

#### 7. AssessmentResult
*   **Key Attributes**: `AssessmentResultId` (PK), `AssessmentId` (FK), `LearnerId`, `SubjectResultId` (FK), `Score`, `ResultStatus`, `RecordedBy`, `RecordedAt`, `PublishedAt`, `IsPublished`, `TakenAt`, `Remark`
*   **Navigation Properties**:
    *   N:1 with `Assessment`
    *   N:1 with `Learner`
    *   N:1 with `SubjectResult`

### Core Application Services

#### IEnrollmentService & EnrollmentService
*   `Task<CreateEnrollmentResponse> CreateEnrollmentAsync(int learnerId, int classId, int createdByUserId, CancellationToken cancellationToken = default);`

#### IEtrService & EtrService
*   `Task<EtrRecordResponse> SubmitEtrAsync(int etrCourseRecordId, int userId, CancellationToken cancellationToken = default);`
*   `Task<EtrRecordResponse> VerifyEtrAsync(int etrCourseRecordId, int userId, CancellationToken cancellationToken = default);`
*   `Task<EtrRecordResponse> CompleteEtrAsync(int etrCourseRecordId, int userId, CancellationToken cancellationToken = default);`
*   `Task<EtrRecordResponse> LockEtrAsync(int etrCourseRecordId, int userId, CancellationToken cancellationToken = default);`
*   `Task<EtrRecordResponse> UnlockEtrAsync(int etrCourseRecordId, int userId, CancellationToken cancellationToken = default);`

#### IAttendanceService & AttendanceService
*   `Task<AttendanceRecordResponse> RecordAttendanceAsync(CreateAttendanceRecordRequest request, CancellationToken cancellationToken = default);`
*   `Task<AttendanceSessionResponse> ConfirmSessionAsync(int sessionId, int confirmedByUserId, CancellationToken cancellationToken = default);`

#### IAssessmentService & AssessmentService
*   `Task<AssessmentResultResponse> RecordAssessmentScoreAsync(CreateAssessmentResultRequest request, CancellationToken cancellationToken = default);`
*   `Task<SubjectSignoffResponse> SignoffSubjectResultAsync(CreateSubjectSignoffRequest request, CancellationToken cancellationToken = default);`

#### IApprovalService & ApprovalService
*   `Task<ApprovalRequestResponse> ProcessApprovalActionAsync(int approvalRequestId, string action, int actionByUserId, string? comment, CancellationToken cancellationToken = default);`

## PART 2: SEQUENCE DIAGRAM SPECIFICATIONS (Main Workflows)

### 1. Automatic ETR Generation (Triggered when creating a CourseEnrollment)
1. **Actor** calls `POST /api/Enrollments` passing `CreateEnrollmentRequest`.
2. **EnrollmentsController** delegates to `IEnrollmentService.CreateEnrollmentAsync()`.
3. **EnrollmentService**:
   - Calls `_unitOfWork.BeginTransactionAsync()`.
   - Validates class exists via `_unitOfWork.ClassRepository.GetByIdAsync()`.
   - Checks for duplicate active enrollments via `CourseEnrollmentRepository.GetAllAsync()` and `ClassRepository.GetAllAsync()`. *Throws InvalidOperationException if duplicate.*
   - Inserts new `CourseEnrollment` via `CourseEnrollmentRepository.AddAsync()`.
   - Calls `await _unitOfWork.SaveAsync()`.
   - Generates new `ETRCourseRecord` tied to the enrollment and calls `ETRCourseRecordRepository.AddAsync()`.
   - Calls `await _unitOfWork.SaveAsync()`.
   - Retrieves all `CourseSubject` entries for the `CourseId`.
   - Loops through subjects and creates a `SubjectResult` for each, calling `SubjectResultRepository.AddAsync()`.
   - Calls `await _unitOfWork.SaveAsync()`.
   - Calls `_unitOfWork.CommitTransactionAsync()`.
4. Returns `CreateEnrollmentResponse` back to the Controller and then to the Actor.

### 2. Session & Attendance Recording (Including auto-calculation of AttendanceRate)
1. **Actor** calls `POST /api/Attendance/record` passing `CreateAttendanceRecordRequest`.
2. **AttendanceController** delegates to `IAttendanceService.RecordAttendanceAsync()`.
3. **AttendanceService**:
   - Calls `_unitOfWork.BeginTransactionAsync()`.
   - Retrieves `Session` via `SessionRepository.GetByIdAsync()`. *Throws InvalidOperationException if session not found or already confirmed.*
   - Inserts `AttendanceRecord` via `AttendanceRecordRepository.AddAsync()`.
   - Calls `await _unitOfWork.SaveAsync()`.
   - Auto-Calculates AttendanceRate:
     - Fetches existing `SubjectResult` for the Learner and Subject.
     - Fetches all "Present" `AttendanceRecord`s for the enrollment.
     - Fetches all `Session`s for the subject.
     - Computes rate `(Present Count / Total Sessions) * 100` and updates `SubjectResult`.
   - Calls `await _unitOfWork.SaveAsync()`.
   - Calls `_unitOfWork.CommitTransactionAsync()`.
4. Returns `AttendanceRecordResponse` back to the Actor.

### 3. Assessment Grading & Instructor Subject Sign-off
**Assessment Grading:**
1. **Actor** calls `POST /api/Assessments/record`.
2. **AssessmentsController** calls `IAssessmentService.RecordAssessmentScoreAsync()`.
3. **AssessmentService**:
   - Calls `_unitOfWork.BeginTransactionAsync()`.
   - Validates `Assessment` and `SubjectResult` existence.
   - Calculates `ResultStatus` by comparing submitted `Score` to `Assessment.PassingScore`.
   - Adds `AssessmentResult` to DB.
   - Calls `await _unitOfWork.SaveAsync()`.
   - Calls `_unitOfWork.CommitTransactionAsync()`.
4. Returns `AssessmentResultResponse`.

**Instructor Subject Sign-off:**
1. **Actor** calls `POST /api/Assessments/signoff`.
2. **AssessmentsController** calls `IAssessmentService.SignoffSubjectResultAsync()`.
3. **AssessmentService**:
   - Calls `_unitOfWork.BeginTransactionAsync()`.
   - Validates `SubjectResult` existence.
   - Adds `SubjectSignoff` record.
   - Updates `SubjectResult.Status` to "Passed".
   - Calls `await _unitOfWork.SaveAsync()`.
   - Calls `_unitOfWork.CommitTransactionAsync()`.
4. Returns `SubjectSignoffResponse`.

### 4. Evidence Upload & QA Verification
*Note: Full file upload streaming is deferred in the current implementation, but the core entity operations define the following workflow.*
1. **Actor** calls `EvidencesController` to upload files or query records (currently implements `GET`).
2. **System** creates `EvidenceFile` entity mapping standard file attributes (`FilePath`, `MimeType`).
3. **QA Verification Step**:
   - Updates `EvidenceFile.VerificationStatus` to Approved/Rejected.
   - Appends `VerificationComment`, `VerifiedBy`, and `VerifiedAt`.
   - Triggers `AppDbContext.Compliance.cs` (`ImmutabilityValidator`) during `SaveChanges`. The validator traverses `EvidenceFile.SubjectResultId -> EnrollmentId -> ETRCourseRecord` to ensure the ETR is not "Locked". If it is locked, it throws `ImmutabilityViolationException`.
4. Returns verification response.

### 5. ETR Submission & Manager Approval (Pre-validation & Freeze Data)
1. **Actor** calls `POST /api/Approvals/{id}/process` with an action (e.g., "Approve").
2. **ApprovalsController** delegates to `IApprovalService.ProcessApprovalActionAsync()`.
3. **ApprovalService**:
   - Calls `_unitOfWork.BeginTransactionAsync()`.
   - Retrieves `ApprovalRequest` via repository. *Throws InvalidOperationException if not found.*
   - Transitions `ApprovalRequest.CurrentStatus` to "Approved".
   - Generates `ApprovalHistory` audit record.
   - **Freeze Data Execution**: Since the status is "Approved", the service fetches the related `ETRCourseRecord` and sets its `Status` to "Completed" and `IsLocked` to `true`.
   - Calls `await _unitOfWork.SaveAsync()`.
   - *Under the hood*, `AppDbContext.Compliance.cs` generates `AuditLog` entries for the ApprovalRequest, ApprovalHistory, and ETRCourseRecord changes automatically.
   - Calls `_unitOfWork.CommitTransactionAsync()`.
4. Returns `ApprovalRequestResponse` back to the Actor.
