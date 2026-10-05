# KỊCH BẢN THUYẾT TRÌNH BẢO VỆ ĐỒ ÁN TỐT NGHIỆP CAPSTONE (SEP490)
## ĐỀ TÀI: HỆ THỐNG HỒ SƠ ĐÀO TẠO ĐIỆN TỬ TRONG NGÀNH HÀNG KHÔNG (ETR)
**Mã đề tài:** SU26SE104_GSU48 | **Đề tài:** Electronic Training Record (ETR) System in Aviation  
**Người thuyết trình:** Đại diện nhóm (Độc thoại toàn diện từ Slide 1 đến Slide 21 + Live Demo)  
**Thời lượng chuẩn:** 15 – 18 phút thuyết trình + Demo | 15 – 20 phút Vấn đáp Hội đồng (Q&A)

---

## ⏱️ BẢNG PHÂN BỔ THỜI GIAN THUYẾT TRÌNH (TỔNG: ~16 PHÚT)

| Phần / Slide | Nội dung chính | Thời lượng |
| :--- | :--- | :---: |
| **Phần 1: Mở đầu & Bối cảnh** (Slide 01 – 05) | Giới thiệu, Agenda, Bối cảnh Hàng không, Vấn đề cốt lõi & Giải pháp ETR | **3 phút 30s** |
| **Phần 2: Yêu cầu & Kiến trúc** (Slide 06 – 11) | 7 Stakeholders, 12 Use Cases, Phạm vi Scope, Kiến trúc Clean Arch & Tech Stack | **3 phút 30s** |
| **Phần 3: Trình diễn 6 Core Flows & Live Demo** (Slide 12 – 17) | 6 Luồng nghiệp vụ khép kín gắn liền với thao tác Live Demo trên hệ thống thực tế | **6 phút 30s** |
| **Phần 4: Đảm bảo Chất lượng & Đóng gói** (Slide 18 – 21) | Kiểm thử (235 Test Cases), Đóng góp nhóm, Giới hạn/Hướng phát triển & Kết luận | **2 phút 30s** |
| **Phần 5: Vấn đáp Hội đồng (Q&A)** | Trả lời phản biện, bảo vệ kiến trúc, bảo mật và nghiệp vụ | **15 – 20 phút** |

---

## 🎙️ LỜI THOẠI CHI TIẾT TỪNG SLIDE (SPEAKER SCRIPT)

---

### SLIDE 01: Title & Team Introduction (30 giây)
- **Hình ảnh trên slide:** Tên đề tài, Logo ETR, Thông tin Nhóm SU26SE104 & GVHD.
- **Lời thuyết trình:**
> "Kính thưa Thầy/Cô Chủ tịch và toàn thể Quý Thầy/Cô trong Hội đồng chấm đồ án tốt nghiệp Capstone SEP490.
> 
> Lời đầu tiên, em xin phép đại diện cho nhóm **SU26SE104** gửi lời chào trân trọng nhất đến Hội đồng. Hôm nay, em xin được trình bày và bảo vệ đề tài: **'Hệ thống Hồ sơ Đào tạo Điện tử trong Ngành Hàng không - Electronic Training Record (ETR) System in Aviation'**, dưới sự hướng dẫn của Giảng viên **Ngô Đặng Hà An**.
> 
> Đề tài được nghiên cứu và phát triển bởi 4 thành viên: Em - **Nguyễn Hữu Nhật Triều** (Team Leader, Backend), bạn **Trần Trọng Nhân** (Frontend Lead), bạn **Võ Trọng Nhân** (Frontend), và bạn **Đoàn Trọng Khôi** (Backend). Sau đây, em xin phép được bắt đầu phần trình bày của mình."

---

