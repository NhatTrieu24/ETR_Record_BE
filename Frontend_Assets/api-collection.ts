// Auto-generated API collection

public interface AccountResponse {
    accountId: number;
    username?: string;
    roleId: number;
    departmentId: number;
    status?: string;
}

public interface ApprovalActionRequest {
    userId: number;
    comment?: string;
}

public interface ApprovalHistoryResponse {
    approvalHistoryId: number;
    approvalRequestId: number;
    actionBy: number;
    actionType?: string;
    previousStatus?: string;
    newStatus?: string;
    comments?: string;
    actionAt: string;
}

public interface ApprovalRequestResponse {
    approvalRequestId: number;
    eTRCourseRecordId: number;
    currentStatus?: string;
    submittedBy: number;
    submittedAt: string;
    currentApproverId?: number;
    completedAt?: string;
}

public interface AssessmentComponentResponse {
    assessmentComponentId: number;
    courseId: number;
    componentName?: string;
    assessmentType?: string;
    weight: number;
    passingScore: number;
    isRequired: boolean;
    displayOrder: number;
}

public interface AssessmentResultResponse {
    assessmentResultId: number;
    assessmentId: number;
    accountId: number;
    subjectResultId: number;
    score: number;
    resultStatus?: string;
    gradedByAccountId: number;
    recordedAt: string;
    publishedAt?: string;
    isPublished: boolean;
    takenAt?: string;
    remark?: string;
}

public interface AssignInstructorRequest {
    classId: number;
    userId: number;
    isPrimaryInstructor: boolean;
}

public interface AttendanceRecordResponse {
    attendanceRecordId: number;
    sessionId: number;
    classStudentId: number;
    status?: string;
    remarks?: string;
    recordedByAccountId: number;
    recordedAt: string;
}

public interface AttendanceSessionResponse {
    sessionId: number;
    classId: number;
    subjectId: number;
    sessionTitle?: string;
    sessionDate: string;
    location?: string;
    isConfirmed: boolean;
    confirmedByAccountId?: number;
    confirmedAt?: string;
}

public interface AuditLogResponse {
    auditLogId: number;
    accountId?: number;
    eTRRecordId?: number;
    actionType?: string;
    entityName?: string;
    recordId: number;
    oldValue?: string;
    newValue?: string;
    description?: string;
    iPAddress?: string;
    userAgent?: string;
}

public interface AuthResponse {
    accountId: number;
    username?: string;
    fullName?: string;
    role?: string;
    token?: string;
    refreshToken?: string;
}

public interface BulkAttendanceItemRequest {
    attendanceSessionId: number;
    learnerId: number;
    etrRecordId: number;
    status?: string;
    remarks?: string;
}

public interface BulkAttendanceRequest {
    records?: BulkAttendanceItemRequest[];
    recordedByUserId: number;
}

public interface ChangePasswordRequest {
    userId: number;
    oldPassword?: string;
    newPassword?: string;
}

public interface ChecklistItemResponse {
    eTRChecklistItemId: number;
    templateId: number;
    itemName?: string;
    description?: string;
    isRequired: boolean;
    displayOrder: number;
}

public interface ChecklistProgressResponse {
    progressId: number;
    eTRRecordId: number;
    checklistItemId: number;
    isCompleted: boolean;
    verifiedBy?: number;
    completedAt?: string;
    verificationComment?: string;
}

public interface ClassInstructorResponse {
    classInstructorId: number;
    classId: number;
    userId: number;
    isPrimaryInstructor: boolean;
    assignedAt: string;
}

public interface CompletionRequirementResponse {
    requirementId: number;
    courseId: number;
    requirementName?: string;
    description?: string;
    isMandatory: boolean;
    displayOrder: number;
}

public interface CompletionRequirementResponse {
    requirementId: number;
    courseId: number;
    requirementName?: string;
    description?: string;
    isMandatory: boolean;
    displayOrder: number;
}

public interface ConfirmSessionRequest {
    userId: number;
}

