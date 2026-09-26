# KẾ HOẠCH DEMO & KỊCH BẢN BẢO VỆ ĐỒ ÁN TỐT NGHIỆP CAPSTONE PROJECT (SWP490)
## HỆ THỐNG QUẢN LÝ HỒ SƠ HUẤN LUYỆN PHI CÔNG ĐIỆN TỬ (ETR)
**Mã đề tài:** SU26SE104_GSU48 | **Nhóm:** SU2026-GSU-ETR  
**Căn cứ chuẩn:** Syllabus SWP490 ID 4901 (QĐ số 1341/QĐ-ĐHFPT), Biểu mẫu FPTU 07.20a & Quy chuẩn Hàng không (VAR Part 10 / ICAO Doc 9868)  
**Giảng viên hướng dẫn:** Ngô Đặng Hà An | **Học kỳ:** Summer 2026  

---

## I. TỔNG QUAN CHIẾN LƯỢC & PHÂN BỔ THỜI GIAN BUỔI BẢO VỆ

Theo quy chế SWP490 FPTU, buổi bảo vệ 1.1 kéo dài **45 – 60 phút**. Nhóm tuyệt đối không để cháy giờ hoặc để một người "gánh" toàn bộ phần nói (vi phạm tiêu chí TC8 & TC9).

### 1.1. Khung thời gian tiêu chuẩn
- **Phần 1: Thuyết trình Slide (15 – 18 phút):** Trình bày từ Slide 01 đến 19 (bài toán, kiến trúc, công nghệ, 6 flows nghiệp vụ, chất lượng kiểm thử, phân công và hạn chế).
- **Phần 2: Trình diễn Live Demo trực tiếp (15 – 18 phút):** Trình diễn chuỗi nghiệp vụ khép kín từ lúc mở lớp đến khi đóng băng hồ sơ và xuất gói thanh tra.
- **Phần 3: Vấn đáp Hội đồng & Soi mã nguồn (15 – 20 phút):** Trả lời câu hỏi phản biện, chạy use case ngoài kịch bản và mở trực tiếp code trong IDE để giải thích.

### 1.2. Phân công vai trò của 4 thành viên trong buổi Demo
| Thành viên | Mã SV | Vai trò kỹ thuật | Nhiệm vụ chính trong Demo & Bảo vệ |
| :--- | :---: | :--- | :--- |
| **Nguyễn Hữu Nhật Triều** | SE170486 | Leader, Backend Core & Security | • Trình bày tổng quan kiến trúc, điều phối chuyển đổi luồng.<br>• Phụ trách **Flow 1 (Admin RBAC)** và cơ chế bảo mật / Audit Trail.<br>• Trực tiếp điều phối máy chiếu hoặc thao tác chuyển vai trò. |
| **Trần Trọng Nhân** | SE171973 | Frontend, Academic & TM Lead | • Thuyết minh & demo **Flow 2 (Academic Staff)**: Mở lớp, import học viên & tự động sinh ETR Draft.<br>• Thuyết minh & demo **Flow 5 (Training Manager)**: Thẩm định điều kiện và phê duyệt **Deep Freeze**.<br>• Phụ trách giải thích các component UI của ban đào tạo. |
| **Võ Trọng Nhân** | SE171411 | Frontend, Instructor & QA Lead | • Thuyết minh & demo **Flow 3 (Instructor)**: Điểm danh buổi bay/sim, chấm điểm lý thuyết & kỹ năng, upload minh chứng.<br>• Thuyết minh & demo **Flow 4 (QA Staff)**: Thẩm định minh chứng, kiểm tra quy tắc Maker-Checker, trả hồ sơ (Return for Correction). |
| **Đoàn Trọng Khôi** | SE141053 | Backend, Automation & Testing | • Thuyết minh & demo **Flow 6 (Auditor / Student)**: Cổng thanh tra hàng không (Auditor Portal), xuất gói hồ sơ thanh tra (PDF/ZIP).<br>• Phụ trách giải thích Unit Test (xUnit/Moq), Transaction Rollback và quản lý máy backup. |

---

## II. THIẾT LẬP MÔI TRƯỜNG & DANH SÁCH TÀI KHOẢN TEST