### SLIDE 02: Agenda (30 giây)
- **Hình ảnh trên slide:** 7 mục nội dung theo đúng trình tự đánh giá của Hội đồng.
- **Lời thuyết trình:**
> "Nội dung báo cáo hôm nay bám sát theo quy trình đánh giá chuẩn của Hội đồng, gồm 7 phần chính:
> 1. Bối cảnh & Quy chuẩn ngành Hàng không.
> 2. Thực trạng & Nỗi đau của phương pháp truyền thống.
> 3. Giải pháp toàn diện & Tính năng cốt lõi.
> 4. Đặc tả Yêu cầu Chức năng, 7 Nhóm tác nhân & 68 Use Cases.
> 5. Phạm vi dự án (In-Scope & Out-of-Scope).
> 6. Thiết kế Kiến trúc Hệ thống & Ngăn xếp Công nghệ.
> 7. Trình diễn 6 Luồng nghiệp vụ thực tế (Live Demo) và Báo cáo Kiểm thử chất lượng."

---

### SLIDE 03: Context / Background (45 giây)
- **Hình ảnh trên slide:** Quy chuẩn CAAV Part 10, ICAO Annex 1, Vòng đời đào tạo 3 giai đoạn (Ground -> SIM -> Flight), Yêu cầu lưu trữ 5-10 năm.
- **Lời thuyết trình:**
> "Kính thưa Hội đồng, ngành Hàng không dân dụng là ngành công nghiệp đòi hỏi sự an toàn và tính kỷ luật tuyệt đối. Các cơ quan quản lý như **CAAV (Cục Hàng không VN)**, **ICAO**, **FAA** hay **EASA** đều ban hành những khung pháp lý cực kỳ nghiêm ngặt (như CAAV Part 10, ICAO Annex 1).
> 
> Một phi công hay kỹ thuật viên hàng không phải trải qua vòng đời đào tạo liên tục 3 giai đoạn: **Lý thuyết mặt đất (Ground School)**, **Thực hành buồng lái mô phỏng (SIM)**, và **Huấn luyện bay thực tế (Flight Operations)**. 
> 
> Toàn bộ điểm danh, giờ bay tích lũy, bài thi, minh chứng và chữ ký xác nhận bắt buộc phải được lưu trữ bất biến từ **5 đến 10 năm** để phục vụ các đợt thanh tra an toàn đột xuất."

---

### SLIDE 04: Problem — Key Issues & Impact (45 giây)
- **Hình ảnh trên slide:** 4 vấn đề lớn: Hồ sơ giấy phân mảnh, Rủi ro gian lận/sửa điểm hồi tố, Thiếu quy trình Maker-Checker, Chi phí thanh tra khổng lồ.
- **Lời thuyết trình:**
> "Tuy nhiên, phần lớn các trung tâm huấn luyện bay hiện nay vẫn quản lý hồ sơ thông qua sổ sách giấy tờ, file PDF rời rạc và các bảng tính Excel phân tán. Thực trạng này dẫn đến 4 lỗ hổng nghiêm trọng:
> 1. **Dễ thất lạc & Gian lận dữ liệu:** Không có cơ chế phát hiện sửa điểm hồi tố (*Retroactive Tampering*).
> 2. **Vi phạm kiểm soát chất lượng:** Giảng viên vừa dạy, vừa nhập điểm và tự phê duyệt mà thiếu sự thẩm định độc lập từ bộ phận Đảm bảo Chất lượng (QA).
> 3. **Lãng phí nguồn lực:** Mỗi đợt thanh tra hàng không của CAAV, trung tâm mất hàng tuần để thu thập, kiểm đếm hàng nghìn trang hồ sơ giấy.
> 4. **Nguy cơ an toàn bay:** Không thể cảnh báo sớm các chứng chỉ/bài bay định kỳ sắp hết hạn (*Recurrent Expiry*), dẫn đến nguy cơ nhân sự bị cấm bay (*Grounding*)."

---