public interface CourseResponse {
    courseId: number;
    courseCode?: string;
    courseName?: string;
    description?: string;
    durationHours: number;
    status?: string;
}

public interface CreateAccountRequest {
    username?: string;
    password?: string;
    roleId: number;
    departmentId: number;
}

public interface CreateApprovalRequest {
    eTRCourseRecordId: number;
    submittedBy: number;
    currentApproverId?: number;
}

public interface CreateApprovalRequest {
    eTRCourseRecordId: number;
    currentApproverId?: number;
}

public interface CreateAssessmentComponentRequest {
    courseId: number;
    componentName?: string;
    assessmentType?: string;
    weight: number;
    passingScore: number;
    isRequired: boolean;
    displayOrder: number;
}

public interface CreateAssessmentResultRequest {
    assessmentId: number;
    accountId: number;
    subjectResultId: number;
    score: number;
    remark?: string;
}

public interface CreateAttendanceRecordRequest {
    sessionId: number;
    classStudentId: number;
    status?: string;
    remarks?: string;
}

public interface CreateAttendanceSessionRequest {
    classId: number;
    sessionTitle?: string;
    sessionDate: string;
    location?: string;
}

public interface CreateChecklistItemRequest {
    templateId: number;
    itemName?: string;
    description?: string;
    isRequired: boolean;
    displayOrder: number;
}

public interface CreateClassRequest {
    classCode?: string;
    className?: string;
    courseId: number;
    startDate: string;
    endDate: string;
    location?: string;
    capacity: number;
    status?: string;
}

public interface CreateCompletionRequirementRequest {
    courseId: number;
    requirementName?: string;
    description?: string;
    isMandatory: boolean;
    displayOrder: number;
}

public interface CreateCompletionRequirementRequest {
    courseId: number;
    requirementName?: string;
    description?: string;
    isMandatory: boolean;
    displayOrder: number;
}

public interface CreateCourseRequest {
    courseCode?: string;
    courseName?: string;
    description?: string;
    durationHours: number;
    status?: string;
}

public interface CreateDepartmentRequest {
    departmentName?: string;
    description?: string;
}

public interface CreateDepartmentRequest {
    departmentName?: string;
    description?: string;
}

public interface CreateEnrollmentRequest {
    accountId: number;
    classId: number;
}

public interface CreateEnrollmentResponse {
    enrollmentId: number;
    accountId: number;
    classId: number;
    status?: string;
    enrolledAt: string;
    etrCourseRecordId: number;
    etrStatus?: string;
    etrIsLocked: boolean;
}

public interface CreateEtrRecordRequest {
    enrollmentId: number;
}

public interface CreateEvidenceRequest {
    evidenceTypeId: number;
    fileName?: string;
    filePath?: string;
    fileExtension?: string;
    mimeType?: string;
    fileSize: number;
    uploadedBy: number;
    learnerId: number;
    eTRRecordId: number;
    attendanceRecordId?: number;
    assessmentResultId?: number;
}

public interface CreateEvidenceRequest {
    evidenceTypeId: number;
    accountId: number;
    subjectResultId: number;
    attendanceRecordId?: number;
    assessmentResultId?: number;
    fileName?: string;
    filePath?: string;
    fileExtension?: string;
    mimeType?: string;
    fileSize: number;
}

public interface CreateEvidenceTypeRequest {
    typeName?: string;
    description?: string;
}

public interface CreateEvidenceTypeRequest {
    typeName?: string;
    description?: string;
}

public interface CreatePracticalChecklistRequest {
    courseId: number;
    subjectId: number;
    itemName?: string;
    description?: string;
    isRequired: boolean;
    displayOrder: number;
}

public interface CreateRoleRequest {
    roleName?: string;
    description?: string;
}

public interface CreateSessionRequest {
    classId: number;
    subjectId: number;
    sessionTitle?: string;
    sessionDate: string;
    location?: string;
}

public interface CreateSubjectRequest {
    subjectCode?: string;
    subjectName?: string;
    subjectType?: string;
    defaultHours: number;
    assessmentMethod?: string;
    description?: string;
    status?: string;
}

