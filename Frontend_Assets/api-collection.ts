/**
 * ALL-IN-ONE API DEFINITIONS
 * Automatically generated from Swagger/OpenAPI spec.
 */

export interface AccountResponse {
  accountId: number;
  username?: string;
  roleId: number;
  departmentId: number;
  status?: string;
}

export interface AuditLogResponse {
  auditLogId: number;
  accountId?: number;
  etrRecordId?: number;
  actionType?: string;
  entityName?: string;
  recordId: number;
  oldValue?: string;
  newValue?: string;
  description?: string;
  ipAddress?: string;
  userAgent?: string;
}

export interface ChangePasswordRequest {
  userId: number;
  oldPassword?: string;
  newPassword?: string;
}

export interface CreateAccountRequest {
  username?: string;
  password?: string;
  roleId: number;
  departmentId: number;
}

export interface CreateApprovalRequest {
  etrCourseRecordId: number;
  submittedBy: number;
  currentApproverId?: number;
}

export interface CreateAssessmentResultRequest {
  assessmentId: number;
  accountId: number;
  subjectResultId: number;
  score: number;
  remark?: string;
}

export interface CreateAttendanceRecordRequest {
  sessionId: number;
  classStudentId: number;
  status?: string;
  remarks?: string;
}

export interface CreateClassRequest {
  classCode?: string;
  className?: string;
  courseId: number;
  startDate: string;
  endDate: string;
  location?: string;
  capacity: number;
  status?: string;
}

export interface CreateCompletionRequirementRequest {
  courseId: number;
  requirementName: string;
  description?: string;
  isMandatory: boolean;
  displayOrder: number;
}

export interface CreateCourseRequest {
  courseCode?: string;
  courseName?: string;
  description?: string;
  durationHours: number;
  status?: string;
}

export interface CreateDepartmentRequest {
  departmentName: string;
  description?: string;
}

export interface CreateEnrollmentRequest {
  accountId: number;
  classId: number;
}

export interface CreateEvidenceRequest {
  evidenceTypeId: number;
  accountId: number;
  subjectResultId: number;
  attendanceRecordId?: number;
  assessmentResultId?: number;
  fileName: string;
  filePath: string;
  fileExtension?: string;
  mimeType?: string;
  fileSize: number;
}

export interface CreateEvidenceTypeRequest {
  typeName: string;
  description?: string;
}

export interface CreatePracticalChecklistRequest {
  courseId: number;
  subjectId: number;
  itemName: string;
  description?: string;
  isRequired: boolean;
  displayOrder: number;
}

export interface CreateSessionRequest {
  classId: number;
  subjectId: number;
  sessionTitle: string;
  sessionDate: string;
  location?: string;
}

export interface CreateSubjectRequest {
  subjectCode?: string;
  subjectName?: string;
  subjectType?: string;
  defaultHours: number;
  assessmentMethod?: string;
  description?: string;
  status?: string;
}

export interface CreateSubjectSignoffRequest {
  subjectResultId: number;
  role?: string;
  comment?: string;
}

export interface CreateUserProfileRequest {
  userCode?: string;
  fullName?: string;
  email?: string;
  phone?: string;
  dateOfBirth: string;
  gender?: string;
  organization?: string;
}

export interface EnrollmentResponse {
  enrollmentId: number;
  accountId: number;
  classId: number;
  status?: string;
  enrolledAt: string;
}

export interface EtrDetailsResponse {
  etrCourseRecordId: number;
  enrollmentId: number;
  status?: string;
  isLocked: boolean;
  submittedAt?: string;
  verifiedAt?: string;
  completedAt?: string;
  subjectResults?: SubjectResultResponse[];
}

export interface EtrRecordResponse {
  etrCourseRecordId: number;
  enrollmentId: number;
  status?: string;
  isLocked: boolean;
  submittedAt?: string;
  verifiedAt?: string;
  completedAt?: string;
}