### SLIDE 05: Solution & Core Features (45 giây)
- **Hình ảnh trên slide:** Giá trị cốt lõi của ETR: Hệ sinh thái số minh bạch, Maker-Checker, Deep Freeze, Cổng thanh tra số hóa.
- **Lời thuyết trình:**
> "Hệ thống **ETR** ra đời nhằm giải quyết triệt để bài toán trên. Chúng em xây dựng một nền tảng quản lý hồ sơ đào tạo điện tử tập trung, minh bạch và có khả năng chống gian lận dữ liệu đạt chuẩn ICAO/CAAV.
> 
> Giá trị cốt lõi mà ETR mang lại bao gồm:
> - **Tự động hóa toàn bộ vòng đời:** Tự động sinh hồ sơ ETR Draft ngay khi học viên ghi danh vào lớp.
> - **Nguyên tắc Maker-Checker:** Tách biệt hoàn toàn vai trò người nhập minh chứng (Instructor) và người thẩm định (QA).
> - **Cơ chế Khóa bất biến (Deep Freeze):** Đóng băng vĩnh viễn hồ sơ ngay khi Trưởng ban đào tạo phê duyệt.
> - **Cổng Thanh tra Hàng không (Auditor Portal):** Hỗ trợ xuất trọn gói hồ sơ kèm mã băm SHA-256 chỉ trong 3 giây."

---

### SLIDE 06: Functional Requirements — Stakeholders & Roles (45 giây)
- **Hình ảnh trên slide:** 7 Roles phân tách theo nguyên tắc Separation of Duties (Admin, Academic Staff, Instructor, QA Staff, Training Manager, Auditor, Trainee).
- **Lời thuyết trình:**
> "Hệ thống ETR được thiết kế với **7 vai trò chuyên biệt**, thực thi nghiêm ngặt nguyên tắc **Phân tách Trách nhiệm (Separation of Duties - SoD)** ở tầng mã nguồn chứ không chỉ là quy ước:
> - **Admin:** Quản trị tài khoản, phân quyền RBAC và giám sát toàn bộ Audit Log.
> - **Academic Staff (Học vụ):** Quản lý khung giáo trình, mở lớp và điều phối ghi danh học viên.
> - **Instructor (Giảng viên):** Điểm danh theo buổi học, chấm điểm lý thuyết/kỹ năng, tải minh chứng bài bay.
> - **QA Staff (Đảm bảo chất lượng):** Thẩm định tính hợp lệ của minh chứng, duyệt hoặc trả hồ sơ về (Return).
> - **Training Manager (Trưởng ban đào tạo):** Thẩm định điều kiện tổng thể, phê duyệt cấp chứng chỉ và kích hoạt đóng băng hồ sơ.
> - **Compliance Auditor (Thanh tra viên):** Quyền chỉ đọc (Read-only), tra cứu hồ sơ và xuất gói thanh tra.
> - **Trainee (Học viên):** Theo dõi tiến độ học tập, chứng chỉ và nhật ký đào tạo cá nhân."

---

### SLIDE 07: Functional Requirements — Core Workflow & Use Cases (45 giây)
- **Hình ảnh trên slide:** 12 Macro Use Cases trải dài qua 4 nhóm nghiệp vụ: Academic Setup -> Training Operations -> Quality & Approval -> Auditing & Tracking.
- **Lời thuyết trình:**
> "Toàn bộ hệ thống vận hành theo **12 Macro Use Cases** khép kín:
> - **Nhóm 1 - Khởi tạo Đào tạo (UC-01 đến UC-03):** Thiết lập môn học, mở lớp và Import danh sách học viên hàng loạt bằng Excel.
> - **Nhóm 2 - Thực thi Huấn luyện (UC-04 đến UC-06):** Điểm danh với điều kiện chặn chuyên cần $\ge 80\%$, chấm điểm lý thuyết & kỹ năng, nộp file minh chứng kèm mã băm SHA-256.
> - **Nhóm 3 - Kiểm soát & Phê duyệt (UC-07 đến UC-09):** Giảng viên ký hoàn thành môn (Sign-off), QA thẩm định minh chứng, Training Manager duyệt cấp chứng chỉ.
> - **Nhóm 4 - Hậu kiểm & Thanh tra (UC-10 đến UC-12):** Quy trình sửa điểm sau phê duyệt (Amendment Workflow) và Truy vết nhật ký thanh tra (Audit Trail)."