public interface CreateSubjectSignoffRequest {
    subjectResultId: number;
    role?: string;
    comment?: string;
}

public interface CreateUserProfileRequest {
    userCode?: string;
    fullName?: string;
    email?: string;
    phone?: string;
    dateOfBirth: string;
    gender?: string;
    organization?: string;
}

public interface CreateUserRequest {
    username?: string;
    passwordHash?: string;
    fullName?: string;
    email?: string;
    roleId: number;
    departmentId: number;
}

public interface DepartmentResponse {
    departmentId: number;
    departmentName?: string;
    description?: string;
}

public interface DepartmentResponse {
    departmentId: number;
    departmentName?: string;
    description?: string;
}

public interface EnrollmentResponse {
    enrollmentId: number;
    accountId: number;
    classId: number;
    status?: string;
    enrolledAt: string;
}

public interface EtrActionRequest {
    userId: number;
}

public interface EtrDetailsResponse {
    eTRCourseRecordId: number;
    enrollmentId: number;
    status?: string;
    isLocked: boolean;
    submittedAt?: string;
    verifiedAt?: string;
    completedAt?: string;
    subjectResults?: SubjectResultResponse[];
}

public interface EtrRecordResponse {
    eTRCourseRecordId: number;
    enrollmentId: number;
    status?: string;
    isLocked: boolean;
    submittedAt?: string;
    verifiedAt?: string;
    completedAt?: string;
}

public interface EtrSummaryResponse {
    etrRecordId: number;
    status?: string;
    isLocked: boolean;
    totalItems: number;
    completedItems: number;
    progressPercentage: number;
}

public interface EvidenceActionRequest {
    verifiedByUserId: number;
    comment?: string;
}

public interface EvidenceFileResponse {
    evidenceFileId: number;
    evidenceTypeId: number;
    fileName?: string;
    filePath?: string;
    fileExtension?: string;
    mimeType?: string;
    fileSize: number;
    verificationStatus?: string;
    qAComment?: string;
    verifiedBy?: number;
    verifiedAt?: string;
    uploadedBy: number;
    uploadedAt: string;
}

public interface EvidenceResponse {
    evidenceFileId: number;
    evidenceTypeId: number;
    uploadedByAccountId: number;
    accountId: number;
    subjectResultId: number;
    attendanceRecordId?: number;
    assessmentResultId?: number;
    fileName?: string;
    filePath?: string;
    fileExtension?: string;
    mimeType?: string;
    fileSize: number;
    verificationStatus?: string;
    verifiedByAccountId?: number;
    verifiedAt?: string;
    verificationComment?: string;
    uploadedAt: string;
}

public interface EvidenceTypeResponse {
    evidenceTypeId: number;
    typeName?: string;
    description?: string;
}

public interface EvidenceTypeResponse {
    evidenceTypeId: number;
    typeName?: string;
    description?: string;
}

public interface ExportJobResponse {
    exportJobId: number;
    requestedByAccountId: number;
    exportType?: string;
    fileName?: string;
    filePath?: string;
    status?: string;
    requestedAt: string;
    completedAt?: string;
    downloadExpiredAt?: string;
}

public interface ExportRequest {
    userId: number;
}

public interface ForgotPasswordRequest {
    email?: string;
}

public interface GoogleLoginRequest {
    idToken?: string;
}

public interface LoginRequest {
    username?: string;
    password?: string;
}

public interface LoginRequestDto {
    username?: string;
    password?: string;
}

public interface LoginResponseDto {
    userId: number;
    username?: string;
    fullName?: string;
    roleName?: string;
    token?: string;
}

public interface PracticalChecklistResponse {
    practicalChecklistId: number;
    courseId: number;
    subjectId: number;
    itemName?: string;
    description?: string;
    isRequired: boolean;
    displayOrder: number;
}

public interface RefreshTokenRequest {
    refreshToken?: string;
}

public interface ResetPasswordRequest {
    token?: string;
    newPassword?: string;
}

