Instruc thì có gắn liền với class r với cái lúc mà chốt điểm ak thì có cái signoff (kiểu ai chốt điểm thì người đó signoff xác nhận) giờ muốn mở lại thì phải do chính người signoff đó mở thì mới dc,  còn theo nghiệp vụ thực tế thì QA dc phép quản toàn bộ evidence chứ ko giới hạn theo course với class

Vì cái audit có truy vết xem ai là người duyệt nên ko cần phải chia rõ ra ông qa nào quản course nào

Đối với Role Instructor (Giảng viên):
​Luật: Chỉ người nào đã đặt bút ký thì người đó mới có quyền làm đơn xin rút lại chữ ký.
​Code BE: Khi API POST /api/SubjectSignoff/{id}/unlock-request được gọi, BE phải kiểm tra xem ID của người đang gửi request (lấy từ Token) có khớp với cột SignedByAccountId trong Database hay không. Nếu không khớp -> Văng lỗi 403 (Không có quyền can thiệp vào chữ ký của người khác).
​Đối với Role Academic (Giáo vụ):
​Luật: Academic TUYỆT ĐỐI KHÔNG có quyền xin mở khóa chữ ký của Instructor. Họ là người thu thập hồ sơ, không phải người chịu trách nhiệm chuyên môn. Nếu hồ sơ sai, Academic chỉ có quyền "Bấm nút từ chối" (Reject) để đẩy hồ sơ ngược lại cho Instructor tự xử lý, chứ không được gọi thẳng API Unlock.
​Đối với Role Admin / Super User:
​Luật ngoại lệ: Chỉ dùng khi Instructor gốc đã nghỉ việc, qua đời, hoặc mất hành vi dân sự. Admin có quyền "Force Unlock" (Mở khóa cưỡng chế).
​Code BE: Nếu Role là Admin gọi, BE cho qua. Tuy nhiên, hành động này phải kích hoạt cơ chế ghi Log Audit cấp độ cao nhất (Cảnh báo đỏ) ghi rõ "Admin can thiệp phá vỡ chữ ký".


nguyên tắc "Cô lập dữ liệu theo phân quyền" (Data Isolation / Resource-based Authorization) – một trong những luật bất thành văn của các hệ thống quản lý giáo dục và đặc biệt khắt khe trong ngành Hàng không.


​1. Luật "Sân nhà ai nấy đá" (Data Visibility)
​Giảng viên (Instructor) khi đăng nhập vào hệ thống thì tài khoản của họ bị "khóa chặt" vào những lớp học mà Giáo vụ (Academic) đã gán cho họ.
​Trên UI (FE): Khi Giảng viên mở màn hình "Lớp học của tôi", họ CHỈ được nhìn thấy những Lớp có InstructorAccountId trùng với ID của chính họ. (Họ không được phép thấy lớp của Giảng viên khác để tránh rò rỉ thông tin cá nhân của học viên).
​Dưới BE: API GET /api/Classes khi nhận Token của Role Instructor, bắt buộc BE phải âm thầm thêm một đoạn filter: .Where(x => x.InstructorAccountId == currentUserId).
​2. Luật "Cầm cân nảy mực đúng chỗ" (Action Authorization)
​Không chỉ giới hạn ở việc xem (Read), mà ranh giới này còn phải siết chặt ở các hành động ghi/sửa (Write/Update).
​Giả sử Giảng viên A (ID = 2) biết được ID lớp học của Giảng viên B (ID = 5) và cố tình dùng Postman gọi API chấm điểm cho lớp đó.
​BE gánh team (Validation): Khi có bất kỳ request Điểm danh (POST /api/Attendance), Nhập điểm, hay Upload Evidence nào gửi lên, BE phải check ngay: "Cái người đang gọi API này có phải là Giảng viên được phân công của cái Lớp này không?". Nếu không phải -> Đá văng ra kèm mã lỗi 403 Forbidden (Bạn không được phân công giảng dạy lớp này).
​3. Có ngoại lệ nào cho Instructor không? (Exceptions)
​Trong thực tế vận hành hàng không, có một case mà team ông cần lưu ý (nếu muốn làm hệ thống điểm 10): Dạy thay / Chấm thi chéo.
​Tình huống: Giảng viên chính (ID = 2) bị ốm, Giáo vụ điều Giảng viên phụ (ID = 9) vào dạy thay 1 buổi và điểm danh. Hoặc quy định bắt buộc người chấm thi thực hành phải là một Giảng viên độc lập (không phải người dạy).
​Cách xử lý (Nếu hệ thống có làm): Lúc này, cái cột InstructorAccountId trong bảng Class chỉ là Giảng viên chủ nhiệm. DB sẽ cần thêm một bảng trung gian (VD: ClassInstructors_Mapping hoặc phân công theo từng Session) để cho phép 1 lớp có nhiều hơn 1 Giảng viên được cấp quyền truy cập.
​(cái này thì chắc là tính năng phụ đi, kịp thì mình bổ sung ko thì cứ lock là 1 class 1 instruc thôi)