---

### SLIDE 08: Main Functions (30 giây)
- **Hình ảnh trên slide:** Danh mục chức năng tổng quan (35 Features chính).
- **Lời thuyết trình:**
> "12 Macro Use Cases trên được hiện thực hóa thành **35 tính năng chính (F-01 đến F-35)**. Mỗi tính năng đều có cơ chế tiền điều kiện (*Pre-conditions*), luật nghiệp vụ chặn lỗi (*Business Invariants*) và cơ chế tự động ghi nhật ký kiểm toán (*Automated Audit Trail*)."

---

### SLIDE 09: Project Scope — In-Scope vs. Out-of-Scope (45 giây)
- **Hình ảnh trên slide:** Con số ấn tượng: 68/68 Use Cases hoàn thành (100%), 35 Features. Bảng ranh giới In-Scope / Out-of-Scope.
- **Lời thuyết trình:**
> "Về phạm vi dự án: Nhóm đã hiện thực hóa trọn vẹn **68/68 Use Cases** đã đăng ký trong tài liệu SRS, đạt tỉ lệ hoàn thành **100%**.
> 
> Để tập trung tối đa nguồn lực vào độ sâu tuân thủ hàng không, nhóm xác định rõ ranh giới:
> - **In-Scope:** Toàn bộ quy trình quản lý hồ sơ ETR, quy chế điểm danh, chấm điểm, thẩm định QA, khóa hồ sơ và xuất gói thanh tra.
> - **Out-of-Scope:** Hệ thống không kiêm nhiệm chức năng kế toán/thu học phí (ERP) hay điều phối lịch bay thực tế (FMS/Flight Dispatch) – những hệ thống này sẽ được tích hợp thông qua RESTful API trong tương lai."

---

### SLIDE 10: System Design / Architecture (45 giây)
- **Hình ảnh trên slide:** Sơ đồ Kiến trúc N-Tier / Clean Architecture, JWT Authentication, EF Core Interceptors, Cloudinary CDN, SQL Server.
- **Lời thuyết trình:**
> "Về thiết kế kỹ thuật, hệ thống áp dụng mô hình kiến trúc **N-Tier Clean Architecture**:
> - **Tầng Presentation:** Ứng dụng Single Page Application (React) tương tác mượt mà, hỗ trợ xác thực JWT và phân quyền giao diện theo Role.
> - **Tầng Application & Domain:** Chứa toàn bộ Business Rules độc lập, xử lý kiểm tra điều kiện tiên quyết và tính toán điểm.
> - **Tầng Infrastructure:** Sử dụng Entity Framework Core với cơ chế **EF Core SaveChanges Interceptor** – tự động bắt mọi giao dịch C-U-D để ghi log kiểm toán mà lập trình viên không phải viết thủ công.
> - **Tầng Lưu trữ:** Cơ sở dữ liệu quan hệ SQL Server chuẩn hóa đảm bảo tính toàn vẹn ACID, kết hợp Cloudinary CDN để lưu trữ file minh chứng."

---

### SLIDE 11: Technology Stack (30 giây)
- **Hình ảnh trên slide:** .NET 8 Web API, React 18 / Tailwind CSS, SQL Server, Cloudinary, MailKit, EPPlus.
- **Lời thuyết trình:**
> "Ngăn xếp công nghệ được lựa chọn cẩn trọng dựa trên độ ổn định và tính bảo mật doanh nghiệp:
> - **Backend:** ASP.NET Core 8 Web API – tối ưu hiệu năng, bảo mật cao và dễ mở rộng.
> - **Frontend:** React 18, Vite và Tailwind CSS – mang lại trải nghiệm người dùng nhanh, trực quan.
> - **Lưu trữ & Thư viện:** SQL Server, Cloudinary CDN, MailKit gửi thông báo tự động, và EPPlus xử lý Import/Export Excel tốc độ cao."