Toàn bộ dữ liệu được nạp từ tệp cơ sở dữ liệu mẫu `Deploy_Demo_ETRManagementDB.sql` (chứa 64 tài khoản, 109 ETR, 626 buổi học, 506 kết quả đánh giá, 5,003 nhật ký kiểm toán), đảm bảo màn hình luôn có số liệu thực tế, không bị rỗng.

### 2.1. Danh sách 7 tài khoản test chuẩn hóa (Mật khẩu chung: `123456`)
| STT | Vai trò (Role) | Mã người dùng | Email / Username | Mật khẩu | Phạm vi trách nhiệm & Mục đích Demo |
| :-: | :--- | :---: | :--- | :---: | :--- |
| **1** | **Administrator** | `ADM-01` | `admin@etr.com` | `123456` | Quản trị người dùng, phân quyền RBAC, xem toàn bộ Audit Log, xử lý mở khóa ngoại lệ (Break-Glass Unlock). |
| **2** | **Academic Staff** | `ACA-01` | `academic@etr.com` | `123456` | Quản lý khung đào tạo, mở lớp bay/sim, ghi danh học viên, kiểm tra ETR sinh tự động ở trạng thái `Draft` và nộp lên QA. |
| **3** | **Senior Flight Instructor** | `INS-01` | `instructor@etr.com` | `123456` | Giáo viên bay: Điểm danh buổi bay, nhập điểm lý thuyết / SIM, đánh giá checklist kỹ năng, upload minh chứng bài bay. |
| **4** | **QA Specialist** | `QA-01` | `qa@etr.com` | `123456` | Kiểm soát chất lượng: Duyệt minh chứng (tuân thủ Maker-Checker), review ETR dossier, trả hồ sơ yêu cầu sửa (Return for Correction). |
| **5** | **Training Manager** | `MGR-01` | `manager@etr.com` | `123456` | Trưởng ban đào tạo: Thẩm định hồ sơ hoàn tất, phê duyệt cuối cùng và kích hoạt khóa vĩnh viễn (**Deep Freeze**). |
| **6** | **Compliance Auditor** | `AUD-01` | `audit@etr.com` | `123456` | Thanh tra Hàng không (CAAV/FAA/EASA): Quyền chỉ đọc (Read-only), tra cứu ETR đã khóa, xuất báo cáo trọn gói ZIP/PDF. |
| **7** | **Student / Trainee** | `STU-01` | `student@etr.com` | `123456` | Học viên phi công: Xem tiến độ học tập cá nhân, chứng chỉ đã cấp, nhật ký tích lũy giờ bay. |

### 2.2. Phương án kỹ thuật & Dự phòng (Backup Protocol)
1. **Chuẩn bị 2 Profile trình duyệt:**
   - *Profile 1 (Cửa sổ thường):* Đăng nhập vai trò Nhập liệu/Giảng viên/Học vụ (`academic@etr.com` hoặc `instructor@etr.com`).
   - *Profile 2 (Cửa sổ Incognito):* Đăng nhập vai trò Kiểm soát/Phê duyệt (`qa@etr.com` hoặc `manager@etr.com`).
   *(Chuyển tab ngay lập tức khi đổi vai trò, không tốn thời gian gõ username/password trước mặt hội đồng)*.
2. **Máy tính dự phòng:**
   - Luôn có 1 máy thứ 2 (của Khôi hoặc Triều) đã clone mã nguồn, chạy sẵn BE/FE và restore sẵn database. Nếu máy chính gặp sự cố kết nối, cắm dây HDMI sang máy 2 trong vòng 30 giây.
   - File SQL `Deploy_Demo_ETRManagementDB.sql` lưu sẵn ở Desktop để phục hồi DB trong 1 phút nếu có thao tác nhầm.

---

## III. KỊCH BẢN DEMO CHÍNH — CHUỖI NGHIỆP VỤ KHÉP KÍN (GOLDEN HAPPY PATH)
*(Thời lượng: 14 – 16 phút — Trực tiếp bám sát 6 Flows trên Slide 10–15 và 12 Workflows trong Báo cáo Report 7)*

```
[Academic Staff: Mở lớp & Auto sinh ETR] 
       ↓ 
[Instructor: Điểm danh & Chấm điểm bài bay] 
       ↓ 
[QA Staff: Duyệt minh chứng & Thẩm định hồ sơ] 
       ↓ 
[Training Manager: Phê duyệt & Kích hoạt Deep Freeze] 
       ↓ 
[Auditor / Student: Tra cứu & Xuất gói Audit Package]
```