public interface RoleResponse {
    roleId: number;
    roleName?: string;
    description?: string;
}

public interface SessionResponse {
    sessionId: number;
    classId: number;
    subjectId: number;
    sessionTitle?: string;
    sessionDate: string;
    location?: string;
    isConfirmed: boolean;
    confirmedByAccountId?: number;
    confirmedAt?: string;
}

public interface SubjectResponse {
    subjectId: number;
    subjectCode?: string;
    subjectName?: string;
    subjectType?: string;
    defaultHours: number;
    assessmentMethod?: string;
    description?: string;
    status?: string;
}

public interface SubjectResultResponse {
    subjectResultId: number;
    subjectId: number;
    status?: string;
    createdAt: string;
}

public interface SubjectSignoffResponse {
    subjectSignoffId: number;
    subjectResultId: number;
    signoffByAccountId: number;
    role?: string;
    signoffAt: string;
    comment?: string;
}

public interface TrainingClassResponse {
    classId: number;
    classCode?: string;
    className?: string;
    courseId: number;
    startDate: string;
    endDate: string;
    location?: string;
    capacity: number;
    status?: string;
}

public interface UpdateAccountStatusRequest {
    status?: string;
}

public interface UpdateApprovalRequest {
    currentApproverId?: number;
}

public interface UpdateAssessmentComponentRequest {
    assessmentComponentId: number;
    courseId: number;
    componentName?: string;
    assessmentType?: string;
    weight: number;
    passingScore: number;
    isRequired: boolean;
    displayOrder: number;
}

public interface UpdateAssessmentResultRequest {
    score: number;
    remark?: string;
}

public interface UpdateAttendanceRecordRequest {
    status?: string;
    remarks?: string;
}

public interface UpdateAttendanceSessionRequest {
    attendanceSessionId: number;
    classId: number;
    sessionTitle?: string;
    sessionDate: string;
    location?: string;
    isConfirmed: boolean;
}

public interface UpdateChecklistItemRequest {
    eTRChecklistItemId: number;
    templateId: number;
    itemName?: string;
    description?: string;
    isRequired: boolean;
    displayOrder: number;
}

public interface UpdateChecklistProgressRequest {
    isCompleted: boolean;
    verifiedByUserId?: number;
    comment?: string;
}

public interface UpdateClassRequest {
    classId: number;
    classCode?: string;
    className?: string;
    courseId: number;
    startDate: string;
    endDate: string;
    location?: string;
    capacity: number;
    status?: string;
}

public interface UpdateCompletionRequirementRequest {
    requirementId: number;
    courseId: number;
    requirementName?: string;
    description?: string;
    isMandatory: boolean;
    displayOrder: number;
}

public interface UpdateCompletionRequirementRequest {
    requirementName?: string;
    description?: string;
    isMandatory: boolean;
    displayOrder: number;
}

public interface UpdateCourseRequest {
    courseId: number;
    courseCode?: string;
    courseName?: string;
    description?: string;
    durationHours: number;
    status?: string;
}

public interface UpdateDepartmentRequest {
    departmentId: number;
    departmentName?: string;
    description?: string;
}

public interface UpdateDepartmentRequest {
    departmentName?: string;
    description?: string;
}

public interface UpdateEnrollmentRequest {
    enrollmentId: number;
    learnerId: number;
    classId: number;
    status?: string;
    enrolledAt: string;
}

public interface UpdateEtrRecordRequest {
    status?: string;
    isLocked: boolean;
}

public interface UpdateEvidenceRequest {
    evidenceFileId: number;
    evidenceTypeId: number;
    fileName?: string;
    filePath?: string;
    fileExtension?: string;
    mimeType?: string;
    fileSize: number;
}

public interface UpdateEvidenceTypeRequest {
    evidenceTypeId: number;
    typeName?: string;
    description?: string;
}

public interface UpdateEvidenceTypeRequest {
    typeName?: string;
    description?: string;
}

public interface UpdatePracticalChecklistRequest {
    itemName?: string;
    description?: string;
    isRequired: boolean;
    displayOrder: number;
}