export interface ExportJobResponse {
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

export interface ExportRequest {
  userId: number;
}

export interface ForgotPasswordRequest {
  email?: string;
}

export interface GoogleLoginRequest {
  idToken?: string;
}

export interface LoginRequestDto {
  username?: string;
  password?: string;
}

export interface RefreshTokenRequest {
  refreshToken?: string;
}

export interface ResetPasswordRequest {
  token?: string;
  newPassword?: string;
}

export interface SubjectResultResponse {
  subjectResultId: number;
  subjectId: number;
  status?: string;
  createdAt: string;
}

export interface UpdateAccountStatusRequest {
  status?: string;
}

export interface UpdateClassRequest {
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

export interface UpdateCompletionRequirementRequest {
  requirementName: string;
  description?: string;
  isMandatory: boolean;
  displayOrder: number;
}

export interface UpdateCourseRequest {
  courseId: number;
  courseCode?: string;
  courseName?: string;
  description?: string;
  durationHours: number;
  status?: string;
}

export interface UpdateDepartmentRequest {
  departmentName: string;
  description?: string;
}

export interface UpdateEvidenceTypeRequest {
  typeName: string;
  description?: string;
}

export interface UpdatePracticalChecklistRequest {
  itemName: string;
  description?: string;
  isRequired: boolean;
  displayOrder: number;
}

export interface UpdateSessionRequest {
  sessionTitle: string;
  sessionDate: string;
  location?: string;
}

export interface UpdateSubjectRequest {
  subjectId: number;
  subjectCode?: string;
  subjectName?: string;
  subjectType?: string;
  defaultHours: number;
  assessmentMethod?: string;
  description?: string;
  status?: string;
}

export interface UpdateUserProfileRequest {
  fullName?: string;
  email?: string;
  phone?: string;
  dateOfBirth: string;
  gender?: string;
  organization?: string;
}

export interface UserProfileResponse {
  accountId: number;
  userCode?: string;
  fullName?: string;
  email?: string;
  phone?: string;
  dateOfBirth: string;
  gender?: string;
  organization?: string;
}

export const API_ENDPOINTS = {
  GET_ACCOUNTS: '/api/Accounts',
  POST_ACCOUNTS: '/api/Accounts',
  GET_ACCOUNTS_BY_ID: (id: number | string) => `/api/Accounts/${id}`,
  DELETE_ACCOUNTS_BY_ID: (id: number | string) => `/api/Accounts/${id}`,
  PUT_ACCOUNTS_BY_ID_STATUS: (id: number | string) => `/api/Accounts/${id}/status`,
  GET_APPROVALS: '/api/Approvals',
  POST_APPROVALS: '/api/Approvals',
  POST_APPROVALS_BY_ID_PROCESS: (id: number | string) => `/api/Approvals/${id}/process`,
  POST_ASSESSMENTS_RECORD: '/api/Assessments/record',
  POST_ASSESSMENTS_SIGNOFF: '/api/Assessments/signoff',
  GET_ASSESSMENTS_STUDENT_BY_CLASSSTUDENTID: (classStudentId: number | string) => `/api/Assessments/student/${classStudentId}`,
  POST_ATTENDANCE_RECORD: '/api/Attendance/record',
  POST_ATTENDANCE_SESSIONS_BY_SESSIONID_CONFIRM: (sessionId: number | string) => `/api/Attendance/sessions/${sessionId}/confirm`,
  GET_ATTENDANCE_STUDENT_BY_CLASSSTUDENTID: (classStudentId: number | string) => `/api/Attendance/student/${classStudentId}`,
  GET_AUDIT: '/api/Audit',
  GET_AUDIT_BY_ID: (id: number | string) => `/api/Audit/${id}`,
  GET_AUDIT_SEARCH: '/api/Audit/search',
  POST_AUTH_LOGIN: '/api/auth/login',
  POST_AUTH_GOOGLE_LOGIN: '/api/auth/google-login',
  POST_AUTH_REFRESH_TOKEN: '/api/auth/refresh-token',
  POST_AUTH_LOGOUT: '/api/auth/logout',
  POST_AUTH_CHANGE_PASSWORD: '/api/auth/change-password',
  POST_AUTH_FORGOT_PASSWORD: '/api/auth/forgot-password',
  POST_AUTH_RESET_PASSWORD: '/api/auth/reset-password',
  GET_AUTH_ME: '/api/auth/me',
  GET_CLASSES: '/api/Classes',
  POST_CLASSES: '/api/Classes',
  GET_CLASSES_BY_ID: (id: number | string) => `/api/Classes/${id}`,
  PUT_CLASSES_BY_ID: (id: number | string) => `/api/Classes/${id}`,
  DELETE_CLASSES_BY_ID: (id: number | string) => `/api/Classes/${id}`,
  GET_COMPLETIONREQUIREMENTS: '/api/CompletionRequirements',
  POST_COMPLETIONREQUIREMENTS: '/api/CompletionRequirements',
  GET_COMPLETIONREQUIREMENTS_BY_ID: (id: number | string) => `/api/CompletionRequirements/${id}`,
  PUT_COMPLETIONREQUIREMENTS_BY_ID: (id: number | string) => `/api/CompletionRequirements/${id}`,
  DELETE_COMPLETIONREQUIREMENTS_BY_ID: (id: number | string) => `/api/CompletionRequirements/${id}`,
  GET_COMPLETIONREQUIREMENTS_COURSE_BY_COURSEID: (courseId: number | string) => `/api/CompletionRequirements/course/${courseId}`,
  GET_COURSES: '/api/Courses',
  POST_COURSES: '/api/Courses',
  GET_COURSES_BY_ID: (id: number | string) => `/api/Courses/${id}`,
  PUT_COURSES_BY_ID: (id: number | string) => `/api/Courses/${id}`,
  DELETE_COURSES_BY_ID: (id: number | string) => `/api/Courses/${id}`,
  GET_DASHBOARD_STATS: '/api/Dashboard/stats',
  GET_DEPARTMENTS: '/api/Departments',
  POST_DEPARTMENTS: '/api/Departments',
  GET_DEPARTMENTS_BY_ID: (id: number | string) => `/api/Departments/${id}`,
  PUT_DEPARTMENTS_BY_ID: (id: number | string) => `/api/Departments/${id}`,
  DELETE_DEPARTMENTS_BY_ID: (id: number | string) => `/api/Departments/${id}`,
  GET_ENROLLMENTS: '/api/Enrollments',
  POST_ENROLLMENTS: '/api/Enrollments',
  GET_ENROLLMENTS_BY_ID: (id: number | string) => `/api/Enrollments/${id}`,
  GET_ENROLLMENTS_STUDENT_BY_STUDENTID: (studentId: number | string) => `/api/Enrollments/student/${studentId}`,
  GET_ETR: '/api/Etr',
  GET_ETR_MY_ETR: '/api/Etr/my-etr',
  GET_ETR_BY_ID: (id: number | string) => `/api/Etr/${id}`,
  POST_ETR_BY_ID_SUBMIT: (id: number | string) => `/api/Etr/${id}/submit`,
  POST_ETR_BY_ID_VERIFY: (id: number | string) => `/api/Etr/${id}/verify`,
  POST_ETR_BY_ID_COMPLETE: (id: number | string) => `/api/Etr/${id}/complete`,
  GET_EVIDENCES: '/api/Evidences',
  POST_EVIDENCES: '/api/Evidences',
  GET_EVIDENCES_BY_ID: (id: number | string) => `/api/Evidences/${id}`,
  DELETE_EVIDENCES_BY_ID: (id: number | string) => `/api/Evidences/${id}`,
  GET_EVIDENCETYPES: '/api/EvidenceTypes',
  POST_EVIDENCETYPES: '/api/EvidenceTypes',
  GET_EVIDENCETYPES_BY_ID: (id: number | string) => `/api/EvidenceTypes/${id}`,
  PUT_EVIDENCETYPES_BY_ID: (id: number | string) => `/api/EvidenceTypes/${id}`,
  DELETE_EVIDENCETYPES_BY_ID: (id: number | string) => `/api/EvidenceTypes/${id}`,
  GET_EXPORTS_BY_ID: (id: number | string) => `/api/Exports/${id}`,
  POST_EXPORTS_TRAINING_PACKAGE: '/api/Exports/training-package',
  POST_EXPORTS_PDF: '/api/Exports/pdf',
  POST_EXPORTS_DASHBOARD: '/api/Exports/dashboard',
  GET_EXPORTS_DOWNLOAD_BY_ID: (id: number | string) => `/api/Exports/download/${id}`,
  GET_PRACTICALCHECKLISTS: '/api/PracticalChecklists',
  POST_PRACTICALCHECKLISTS: '/api/PracticalChecklists',
  GET_PRACTICALCHECKLISTS_BY_ID: (id: number | string) => `/api/PracticalChecklists/${id}`,
  PUT_PRACTICALCHECKLISTS_BY_ID: (id: number | string) => `/api/PracticalChecklists/${id}`,
  DELETE_PRACTICALCHECKLISTS_BY_ID: (id: number | string) => `/api/PracticalChecklists/${id}`,
  GET_PRACTICALCHECKLISTS_COURSE_BY_COURSEID_SUBJECT_BY_SUBJECTID: (courseId: number | string, subjectId: number | string) => `/api/PracticalChecklists/course/${courseId}/subject/${subjectId}`,
  GET_REPORTS_SUMMARY: '/api/Reports/summary',
  GET_SEARCH_CLASSES: '/api/Search/classes',
  GET_SEARCH_ETRS: '/api/Search/etrs',
  GET_SESSIONS: '/api/Sessions',
  POST_SESSIONS: '/api/Sessions',
  GET_SESSIONS_BY_ID: (id: number | string) => `/api/Sessions/${id}`,
  PUT_SESSIONS_BY_ID: (id: number | string) => `/api/Sessions/${id}`,
  DELETE_SESSIONS_BY_ID: (id: number | string) => `/api/Sessions/${id}`,
  GET_SUBJECTS: '/api/Subjects',
  POST_SUBJECTS: '/api/Subjects',
  GET_SUBJECTS_BY_ID: (id: number | string) => `/api/Subjects/${id}`,
  PUT_SUBJECTS_BY_ID: (id: number | string) => `/api/Subjects/${id}`,
  DELETE_SUBJECTS_BY_ID: (id: number | string) => `/api/Subjects/${id}`,
  GET_USERPROFILES: '/api/UserProfiles',
  GET_USERPROFILES_LEARNERS: '/api/UserProfiles/learners',
  GET_USERPROFILES_ME: '/api/UserProfiles/me',
  PUT_USERPROFILES_ME: '/api/UserProfiles/me',
  GET_USERPROFILES_BY_ACCOUNTID: (accountId: number | string) => `/api/UserProfiles/${accountId}`,
  POST_USERPROFILES_BY_ACCOUNTID: (accountId: number | string) => `/api/UserProfiles/${accountId}`,
  PUT_USERPROFILES_BY_ACCOUNTID: (accountId: number | string) => `/api/UserProfiles/${accountId}`,
};


export const apiClient = {
  async get<T>(url: string, token?: string): Promise<T> {
    return request<T>(url, { method: 'GET' }, token);
  },
  async post<T>(url: string, body: any, token?: string): Promise<T> {
    return request<T>(url, { method: 'POST', body: JSON.stringify(body) }, token);
  },
  async put<T>(url: string, body: any, token?: string): Promise<T> {
    return request<T>(url, { method: 'PUT', body: JSON.stringify(body) }, token);
  },
  async delete<T>(url: string, token?: string): Promise<T> {
    return request<T>(url, { method: 'DELETE' }, token);
  }
};

async function request<T>(url: string, config: RequestInit, token?: string): Promise<T> {
  const headers: HeadersInit = {
    'Content-Type': 'application/json',
    'Accept': 'application/json'
  };
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }
  
  const response = await fetch(url, { ...config, headers });
  if (!response.ok) {
    throw new Error(`API Error: ${response.statusText}`);
  }
  
  if (response.status === 204) return {} as T;
  
  return await response.json();
}