### Chặng 1: Quản trị hệ thống & Giám sát phân quyền (Flow 1 - 2 phút)
- **Người trình bày:** Nguyễn Hữu Nhật Triều
- **Tài khoản:** `admin@etr.com`
- **Thao tác:**
  1. Đăng nhập vào hệ thống $\rightarrow$ Mở **Admin Dashboard** (tổng quan tài khoản hoạt động, phân bổ vai trò, hoạt động hệ thống).
  2. Mở **User Management**: Trình chiếu danh sách tài khoản, cơ chế phân quyền RBAC và trạng thái kích hoạt.
  3. Mở **System Audit Log**: Chỉ cho hội đồng thấy mọi thao tác thêm, sửa, xóa trên hệ thống đều được tự động ghi nhận với IP, User, thời gian và dữ liệu JSON `OldValue` / `NewValue`.
- **Điểm nhấn nghiệp vụ:** Chứng minh hệ thống đáp ứng tiêu chuẩn an ninh hàng không: Phân quyền theo vai trò (Role-Based Access Control) và không có bất kỳ hành động nào bị lọt khỏi nhật ký kiểm toán.

### Chặng 2: Khởi tạo đào tạo & Tự động sinh hồ sơ ETR (Flow 2 - 3 phút)
- **Người trình bày:** Trần Trọng Nhân
- **Tài khoản:** `academic@etr.com`
- **Thao tác:**
  1. Đăng nhập tài khoản Học vụ $\rightarrow$ Mở **Academic Dashboard** (theo dõi tiến độ đào tạo các khóa phi công).
  2. Vào **Class Management**: Chọn lớp bay A320 Sim Cohort.
  3. Mở danh sách học viên: Trình chiếu tính năng khi học viên được ghi danh vào lớp, hệ thống **tự động sinh hồ sơ ETR tương ứng ở trạng thái `Draft`**.
  4. Mở chi tiết 1 ETR ở trạng thái `Draft`: Trình chiếu danh sách môn học (Subjects) và cấu trúc điểm (Passing Threshold $\ge 80\%$) đã được bind sẵn theo khung giáo trình được phê duyệt, loại bỏ nguy cơ thiếu sót hồ sơ so với quản lý giấy.

### Chặng 3: Ghi nhận chuyên cần, chấm điểm & Nộp minh chứng bài bay (Flow 3 - 3 phút)
- **Người trình bày:** Võ Trọng Nhân
- **Tài khoản:** `instructor@etr.com`
- **Thao tác:**
  1. Đăng nhập tài khoản Giảng viên $\rightarrow$ Trình chiếu **Data Isolation**: Giảng viên chỉ thấy đúng các lớp và môn học mình được phân công giảng dạy.
  2. Vào **Attendance Recording**: Mở buổi bay hôm nay, đánh dấu chuyên cần cho học viên (`Present`). Hệ thống tự động tính toán lại Attendance Rate.
  3. Vào **Assessment Recording**: Nhập điểm bài thi lý thuyết quy trình khẩn cấp (`88/100`), đánh giá kỹ năng hạ cánh trong điều kiện gió cạnh (`Pass`).
  4. Vào **Evidence Upload**: Tải lên bản scan biên bản đánh giá bài bay (file PDF/ảnh). Hệ thống tự động băm mã toàn vẹn SHA-256 và lưu trữ trạng thái `Pending Verification`.
  5. Giảng viên ký xác nhận hoàn thành môn học (**Sign-off Subject**).

### Chặng 4: Kiểm soát chất lượng & Thực thi nguyên tắc Maker-Checker (Flow 4 - 3 phút)
- **Người trình bày:** Võ Trọng Nhân & Nguyễn Hữu Nhật Triều
- **Tài khoản:** `qa@etr.com`
- **Thao tác:**
  1. Chuyển sang Profile trình duyệt thứ 2, đăng nhập vai trò QA $\rightarrow$ Mở **Evidence Verification Queue**.
  2. Mở file minh chứng vừa được giáo viên upload ở Chặng 3.
  3. Nhấn nút **Approve Evidence**: Minh chứng chuyển trạng thái sang `Verified`.
  4. Mở **ETR Review Queue**: Mở hồ sơ ETR vừa được giáo viên nộp.
  5. QA kiểm tra tính hợp lệ: Tỷ lệ chuyên cần đủ điều kiện ($\ge 90\%$), các bài thi đều đạt điểm Pass, minh chứng đã được thẩm định đầy đủ.
  6. QA ký xác nhận duyệt hồ sơ $\rightarrow$ ETR chính thức chuyển từ trạng thái `Submitted` sang **`Verified`**, đủ điều kiện chuyển lên Ban Giám đốc Đào tạo.
