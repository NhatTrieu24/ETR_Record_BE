import os

translations = {
    "Identity &amp; Access Management": "Quản lý Định danh &amp; Truy cập",
    "Identity & Access Management": "Quản lý Định danh & Truy cập",
    "Retrieves all accounts.": "Lấy danh sách tất cả các tài khoản.",
    "Creates a new system account.": "Tạo một tài khoản hệ thống mới.",
    "Retrieves a specific account by ID.": "Lấy thông tin một tài khoản cụ thể theo ID.",
    "Deletes an account from the system.": "Xóa một tài khoản khỏi hệ thống.",
    "Updates the status of an existing account.": "Cập nhật trạng thái của một tài khoản hiện có.",
    
    "Training Execution": "Thực thi Đào tạo",
    "Records a score for a specific assessment.": "Ghi nhận điểm số cho một bài kiểm tra (assessment) cụ thể.",
    "Signs off a subject result.": "Ký xác nhận (sign off) kết quả môn học.",
    "Retrieves assessment results for a specific student in a class.": "Lấy danh sách kết quả kiểm tra của một học viên cụ thể trong lớp.",
    "Records attendance for a specific session.": "Điểm danh cho một buổi học (session) cụ thể.",
    "Confirms that an attendance session has been finalized.": "Xác nhận phiên điểm danh đã được chốt (finalized).",
    "Retrieves attendance records for a specific student in a class.": "Lấy lịch sử điểm danh của một học viên cụ thể trong lớp.",
    
    "System Auditing &amp; Compliance": "Kiểm toán Hệ thống &amp; Tuân thủ",
    "System Auditing & Compliance": "Kiểm toán Hệ thống & Tuân thủ",
    "Retrieves all audit logs.": "Lấy toàn bộ nhật ký hệ thống (audit logs).",
    "Retrieves a specific audit log by ID.": "Lấy thông tin một audit log cụ thể theo ID.",
    "Searches audit logs by action type, entity name, or description.": "Tìm kiếm audit logs theo loại hành động, tên thực thể hoặc mô tả.",
    
    "Authenticates user and returns JWT token.": "Xác thực người dùng và trả về JWT token.",
    "Authenticates user via Google OAuth and returns JWT token.": "Xác thực người dùng qua Google OAuth và trả về JWT token.",
    "Refreshes an expired JWT token using a valid refresh token.": "Cấp lại (refresh) một JWT token đã hết hạn bằng cách sử dụng refresh token hợp lệ.",
    "Invalidates the current user session.": "Hủy phiên đăng nhập (session) hiện tại của người dùng.",
    "Changes the password for the currently authenticated user.": "Thay đổi mật khẩu cho người dùng hiện đang xác thực.",
    "Initiates the forgot password flow.": "Khởi tạo luồng quên mật khẩu.",
    "Resets the user's password using a reset token.": "Đặt lại mật khẩu của người dùng bằng cách sử dụng reset token.",
    "Retrieves the currently authenticated user's account and profile information.": "Lấy thông tin tài khoản và hồ sơ (profile) của người dùng hiện đang xác thực.",
    
    "Master Data Management": "Quản lý Dữ liệu Gốc (Master Data)",
    "Retrieves all classes.": "Lấy danh sách tất cả các lớp học.",
    "Creates a new class.": "Tạo một lớp học mới.",
    "Retrieves a class by ID.": "Lấy thông tin một lớp học theo ID.",
    "Updates an existing class.": "Cập nhật một lớp học hiện có.",
    "Soft deletes a class.": "Xóa mềm (soft delete) một lớp học.",
    
    "Retrieves all courses.": "Lấy danh sách tất cả các khóa học.",
    "Creates a new course.": "Tạo một khóa học mới.",
    "Retrieves a course by ID.": "Lấy thông tin một khóa học theo ID.",
    "Updates an existing course.": "Cập nhật một khóa học hiện có.",
    "Soft deletes a course.": "Xóa mềm (soft delete) một khóa học.",
    
    "Reporting &amp; Analytics": "Báo cáo &amp; Phân tích",
    "Reporting & Analytics": "Báo cáo & Phân tích",
    "Retrieves high-level dashboard statistics.": "Lấy các số liệu thống kê tổng quan cho dashboard.",
    
    "ETR Processing": "Xử lý ETR",
    "Retrieves all course enrollments.": "Lấy danh sách tất cả các đăng ký khóa học (enrollments).",
    "Creates a new course enrollment for a learner.": "Tạo một đăng ký khóa học mới cho học viên.",
    "Retrieves a specific course enrollment by ID.": "Lấy thông tin một đăng ký khóa học cụ thể theo ID.",
    "Retrieves enrollments for a specific student.": "Lấy danh sách các đăng ký khóa học của một học viên cụ thể.",
    
    "Retrieves all ETR records.": "Lấy danh sách tất cả các hồ sơ ETR.",
    "Retrieves ETR records for the currently authenticated student.": "Lấy danh sách hồ sơ ETR của học viên hiện đang xác thực.",
    "Retrieves a specific ETR record by ID, including its Subject Results.": "Lấy thông tin một hồ sơ ETR cụ thể theo ID, bao gồm cả Kết quả Môn học.",
    "Submits an ETR for verification.": "Gửi một ETR để chờ xác minh (verification).",
    "Verifies a submitted ETR.": "Xác minh một ETR đã được gửi.",
    "Completes a verified ETR if all conditions are met.": "Hoàn tất một ETR đã được xác minh nếu đáp ứng đủ mọi điều kiện.",
    
    "Retrieves all uploaded evidence files.": "Lấy danh sách tất cả các tệp bằng chứng (evidence) đã tải lên.",
    "Uploads a new evidence file record.": "Tải lên một bản ghi tệp bằng chứng mới.",
    "Retrieves a specific evidence file by ID.": "Lấy thông tin một tệp bằng chứng cụ thể theo ID.",
    "Soft deletes an evidence file.": "Xóa mềm (soft delete) một tệp bằng chứng.",
    
    "Retrieves a specific export job by ID.": "Lấy thông tin một công việc xuất tệp (export job) cụ thể theo ID.",
    "Triggers an export job for a training package.": "Kích hoạt một công việc xuất tệp cho gói đào tạo (training package).",
    "Triggers an export job for a PDF report.": "Kích hoạt một công việc xuất tệp cho báo cáo PDF.",
    "Triggers an export job for a dashboard summary.": "Kích hoạt một công việc xuất tệp cho bản tóm tắt dashboard.",
    "Downloads the generated file of a completed export job.": "Tải xuống tệp đã được tạo từ một công việc xuất tệp hoàn tất.",
    
    "Retrieves summary reports for classes and ETRs.": "Lấy các báo cáo tổng hợp cho các lớp học và ETR.",
    
    "System Discovery": "Khám phá Hệ thống (System Discovery)",
    "Searches for classes by name.": "Tìm kiếm các lớp học theo tên.",
    "Searches for ETR records.": "Tìm kiếm các hồ sơ ETR.",
    
    "Retrieves all sessions.": "Lấy danh sách tất cả các buổi học (sessions).",
    "Creates a new session.": "Tạo một buổi học mới.",
    "Retrieves a session by ID.": "Lấy thông tin một buổi học theo ID.",
    "Updates an existing session.": "Cập nhật một buổi học hiện có.",
    "Deletes a session.": "Xóa một buổi học.",
    
    "Retrieves all subjects.": "Lấy danh sách tất cả các môn học.",
    "Creates a new subject.": "Tạo một môn học mới.",
    "Retrieves a subject by ID.": "Lấy thông tin một môn học theo ID.",
    "Updates an existing subject.": "Cập nhật một môn học hiện có.",
    "Soft deletes a subject.": "Xóa mềm (soft delete) một môn học.",
    
    "Retrieves all user profiles.": "Lấy danh sách tất cả các hồ sơ người dùng (user profiles).",
    "Retrieves all learner profiles.": "Lấy danh sách tất cả các hồ sơ học viên (learner profiles).",
    "Retrieves the profile of the currently authenticated user.": "Lấy hồ sơ của người dùng hiện đang xác thực.",
    "Updates the profile of the currently authenticated user.": "Cập nhật hồ sơ của người dùng hiện đang xác thực.",
    "Retrieves a user profile by account ID.": "Lấy hồ sơ người dùng theo ID tài khoản.",
    "Creates a new user profile for an account.": "Tạo một hồ sơ người dùng mới cho một tài khoản.",
    "Updates a specific user profile by account ID.": "Cập nhật một hồ sơ người dùng cụ thể theo ID tài khoản."
}

def process_directory(directory):
    for root, dirs, files in os.walk(directory):
        for file in files:
            if file.endswith('.cs'):
                file_path = os.path.join(root, file)
                with open(file_path, 'r', encoding='utf-8') as f:
                    content = f.read()
                
                new_content = content
                for eng, vie in translations.items():
                    new_content = new_content.replace(eng, vie)
                    
                if content != new_content:
                    with open(file_path, 'w', encoding='utf-8') as f:
                        f.write(new_content)
                    print(f"Updated {file_path}")

if __name__ == '__main__':
    controllers_dir = r"d:\Project\CapStone\ETR_Record_BE\ETR.API\Controllers"
    process_directory(controllers_dir)