---

### SLIDE 12: Flow 1 — User & Access Management (Admin) (1 phút)
*(Chuyển tab trình duyệt thực hiện Live Demo Flow 1)*
- **Lời thuyết trình & Thao tác Demo:**
> *(Thao tác: Đăng nhập `admin@etr.com`)*
> "Sau đây, em xin phép trình diễn **Flow 1: Quản trị Hệ thống & Phân quyền**.
> 
> Đăng nhập với vai trò Administrator, màn hình hiển thị **Admin Dashboard** với tổng quan các tài khoản đang hoạt động.
> - Em mở chức năng **User Management**: Tại đây, Admin có thể tạo tài khoản mới với các ràng buộc nghiêm ngặt (Email hợp lệ, số điện thoại, gán đúng 1 trong 7 Roles). Hệ thống hỗ trợ tính năng **Import danh sách tài khoản hàng loạt qua file Excel**.
> - Tiếp theo là **System Audit Log**: Mọi thao tác thêm, sửa, xóa trong toàn hệ thống đều được tự động lưu lại với đầy đủ IP, Thời gian, Người thực hiện, Dữ liệu trước và sau khi thay đổi (`OldValue` / `NewValue`). Không ai có thể xóa hay chỉnh sửa nhật ký này."

---

### SLIDE 13: Flow 2 — Learner, Course & Enrollment (Academic Staff) (1 phút)
*(Chuyển tab trình duyệt thực hiện Live Demo Flow 2)*
- **Lời thuyết trình & Thao tác Demo:**
> *(Thao tác: Đăng nhập `academic@etr.com`)*
> "Tiếp theo là **Flow 2: Khởi tạo Đào tạo & Tự động sinh Hồ sơ ETR**.
> 
> Với vai trò Academic Staff (Cán bộ học vụ):
> - Học vụ quản lý khung giáo trình môn học (**Course & Syllabus**), thiết lập các môn lý thuyết và môn thực hành SIM/Flight.
> - Tại mục **Class Management**: Khi mở một lớp học mới và thực hiện ghi danh (*Enroll*) học viên, hệ thống sẽ **tự động sinh ra Hồ sơ ETR tương ứng ở trạng thái `Draft`**, kèm theo toàn bộ các `SubjectResult` theo đúng cấu trúc giáo trình.
> Điều này loại bỏ 100% rủi ro tạo thiếu hồ sơ hay nhầm lẫn môn học so với phương pháp thủ công."

---

### SLIDE 14: Flow 3 — Attendance & Assessment (Instructor) (1 phút 15s)
*(Chuyển tab trình duyệt thực hiện Live Demo Flow 3)*
- **Lời thuyết trình & Thao tác Demo:**
> *(Thao tác: Đăng nhập `instructor@etr.com`)*
> "Bước vào giai đoạn đào tạo, em chuyển sang vai trò **Instructor (Giảng viên)** với **Flow 3: Điểm danh, Chấm điểm & Nộp minh chứng**.
> - **Tính cô lập dữ liệu (Data Isolation):** Giảng viên chỉ nhìn thấy đúng các lớp và môn học mà mình được phân công giảng dạy.
> - **Điểm danh (Attendance):** Giảng viên thực hiện điểm danh buổi học theo chuẩn Nhị phân (`Present` hoặc `Absent`). Hệ thống tự động tính tỷ lệ chuyên cần. Nếu học viên có tỷ lệ chuyên cần $< 80.0\%$, hệ thống lập tức khóa quyền nhập điểm thi của học viên đó (*80% Gate Check*).
> - **Chấm điểm (Assessment):** Giảng viên nhập điểm lý thuyết (thang điểm 100) và đánh giá checklist kỹ năng thực hành (`Pass` / `Fail`).
> - **Nộp minh chứng (Evidence):** Giảng viên tải lên bản scan phiếu đánh giá (PDF/ảnh). Hệ thống tự động tạo mã băm SHA-256 và đưa minh chứng vào hàng đợi `Pending Verification` của QA."