public interface UpdateRoleRequest {
    roleId: number;
    roleName?: string;
    description?: string;
}

public interface UpdateSessionRequest {
    sessionTitle?: string;
    sessionDate: string;
    location?: string;
}

public interface UpdateSubjectRequest {
    subjectId: number;
    subjectCode?: string;
    subjectName?: string;
    subjectType?: string;
    defaultHours: number;
    assessmentMethod?: string;
    description?: string;
    status?: string;
}

public interface UpdateUserProfileRequest {
    fullName?: string;
    email?: string;
    phone?: string;
    dateOfBirth: string;
    gender?: string;
    organization?: string;
}

public interface UpdateUserRequest {
    userId: number;
    fullName?: string;
    email?: string;
    roleId: number;
    departmentId: number;
    isActive: boolean;
}

public interface UploadEvidenceRequest {
    file?: IFormFile;
    evidenceTypeId: number;
    uploadedBy: number;
    learnerId: number;
    eTRRecordId: number;
    attendanceRecordId?: number;
    assessmentResultId?: number;
}

public interface UserProfileResponse {
    accountId: number;
    userCode?: string;
    fullName?: string;
    email?: string;
    phone?: string;
    dateOfBirth: string;
    gender?: string;
    organization?: string;
}

public interface UserResponse {
    userId: number;
    username?: string;
    fullName?: string;
    email?: string;
    roleId: number;
    departmentId: number;
    isActive: boolean;
}