- **Điểm nhấn nghiệp vụ:** Tuân thủ nguyên tắc độc lập **Maker-Checker** (Người tạo hồ sơ không thể tự duyệt hồ sơ của mình).

### Chặng 5: Thẩm định tối cao & Kích hoạt cơ chế Đóng băng hồ sơ (Deep Freeze) (Flow 5 - 3 phút)
- **Người trình bày:** Trần Trọng Nhân
- **Tài khoản:** `manager@etr.com`
- **Thao tác:**
  1. Đăng nhập tài khoản Training Manager $\rightarrow$ Mở **ETR Final Approval**.
  2. Mở hồ sơ ETR đang ở trạng thái `Verified`.
  3. Hệ thống kích hoạt **Pre-validation Engine**: Tự động kiểm tra tất cả điều kiện tiên quyết (chuyên cần, điểm số, minh chứng có hợp lệ không).
  4. Nhấn **Final Approve & Sign-off**:
     - Trạng thái ETR chuyển sang **`Completed`**.
     - Cờ **`IsLocked = true` (Deep Freeze)** được kích hoạt.
     - Hệ thống tự động sinh số hiệu chứng chỉ và thời hạn hiệu lực theo quy định hàng không.
  5. **Chứng minh tính bất biến ngay trên UI:** Thử bấm vào các nút sửa điểm hoặc sửa chuyên cần $\rightarrow$ Toàn bộ giao diện bị vô hiệu hóa (Read-only), ngăn chặn tuyệt đối mọi hành vi sửa đổi dữ liệu sau khi đã phê duyệt.

### Chặng 6: Cổng thanh tra hàng không & Cổng học viên (Flow 6 - 2 phút)
- **Người trình bày:** Đoàn Trọng Khôi
- **Tài khoản:** `audit@etr.com` & `student@etr.com`
- **Thao tác:**
  1. **Auditor Portal (`audit@etr.com`):**
     - Đăng nhập giao diện dành riêng cho đoàn thanh tra Cục Hàng không (CAAV/FAA/EASA).
     - Dùng **Advanced Search** lọc nhanh hồ sơ học viên theo mã phi công hoặc khóa học.
     - Mở hồ sơ ETR đã bị đóng băng $\rightarrow$ Nhấn nút **Export Training Package**.
     - Hệ thống tự động biên dịch toàn bộ dữ liệu, điểm số, lịch sử điểm danh và tệp minh chứng đính kèm thành **1 file ZIP / PDF dossier hoàn chỉnh** trong vài giây (thay vì mất 3–5 ngày lục tìm hồ sơ giấy như quy trình cũ).
  2. **Student Portal (`student@etr.com`):**
     - Đăng nhập tài khoản học viên: Học viên thấy ngay chứng chỉ bay hợp lệ của mình, xem lại toàn bộ lộ trình huấn luyện và số giờ bay tích lũy một cách minh bạch.

---

## IV. KỊCH BẢN BẪY LỖI & LUỒNG NGOẠI LỆ (ĐỐI PHÓ HỘI ĐỒNG PHẢN BIỆN)
*(Thời lượng: 3 – 5 phút — Chuẩn bị sẵn để ghi điểm tuyệt đối ở Tiêu chí TC7 và TC9)*

Hội đồng phản biện FPTU luôn yêu cầu: *"Em hãy làm thử trường hợp nhập sai, hoặc cố tình làm trái quyền xem hệ thống xử lý thế nào"*. Nhóm chủ động thực hiện 4 tình huống sau:

### Tình huống 1: Bẫy kiểm thử nguyên tắc Maker-Checker (Thẩm quyền chéo)
- **Câu hỏi phản biện:** *"Nếu Giảng viên tự upload minh chứng rồi tự vào duyệt minh chứng của chính mình thì sao?"*
- **Thao tác demo:**
  - Lấy tài khoản `instructor@etr.com` cố tình truy cập vào URL duyệt minh chứng của QA (`/qa/evidence-verification`) hoặc gửi lệnh duyệt qua API.
  - **Kết quả:** Hệ thống lập tức chặn đứng với mã lỗi **403 Forbidden - Unauthorized**, chứng minh việc kiểm soát quyền được thực thi ở tầng API Controller Middleware, không phải chỉ ẩn nút trên giao diện.