---

### SLIDE 15: Flow 4 — Evidence & QA Verification (QA Staff) (1 phút)
*(Chuyển tab trình duyệt thực hiện Live Demo Flow 4)*
- **Lời thuyết trình & Thao tác Demo:**
> *(Thao tác: Đăng nhập `qa@etr.com`)*
> "Đây là điểm cốt lõi của tính tuân thủ hàng không – **Flow 4: Kiểm soát Chất lượng & Quy tắc Maker-Checker**.
> - Giảng viên nhập liệu (Maker) **không thể tự phê duyệt** minh chứng của chính mình.
> - Đăng nhập với tài khoản **QA Specialist (Checker)**: QA mở danh sách minh chứng đang chờ duyệt. QA kiểm tra file scan, đối chiếu điểm số và chữ ký của Giảng viên.
> - Nếu minh chứng hợp lệ, QA bấm **Approve** $\rightarrow$ Trạng thái chuyển sang `Verified`.
> - Nếu phát hiện sai sót, QA có thể bấm **Reject / Return for Correction**, ghi rõ lý do để Giảng viên bổ sung lại."

---

### SLIDE 16: Flow 5 — ETR Approval & Deep Freeze (Training Manager) (1 phút)
*(Chuyển tab trình duyệt thực hiện Live Demo Flow 5)*
- **Lời thuyết trình & Thao tác Demo:**
> *(Thao tác: Đăng nhập `manager@etr.com`)*
> "Khi tất cả các môn học đã hoàn tất và minh chứng đã được QA xác thực, chúng ta đến với **Flow 5: Phê duyệt & Đóng băng Hồ sơ (Deep Freeze)**.
> 
> Đăng nhập tài khoản **Training Manager (Trưởng ban đào tạo)**:
> - Hệ thống kích hoạt **Engine thẩm định tự động**: Kiểm tra học viên có đủ $\ge 80\%$ chuyên cần tất cả các môn không, điểm số có $\ge$ điểm chuẩn không, và 100% minh chứng đã được QA duyệt chưa.
> - Khi mọi điều kiện thỏa mãn, Training Manager thực hiện phê duyệt cuối cùng (**Sign-off & Approve**).
> - Ngay lập tức, hệ thống kích hoạt cơ chế **Deep Freeze**: Cấp mã chứng chỉ số, tính toán ngày hết hạn (*ExpiryDate*), và gán thuộc tính `IsLocked = true`. Toàn bộ hồ sơ trở thành **Chỉ Đọc (Read-Only)** vĩnh viễn, ngăn chặn mọi hành vi can thiệp sửa điểm hồi tố."

---

### SLIDE 17: Flow 6 — Reporting, Export & Audit (Auditor & Trainee) (1 phút)
*(Chuyển tab trình duyệt thực hiện Live Demo Flow 6)*
- **Lời thuyết trình & Thao tác Demo:**
> *(Thao tác: Đăng nhập `audit@etr.com` hoặc `student@etr.com`)*
> "Cuối cùng là **Flow 6: Cổng Thanh tra Hàng không & Theo dõi Tiến độ**.
> - Đăng nhập tài khoản **Compliance Auditor (Thanh tra CAAV/FAA)**: Thanh tra viên có quyền tra cứu toàn bộ hồ sơ đã đóng băng, xem bảng điểm tổng hợp (*Digital Transcript*), tải gói hồ sơ thanh tra trọn gói dạng PDF/ZIP có đính kèm mã SHA-256 để đối soát.
> - Đồng thời, **Học viên (Trainee)** có thể đăng nhập để tra cứu lịch sử đào tạo cá nhân và chứng chỉ điện tử của mình một cách minh bạch."

---