export const API_ENDPOINTS = {
    // =========================================
    // Accounts Module
    // =========================================
    // [Flow]: Quản lý Định danh &amp; Truy cập
    // [Action]: Manages core system authentication accounts, roles, departments, and statuses.
    GET_UPDATEACCOUNTSTATUS: '/api/Accounts',

    // [Flow]: Quản lý Định danh &amp; Truy cập
    // [Action]: Xóa một tài khoản khỏi hệ thống.
    DELETE_DELETEACCOUNT: '/api/Accounts/{id:int}',

    // =========================================
    // Approvals Module
    // =========================================
    // [Flow]: Xử lý ETR
    // [Action]: Processes approval workflows and state transitions for ETR records.
    GET_GETALLAPPROVALREQUESTS: '/api/Approvals',

    // =========================================
    // Assessments Module
    // =========================================
    // [Flow]: Thực thi Đào tạo
    // [Action]: Records assessment scores and handles subject signoffs.
    GET_GETALL: '/api/Assessments',

    // [Flow]: Thực thi Đào tạo
    // [Action]: Ghi nhận điểm số cho một bài kiểm tra (assessment) cụ thể.
    POST_RECORDASSESSMENT: '/api/Assessments/record',

    // [Flow]: Thực thi Đào tạo
    // [Action]: Ký xác nhận (sign off) kết quả môn học.
    POST_SIGNOFFSUBJECT: '/api/Assessments/signoff',

    // [Flow]: Thực thi Đào tạo
    // [Action]: Lấy danh sách kết quả kiểm tra của một học viên cụ thể trong lớp.
    GET_GETASSESSMENTRESULTS: '/api/Assessments/student/{classStudentId}',

    // =========================================
    // Attendance Module
    // =========================================
    // [Flow]: Thực thi Đào tạo
    // [Action]: Records student attendance and confirms session completion.
    GET_GETALL: '/api/Attendance',

    // [Flow]: Thực thi Đào tạo
    // [Action]: Điểm danh cho một buổi học (session) cụ thể.
    POST_RECORDATTENDANCE: '/api/Attendance/record',

    // [Flow]: Thực thi Đào tạo
    // [Action]: Xác nhận phiên điểm danh đã được chốt (finalized).
    POST_CONFIRMSESSION: '/api/Attendance/sessions/{sessionId}/confirm',

    // [Flow]: Thực thi Đào tạo
    // [Action]: Lấy lịch sử điểm danh của một học viên cụ thể trong lớp.
    GET_GETATTENDANCERECORDS: '/api/Attendance/student/{classStudentId}',

    // =========================================
    // Audit Module
    // =========================================
    AUDIT: '/api/Audit',

    // =========================================
    // Auth Module
    // =========================================
    // [Flow]: Quản lý Định danh &amp; Truy cập
    // [Action]: Authenticates users and generates JWT tokens based on Account credentials.
    POST_LOGIN: '/api/auth/login',

    // [Flow]: Quản lý Định danh &amp; Truy cập
    // [Action]: Xác thực người dùng qua Google OAuth và trả về JWT token.
    POST_CHANGEPASSWORD: '/api/auth/google-login',

    // [Flow]: Quản lý Định danh &amp; Truy cập
    // [Action]: Khởi tạo luồng quên mật khẩu.
    POST_GETME: '/api/auth/forgot-password',

    // =========================================
    // Classes Module
    // =========================================
    // [Flow]: Quản lý Dữ liệu Gốc (Master Data)
    // [Action]: Manages classes and scheduling.
    GET_GETALLCLASSES: '/api/Classes',

    // [Flow]: Quản lý Dữ liệu Gốc (Master Data)
    // [Action]: Lấy thông tin một lớp học theo ID.
    GET_GETCLASS: '/api/Classes/{id}',

    // [Flow]: Quản lý Dữ liệu Gốc (Master Data)
    // [Action]: Tạo một lớp học mới.
    POST_CREATECLASS: '/api/Classes',

    // [Flow]: Quản lý Dữ liệu Gốc (Master Data)
    // [Action]: Cập nhật một lớp học hiện có.
    PUT_UPDATECLASS: '/api/Classes/{id}',

    // [Flow]: Quản lý Dữ liệu Gốc (Master Data)
    // [Action]: Xóa mềm (soft delete) một lớp học.
    DELETE_DELETECLASS: '/api/Classes/{id}',

    // =========================================
    // CompletionRequirements Module
    // =========================================
    COMPLETIONREQUIREMENTS: '/api/CompletionRequirements',

    // =========================================
    // Courses Module
    // =========================================
    // [Flow]: Quản lý Dữ liệu Gốc (Master Data)
    // [Action]: Manages course catalogues.
    GET_GETALLCOURSES: '/api/Courses',

    // [Flow]: Quản lý Dữ liệu Gốc (Master Data)
    // [Action]: Lấy thông tin một khóa học theo ID.
    GET_GETCOURSE: '/api/Courses/{id}',

    // [Flow]: Quản lý Dữ liệu Gốc (Master Data)
    // [Action]: Tạo một khóa học mới.
    POST_CREATECOURSE: '/api/Courses',

    // [Flow]: Quản lý Dữ liệu Gốc (Master Data)
    // [Action]: Cập nhật một khóa học hiện có.
    PUT_UPDATECOURSE: '/api/Courses/{id}',

    // [Flow]: Quản lý Dữ liệu Gốc (Master Data)
    // [Action]: Xóa mềm (soft delete) một khóa học.
    DELETE_DELETECOURSE: '/api/Courses/{id}',

    // =========================================
    // Dashboard Module
    // =========================================
    // [Flow]: Báo cáo &amp; Phân tích
    // [Action]: Aggregates high-level statistics for system dashboards.
    GET_GETSTATS: '/api/Dashboard/stats',

    // =========================================
    // Departments Module
    // =========================================
    DEPARTMENTS: '/api/Departments',

    // =========================================
    // Enrollments Module
    // =========================================
    // [Flow]: Xử lý ETR
    // [Action]: Handles course enrollment operations.
    GET_CREATEENROLLMENT: '/api/Enrollments',

    // =========================================
    // Etr Module
    // =========================================
    // [Flow]: Xử lý ETR
    // [Action]: Handles the workflow and state transitions of the Electronic Training Record (ETR).
    GET_SUBMITETR: '/api/Etr',

    // [Action]: Xác minh một ETR đã được gửi.
    POST_VERIFYETR: '/api/Etr/{id}/verify',

    // [Action]: Hoàn tất một ETR đã được xác minh nếu đáp ứng đủ mọi điều kiện.
    POST_COMPLETEETR: '/api/Etr/{id}/complete',

    // =========================================
    // Evidences Module
    // =========================================
    // [Flow]: Document Management
    // [Action]: Manages uploaded evidence files for practical checklists and assessments.
    GET_GETALL: '/api/Evidences',

    // [Action]: Lấy thông tin một tệp bằng chứng cụ thể theo ID.
    GET_GETBYID: '/api/Evidences/{id}',

    // [Action]: Tải lên một bản ghi tệp bằng chứng mới.
    POST_UPLOADEVIDENCE: '/api/Evidences',

    // [Action]: Xóa mềm (soft delete) một tệp bằng chứng.
    DELETE_DELETEEVIDENCE: '/api/Evidences/{id}',

    // =========================================
    // EvidenceTypes Module
    // =========================================
    EVIDENCETYPES: '/api/EvidenceTypes',

    // =========================================
    // Exports Module
    // =========================================
    // [Flow]: Kiểm toán Hệ thống &amp; Tuân thủ
    // [Action]: Triggers and retrieves data export jobs.
    GET_DOWNLOADEXPORTFILE: '/api/Exports/{id:int}',

    // =========================================
    // PracticalChecklists Module
    // =========================================
    PRACTICALCHECKLISTS: '/api/PracticalChecklists',

    // =========================================
    // Reports Module
    // =========================================
    // [Flow]: Báo cáo &amp; Phân tích
    // [Action]: Generates summary reports for ETR and Class data.
    GET_GETSUMMARY: '/api/Reports/summary',

    // =========================================
    // Search Module
    // =========================================
    // [Flow]: Khám phá Hệ thống (System Discovery)
    // [Action]: Provides global search capabilities across classes and ETR records.
    GET_SEARCHCLASSES: '/api/Search/classes',

    // [Flow]: Khám phá Hệ thống (System Discovery)
    // [Action]: Tìm kiếm các hồ sơ ETR.
    GET_SEARCHETRS: '/api/Search/etrs',

    // =========================================
    // Sessions Module
    // =========================================
    // [Flow]: Session Management
    // [Action]: Manages class sessions.
    GET_GETALLSESSIONS: '/api/Sessions',

    // [Action]: Lấy thông tin một buổi học theo ID.
    GET_GETSESSION: '/api/Sessions/{id}',

    // [Action]: Tạo một buổi học mới.
    POST_CREATESESSION: '/api/Sessions',

    // [Action]: Cập nhật một buổi học hiện có.
    PUT_UPDATESESSION: '/api/Sessions/{id}',

    // [Action]: Xóa một buổi học.
    DELETE_DELETESESSION: '/api/Sessions/{id}',

    // =========================================
    // Subjects Module
    // =========================================
    // [Flow]: Quản lý Dữ liệu Gốc (Master Data)
    // [Action]: Manages subject catalogues.
    GET_GETALLSUBJECTS: '/api/Subjects',

    // [Flow]: Quản lý Dữ liệu Gốc (Master Data)
    // [Action]: Lấy thông tin một môn học theo ID.
    GET_GETSUBJECT: '/api/Subjects/{id}',

    // [Flow]: Quản lý Dữ liệu Gốc (Master Data)
    // [Action]: Tạo một môn học mới.
    POST_CREATESUBJECT: '/api/Subjects',

    // [Flow]: Quản lý Dữ liệu Gốc (Master Data)
    // [Action]: Cập nhật một môn học hiện có.
    PUT_UPDATESUBJECT: '/api/Subjects/{id}',

    // [Flow]: Quản lý Dữ liệu Gốc (Master Data)
    // [Action]: Xóa mềm (soft delete) một môn học.
    DELETE_DELETESUBJECT: '/api/Subjects/{id}',

    // =========================================
    // UserProfiles Module
    // =========================================
    USERPROFILES: '/api/UserProfiles',

};