### Tình huống 2: Bẫy kiểm thử ràng buộc dữ liệu (Validation Constraints)
- **Câu hỏi phản biện:** *"Nếu người dùng nhập điểm âm hoặc điểm 150 thì hệ thống xử lý thế nào?"*
- **Thao tác demo:**
  - Tại màn hình nhập điểm, cố tình nhập số `105` hoặc ký tự chữ.
  - **Kết quả:** Giao diện lập tức báo đỏ với tooltip validation; nếu cố tình bypass qua Postman, tầng Domain Service và FluentValidation của Backend sẽ chặn lại với mã lỗi **400 Bad Request** kèm thông báo lỗi chuẩn xác.

### Tình huống 3: Luồng trả hồ sơ yêu cầu bổ sung (Return for Correction)
- **Câu hỏi phản biện:** *"Nếu QA phát hiện minh chứng bị mờ hoặc học viên chưa đủ điều kiện thì xử lý ra sao?"*
- **Thao tác demo:**
  - Đăng nhập `qa@etr.com`, chọn 1 ETR chưa đạt, nhấn nút **Return for Correction**.
  - Cố tình không nhập lý do $\rightarrow$ Hệ thống chặn lại, bắt buộc nhập lý do tối thiểu 10 ký tự (theo **Business Rule BR-048**).
  - Nhập lý do: *"Bản scan bài bay SIM mờ số hiệu, yêu cầu giáo viên nộp lại"* $\rightarrow$ ETR quay về trạng thái cho phép giáo viên chỉnh sửa, đồng thời gửi thông báo tới Notification Center.

### Tình huống 4: Cơ chế mở khóa ngoại lệ khẩn cấp (Break-Glass Unlock)
- **Câu hỏi phản biện:** *"Hồ sơ đã Deep Freeze rồi nhưng sau này có quyết định phúc khảo của Cục Hàng không thì làm thế nào?"*
- **Thao tác demo:**
  - Đăng nhập `admin@etr.com` (chỉ duy nhất Admin cấp cao nhất có quyền mở khóa).
  - Thực hiện chức năng **Request Unlock / Amend ETR**: Bắt buộc nhập số công văn phê duyệt phúc khảo và lý do mở khóa.
  - Mở bảng **Audit Logs**: Chỉ cho hội đồng thấy hành vi mở khóa này đã được ghi vĩnh viễn vào nhật ký kiểm toán với `ActionType = 'UNLOCK'`, ghi nhận chính xác thời gian, IP và người thực hiện. Dữ liệu gốc trước khi sửa vẫn được bảo lưu nguyên vẹn trong trường `OldValue`.

---

## V. CHUẨN BỊ PHẢN BIỆN MÃ NGUỒN (CODE WALKTHROUGH TRONG IDE)
*(Đáp ứng điều kiện bắt buộc A1 và Tiêu chí TC9: "Mỗi thành viên phải giải thích được phần việc của mình ở mức mã nguồn")*