### SLIDE 18: Testing & Quality Assurance (45 giây)
- **Hình ảnh trên slide:** Tổng số 235 Test Cases (129 Unit Tests + 106 System/Integration Tests), Pass Rate 100.0%, 3 vòng thực thi kiểm thử.
- **Lời thuyết trình:**
> "Để đảm bảo hệ thống hoạt động ổn định và tin cậy tuyệt đối, nhóm đã triển khai chiến lược kiểm thử đa tầng:
> - Tổng cộng **235 Test Cases** được xây dựng và thực thi qua 3 vòng kiểm thử nghiêm ngặt.
> - Trong đó gồm **129 Unit Tests** (sử dụng xUnit và Moq) bao phủ toàn bộ Business Rules, và **106 System/Integration Tests** kiểm tra luồng nghiệp vụ E2E từ Frontend đến Backend.
> - Tỷ lệ vượt qua kiểm thử đạt **100.0% (235/235 Passed)**, không còn tồn đọng lỗi nghiêm trọng."

---

### SLIDE 19: Team Roles & Contribution (30 giây)
- **Hình ảnh trên slide:** Phân công trách nhiệm 4 thành viên, tỷ lệ đóng góp cân bằng, mô hình Agile/Scrum 1-2 tuần.
- **Lời thuyết trình:**
> "Dự án được hoàn thành nhờ sự phối hợp nhịp nhàng giữa 4 thành viên theo mô hình Agile/Scrum:
> - **Nguyễn Hữu Nhật Triều (Team Leader & Backend):** Thiết kế kiến trúc tổng thể, bảo mật RBAC, cơ chế Deep Freeze, Audit Interceptor và Core APIs.
> - **Trần Trọng Nhân (Frontend Lead):** Xây dựng UI/UX phân hệ Academic, Training Manager, Report & Dashboard.
> - **Võ Trọng Nhân (Frontend):** Xây dựng UI/UX phân hệ Giảng viên, QA Specialist và Attendance/Grading Modals.
> - **Đoàn Trọng Khôi (Backend):** Xây dựng module Import/Export Excel, dịch vụ Email tự động, Hash SHA-256 và bộ kiểm thử tự động xUnit."

---

### SLIDE 20: Limitations & Future Work (30 giây)
- **Hình ảnh trên slide:** Hạn chế hiện tại (Chưa có Offline mode hoàn chỉnh, Chưa tích hợp biometric/FMS) & Hướng phát triển tương lai.
- **Lời thuyết trình:**
> "Bên cạnh những kết quả đạt được, nhóm cũng thẳng thắn nhìn nhận các hạn chế hiện tại:
> 1. Hệ thống hiện hoạt động trên nền tảng Web trực tuyến, đòi hỏi kết nối mạng ổn định.
> 2. Dữ liệu lịch sử từ các hồ sơ giấy cũ cần được chuyển đổi thông qua quy trình Import có kiểm soát.
> 
> **Hướng phát triển tiếp theo:**
> - Phát triển ứng dụng Mobile Offline Mode hỗ trợ Giảng viên điểm danh ngay trên buồng lái/sân đỗ không có sóng Wi-Fi.
> - Tích hợp chữ ký số chuẩn PKI / Smart Card và mở rộng API kết nối trực tiếp với Hệ thống Điều hành Bay (FMS)."

---

### SLIDE 21: Conclusion & Q&A (30 giây)
- **Hình ảnh trên slide:** Tóm tắt thành quả (68 Use Cases, 35 Features, 7 Roles, 235 Test Cases), Lời cảm ơn và mở phiên Q&A.
- **Lời thuyết trình:**
> "Kính thưa Hội đồng, hệ thống ETR đã giải quyết trọn vẹn bài toán chuyển đổi số hồ sơ đào tạo phi công, tuân thủ nghiêm ngặt các quy chuẩn Hàng không quốc tế, đảm bảo tính toàn vẹn dữ liệu và sẵn sàng phục vụ thanh tra.
> 
> Em xin chân thành cảm ơn Quý Thầy/Cô trong Hội đồng đã chú ý lắng nghe phần trình bày của nhóm. Sau đây, nhóm chúng em rất mong nhận được những nhận xét, đóng góp quý báu và sẵn sàng trả lời các câu hỏi phản biện từ Quý Thầy/Cô. 
> 
> Em xin trân trọng cảm ơn!"

---

## 🎯 BỘ CÂU HỎI VẤN ĐÁP PHẢN BIỆN DỰ PHÒNG (DEFENSE Q&A CHEAT SHEET)

### Câu 1: "Làm thế nào hệ thống chống được việc Giảng viên hoặc Admin tự ý sửa điểm sau khi học viên đã tốt nghiệp?"
- **Trả lời:**
  > "Kính thưa Thầy/Cô, hệ thống áp dụng cơ chế bảo vệ 3 lớp:
  > 1. **Deep Freeze (`IsLocked = true`):** Khi Training Manager bấm Duyệt, cờ `IsLocked` được bật. Ở tầng Backend, mọi API cập nhật điểm, chuyên cần hay minh chứng đều kiểm tra cờ này và từ chối request với mã lỗi `400 BadRequest - Record is locked`.
  > 2. **Quy trình Amendment 4 bước:** Nếu có sai sót khách quan cần sửa, Giảng viên phải gửi Yêu cầu giải trình $\rightarrow$ Training Manager phê duyệt mở khóa tạm thời $\rightarrow$ Giảng viên sửa và nộp lại $\rightarrow$ Khóa lại.
  > 3. **Audit Trail tự động:** Mọi giá trị điểm cũ và điểm mới đều được EF Core Interceptor ghi vào bảng `AuditLogs` vĩnh viễn, không thể xóa."

### Câu 2: "Tại sao điểm danh chỉ có `Present` và `Absent` mà không có trạng thái `Late` (Đi muộn)?"
- **Trả lời:**
  > "Kính thưa Thầy/Cô, đây là đặc thù nghiêm ngặt của Ngành Hàng không theo chuẩn CAAV Part 10 và ICAO. 
  > Một buổi học lý thuyết bay hay một sortie bay huấn luyện không chấp nhận việc tham gia một nửa. Học viên bắt buộc phải có mặt trọn vẹn buổi học để được tính chuyên cần (`Present`), nếu không tham gia đầy đủ thì tính là `Absent` và bắt buộc phải bay/học bù (*Make-up Session*). Việc dùng điểm danh Nhị phân (`Present` / `Absent`) phản ánh đúng 100% bản chất an toàn hàng không."

### Câu 3: "Mã băm SHA-256 của file minh chứng giải quyết vấn đề gì?"
- **Trả lời:**
  > "Kính thưa Thầy/Cô, khi Giảng viên tải file minh chứng lên Cloudinary, Backend sẽ đọc luồng byte của file và tính ra chuỗi SHA-256 băm lưu vào Database.
  > Nếu có ai đó can thiệp trực tiếp trên Cloudinary để tráo đổi nội dung file scan, khi xuất hồ sơ thanh tra, hệ thống đối soát mã SHA-256 của file hiện tại với mã lưu trong Database sẽ phát hiện ngay sự sai lệch, đảm bảo tính toàn vẹn chứng cứ (*Evidence Integrity*)."

### Câu 4: "Tại sao nhóm chọn kiến trúc N-Tier / Clean Architecture thay vì Microservices?"
- **Trả lời:**
  > "Kính thưa Thầy/Cô, hệ thống ETR là hệ thống quản lý hồ sơ nghiệp vụ cốt lõi, trong đó các thực thể (Khóa học, Môn học, Điểm danh, Kết quả đánh giá, Chứng chỉ) có mối quan hệ ràng buộc toàn vẹn rất chặt chẽ và yêu cầu giao dịch nguyên tử (**ACID Transactions**) cao. 
  > Kiến trúc Clean Architecture Monolith phân tách rõ ràng Domain/Application/Infrastructure giúp tối ưu hóa hiệu năng, giảm độ trễ mạng, đơn giản hóa việc duy trì tính nhất quán của dữ liệu và dễ dàng kiểm toán hơn rất nhiều so với mô hình phân tán Microservices."