| Thành viên | Tệp mã nguồn trọng tâm trong IDE | Kiến trúc & Cơ chế kỹ thuật cần giải thích |
| :--- | :--- | :--- |
| **Nguyễn Hữu Nhật Triều** | • `ETR.Infrastructure/Data/AppDbContext.Compliance.cs`<br>• `ETR.API/Middleware/JwtMiddleware.cs`<br>• `ETR.Domain/Entities/Account.cs` & `AuditLog.cs` | **Cơ chế Audit Trail tự động & Khóa bất biến:**<br>• Giải thích cách EF Core Interceptor tự động bắt các sự kiện `SaveChangesAsync()` để serialize `OldValue` và `NewValue` sang JSON lưu vào bảng `AuditLogs`.<br>• Giải thích cơ chế bảo mật JWT Claims và cách ngăn chặn can thiệp dữ liệu khi `IsLocked == true`. |
| **Đoàn Trọng Khôi** | • `ETR.Application/Services/EtrService.cs`<br>• `ETR.Application.Tests/Services/AssessmentTests.cs`<br>• `BackgroundJobs/CertificateExpiryJob.cs` | **Xử lý nghiệp vụ Backend & Unit Testing:**<br>• Giải thích cấu trúc Clean Architecture, tách biệt Domain - Application - Infrastructure.<br>• Giải thích cách viết Unit Test với mô hình AAA (Arrange-Act-Assert) cho 129 test cases trên xUnit và Moq.<br>• Giải thích Transaction Rollback đảm bảo tính toàn vẹn dữ liệu khi tạo ETR kèm các môn học con. |
| **Võ Trọng Nhân** | • `src/Instructor/AttendanceRecording.jsx`<br>• `src/QA/QARETRReviewQueue.jsx`<br>• `src/utils/cloudinary.js` & `evidenceFiles.js` | **Kiến trúc giao diện & Tích hợp Upload Minh chứng:**<br>• Giải thích cách phân trang bất đồng bộ và bộ lọc tìm kiếm hồ sơ trong hàng đợi thẩm định của QA.<br>• Giải thích pipeline upload tệp an toàn lên Cloudinary và thuật toán băm SHA-256 chống tráo đổi file sau khi duyệt. |
| **Trần Trọng Nhân** | • `src/TrainingManager/EtrApproval.jsx`<br>• `src/Academic/AcademicClassManagement.jsx`<br>• `src/test/AllFlowsIntegration.test.jsx` | **Quản trị State Frontend & Xuất dữ liệu báo cáo:**<br>• Giải thích cách quản lý State động giữa các bước duyệt hồ sơ của Ban Đào tạo.<br>• Giải thích cơ chế xuất gói tài liệu kiểm toán (PDF Dossier và ZIP package) trực tiếp từ frontend kết hợp API backend.<br>• Trình diễn kết quả chạy integration test bằng Vitest. |

---

## VI. BẢNG CHECKLIST TỰ ĐÁNH GIÁ THEO HƯỚNG DẪN FPTU 1.1

| Tiêu chuẩn FPTU | Yêu cầu Syllabus SWP490 | Kết quả thực tế của Nhóm ETR | Đánh giá | Minh chứng đối soát |
| :--- | :--- | :--- | :---: | :--- |
| **Điều kiện A1** | Liêm chính học thuật | 100% nhóm tự xây dựng | **ĐẠT** | Lịch sử commit đều đặn trên GitHub từ tuần 1 đến tuần 15 của 4 thành viên. |
| **Điều kiện A2** | Demo Use Case hoàn chỉnh | $\ge 1$ luồng end-to-end | **ĐẠT** | Demo liên hoàn trơn tru qua 6 chặng từ Mở lớp đến Đóng băng và Xuất gói Audit. |
| **Điều kiện A3** | Điểm quá trình (OGA) | Không có OGA nào $< 2/10$ | **ĐẠT** | Cả 7 đầu báo cáo đều được Giảng viên hướng dẫn thông qua. |
| **Điều kiện B1** | Tỷ lệ hoàn thành chức năng | $\ge 75\%$ Use Case ở Report 3 | **ĐẠT (100%)** | 68/68 Use Cases (UC-01 đến UC-68) đã hoàn thiện và chạy thực tế. |
| **Điều kiện B2** | Số Use Case cỡ trung bình | $\ge 20$ Use Cases (3-7 Tx) | **ĐẠT** | 35 tính năng lớn, mỗi tính năng đều có từ 3 đến 8 transaction cơ sở dữ liệu. |
| **Điều kiện B3 & B4** | Lỗi nghiêm trọng & Show-stopper | Lỗi logic $\le 3$, Show-stopper $\le 1$ | **ĐẠT (0 lỗi)** | Không còn lỗi show-stopper; lỗi xung đột trạng thái đăng nhập đã được xử lý triệt để. |
| **Điều kiện C1** | Định dạng hồ sơ | Đủ 7 báo cáo bằng tiếng Anh | **ĐẠT** | Báo cáo Report 7 hơn 200 trang viết bằng 100% tiếng Anh chuẩn. |
| **Điều kiện C2** | Gói cài đặt & CSDL mẫu | Cài được trên máy sạch | **ĐẠT** | File `Deploy_Demo_ETRManagementDB.sql` chứa đầy đủ dữ liệu thực tế cho 7 vai trò. |
