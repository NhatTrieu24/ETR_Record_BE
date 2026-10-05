import docx
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
import os, winreg

# Get desktop path via Windows Registry
key = winreg.OpenKey(winreg.HKEY_CURRENT_USER, r'Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders')
val, _ = winreg.QueryValueEx(key, 'Desktop')
desktop_path = os.path.expandvars(val)

doc = docx.Document()

# Page setup - Margins 1 inch
for section in doc.sections:
    section.top_margin = Inches(1.0)
    section.bottom_margin = Inches(1.0)
    section.left_margin = Inches(1.0)
    section.right_margin = Inches(1.0)

# Set base style font
style = doc.styles['Normal']
font = style.font
font.name = 'Calibri'
font.size = Pt(12)
font.color.rgb = RGBColor(0x22, 0x22, 0x22)

# Title
title_p = doc.add_paragraph()
title_p.alignment = WD_ALIGN_PARAGRAPH.CENTER
run_title = title_p.add_run('KỊCH BẢN THUYẾT TRÌNH BẢO VỆ ĐỒ ÁN CAPSTONE (SEP490)\nHỆ THỐNG HỒ SƠ ĐÀO TẠO ĐIỆN TỬ HÀNG KHÔNG (ETR)')
run_title.bold = True
run_title.font.size = Pt(16)
run_title.font.color.rgb = RGBColor(0x0A, 0x36, 0x63)

# Subtitle
sub_p = doc.add_paragraph()
sub_p.alignment = WD_ALIGN_PARAGRAPH.CENTER
run_sub = sub_p.add_run('Mã đề tài: SU26SE104_GSU48 | Đề tài: Electronic Training Record (ETR) System in Aviation\n(Nội dung chuẩn đọc trực tiếp từ Slide 01 đến Slide 21 — Đồng bộ 100% với Report 7)')
run_sub.italic = True
run_sub.font.size = Pt(11)
run_sub.font.color.rgb = RGBColor(0x55, 0x55, 0x55)

doc.add_paragraph() # spacer

slides_data = [
    (
        'SLIDE 01: TITLE & TEAM INTRODUCTION',
        'Kính thưa Thầy/Cô Chủ tịch và toàn thể Quý Thầy/Cô trong Hội đồng chấm đồ án tốt nghiệp Capstone SEP490.\n\n'
        'Lời đầu tiên, em xin phép đại diện cho nhóm SU26SE104 gửi lời chào trân trọng nhất đến Hội đồng. '
        'Hôm nay, nhóm chúng em xin được báo cáo và bảo vệ đề tài: "Hệ thống Hồ sơ Đào tạo Điện tử trong Ngành Hàng không - '
        'Electronic Training Record (ETR) System in Aviation", dưới sự hướng dẫn khoa học của Giảng viên Thạc sĩ Ngô Đặng Hà An.\n\n'
        'Đề tài được nghiên cứu và phát triển toàn diện bởi 4 thành viên: Em - Nguyễn Hữu Nhật Triều (Team Leader, phụ trách Backend Core & Bảo mật), '
        'bạn Trần Trọng Nhân (Frontend Lead), bạn Võ Trọng Nhân (Frontend Developer), và bạn Đoàn Trọng Khôi (Backend Developer, phụ trách Automation Testing & Import/Export). '
        'Sau đây, em xin phép được bắt đầu phần trình bày của mình.'
    ),
    (
        'SLIDE 02: AGENDA',
        'Nội dung báo cáo hôm nay bám sát theo đúng tiến trình đánh giá chuẩn của Hội đồng, gồm 7 phần cốt lõi:\n'
        'Phần 1: Bối cảnh pháp lý và các quy chuẩn ngành Hàng không.\n'
        'Phần 2: Thực trạng và 4 lỗ hổng nghiêm trọng của quy trình quản lý hồ sơ truyền thống.\n'
        'Phần 3: Giải pháp hệ thống ETR và 4 trụ cột công nghệ chống giả mạo dữ liệu.\n'
        'Phần 4: Đặc tả yêu cầu chức năng với 7 nhóm vai trò phân tách trách nhiệm và 12 luồng nghiệp vụ cốt lõi.\n'
        'Phần 5: Phạm vi dự án, làm rõ việc hoàn thành 100% 68 Use Cases và các ranh giới loại trừ.\n'
        'Phần 6: Thiết kế kiến trúc hệ thống Clean Architecture và ngăn xếp công nghệ.\n'
        'Phần 7: Trình diễn trực tiếp 6 chuỗi nghiệp vụ thực tế và báo cáo kết quả kiểm thử chất lượng.'
    ),
    (
        'SLIDE 03: CONTEXT / BACKGROUND',
        'Kính thưa Hội đồng, ngành hàng không dân dụng là lĩnh vực đòi hỏi sự an toàn và kỷ luật tuyệt đối. '
        'Mọi hoạt động đào tạo phi công và nhân sự hàng không đều chịu sự giám sát nghiêm ngặt của Cục Hàng không Việt Nam (CAAV Part 10), '
        'Tổ chức Hàng không Dân dụng Quốc tế (ICAO Annex 1), cũng như các chuẩn mực quốc tế của FAA và EASA.\n\n'
        'Một học viên phi công phải trải qua lộ trình huấn luyện 3 giai đoạn liên tục và khép kín: Giai đoạn 1 là Lý thuyết mặt đất (Ground School); '
        'Giai đoạn 2 là Huấn luyện trên buồng lái mô phỏng (Simulator); và Giai đoạn 3 là Huấn luyện bay thực tế tích lũy giờ bay (Flight Operations).\n\n'
        'Cơ quan quản lý hàng không bắt buộc toàn bộ nhật ký giờ bay, điểm danh từng buổi, kết quả kiểm tra, minh chứng và chữ ký xác nhận của giáo viên '
        'phải được lưu trữ an toàn và bất biến từ 5 đến 10 năm để phục vụ các đợt thanh tra an toàn đột xuất. '
        'Tuy nhiên, phần lớn các trường bay hiện nay vẫn phụ thuộc vào hồ sơ giấy tờ, file PDF rời rạc và các bảng tính Excel phân tán, '
        'đặt ra những rủi ro rất lớn về tính toàn vẹn dữ liệu.'
    ),
    (
        'SLIDE 04: PROBLEM — KEY ISSUES & REGULATORY IMPACT',
        'Khi khảo sát thực tế tại các trung tâm huấn luyện bay, nhóm chúng em đã nhận diện 4 lỗ hổng nghiêm trọng mang tính sống còn:\n\n'
        'Thứ nhất là Dữ liệu phân mảnh và chi phí lưu trữ khổng lồ: Hồ sơ đào tạo nằm rải rác ở sổ tay, file scan và bảng tính Excel, '
        'vi phạm quy định lưu trữ 5 đến 10 năm bắt buộc của CAAV Part 10. Khi có thanh tra đột xuất, trường bay mất từ 2 đến 3 tuần chỉ để tìm kiếm và đối chiếu hồ sơ.\n\n'
        'Thứ hai là Nguy cơ sửa điểm và hợp thức hóa giờ bay hồi tố: Do không có cơ chế ràng buộc thời gian, giảng viên có thể sửa đổi điểm số '
        'hoặc điểm danh bù trong quá khứ mà không để lại bất kỳ dấu vết kiểm toán nào, tiềm ẩn nguy cơ cho phép nhân sự chưa đạt điều kiện tham gia chuyến bay.\n\n'
        'Thứ ba là Vi phạm nguyên tắc Kiểm soát Độc lập (Maker-Checker): Giảng viên vừa trực tiếp giảng dạy, vừa nhập điểm và tự phê duyệt hoàn thành '
        'mà thiếu sự thẩm định độc lập từ bộ phận Đảm bảo chất lượng (QA); các tệp minh chứng bài bay rất dễ bị làm giả hoặc tráo đổi.\n\n'
        'Thứ tư là Nghẽn luồng phê duyệt và nguy cơ can thiệp sau chốt: Hồ sơ giấy mất hàng tuần để luân chuyển qua các cấp lãnh đạo; '
        'nguy hiểm hơn, hồ sơ sau khi đã ký chốt vẫn có thể bị tráo trang hoặc can thiệp bất hợp pháp mà không thể phát hiện.'
    ),
    (
        'SLIDE 05: SOLUTION & CORE ARCHITECTURAL FEATURES',
        'Để giải quyết triệt để 4 bài toán cốt lõi vừa nêu, nhóm chúng em đã xây dựng Hệ thống ETR với 4 giải pháp công nghệ đối ứng trực tiếp:\n\n'
        'Một là Nguồn chân lý duy nhất (Single Source of Truth): Khi học viên ghi danh vào lớp, hệ thống tự động khởi tạo hồ sơ ETR '
        'cùng toàn bộ cây môn học con, rút ngắn thời gian chuẩn bị hồ sơ thanh tra từ 2 tuần xuống chỉ còn đúng 3 giây.\n\n'
        'Hai là Cổng kiểm soát tự động và Nhật ký kiểm toán bất biến: Hệ thống áp dụng Quy tắc 48h Grace Period tự động khóa quyền điểm danh sau 48 giờ; '
        'chụp ảnh điểm đạt (Passing Score Snapshot); và cơ chế EF Core Interceptor tự động ghi nhận 100% thay đổi dữ liệu cũ - mới kèm thời gian và định danh người thực hiện.\n\n'
        'Ba là Quy trình Maker-Checker và Mã hóa băm SHA-256: Phân tách độc lập tuyệt đối giữa Giảng viên (người tạo minh chứng) và QA (người thẩm định); '
        'toàn bộ tệp minh chứng lưu trữ trên Cloudinary đều được băm mã SHA-256 ngay tại trình duyệt để chống tráo đổi tệp tin.\n\n'
        'Bốn là Số hóa 3 tầng ký duyệt và Cơ chế Đóng băng vĩnh viễn (Deep Freeze): Rút ngắn thời gian phê duyệt hồ sơ xuống tính bằng phút; '
        'và khi Trưởng ban đào tạo phê duyệt cuối cùng, hệ thống kích hoạt cờ IsLocked = true đóng băng vĩnh viễn hồ sơ, chặn tuyệt đối mọi hành vi sửa xóa ở mức Database.'
    ),
    (
        'SLIDE 06: FUNCTIONAL REQUIREMENTS — SYSTEM STAKEHOLDERS & ROLES',
        'Hệ thống ETR được thiết kế với 7 vai trò chuyên biệt, thực thi nghiêm ngặt nguyên tắc Phân tách Trách nhiệm (Separation of Duties) ở tầng mã nguồn:\n\n'
        '1. Administrator: Quản trị tài khoản, phân quyền RBAC và giám sát toàn bộ Audit Log của hệ thống.\n'
        '2. Academic Staff (Cán bộ học vụ): Quản lý khung giáo trình, mở lớp và thực hiện import hàng loạt danh sách học viên qua Excel.\n'
        '3. Instructor (Giảng viên bay): Điểm danh buổi học trong khung 48 giờ, chấm điểm lý thuyết, đánh giá checklist kỹ năng bay, tải minh chứng và ký chốt môn học.\n'
        '4. QA Staff (Kiểm soát chất lượng): Thẩm định minh chứng độc lập theo nguyên tắc Maker-Checker, duyệt hoặc trả hồ sơ yêu cầu sửa đổi.\n'
        '5. Training Manager (Trưởng ban đào tạo): Thẩm định điều kiện tổng thể, phê duyệt cấp chứng chỉ và kích hoạt cơ chế đóng băng hồ sơ Deep Freeze.\n'
        '6. Compliance Auditor (Thanh tra viên Cục Hàng không): Có quyền chỉ đọc (Read-only), tra cứu lịch sử và xuất gói kiểm toán trọn gói.\n'
        '7. Student (Học viên phi công): Cổng cá nhân theo dõi lộ trình huấn luyện, giờ bay tích lũy, chứng chỉ và kiểm tra trạng thái cấm bay Grounded.'
    ),
    (
        'SLIDE 07: CORE OPERATIONAL WORKFLOW & 68 USE CASES MAPPING',
        'Kính thưa Hội đồng, toàn bộ nghiệp vụ của hệ thống ETR được chuẩn hóa qua 4 giai đoạn tác nghiệp liên hoàn, bao phủ trọn vẹn 68 Use Cases được đặc tả trong Báo cáo Report 7:\n\n'
        'Giai đoạn 1 - Khởi tạo Đào tạo (gồm 22 Use Cases từ UC-01 đến UC-14 và UC-48 đến UC-55): Cán bộ học vụ và Quản trị viên thiết lập khung giáo trình, chụp ảnh ngưỡng điểm đậu (Passing Score Snapshot), mở lớp, batch import học viên và tự động sinh hồ sơ ETR ở trạng thái InProgress.\n\n'
        'Giai đoạn 2 - Tác nghiệp Huấn luyện (gồm 11 Use Cases từ UC-15 đến UC-25): Giảng viên bay thực hiện điểm danh có khóa tự động 48 giờ (Grace Period), kiểm tra cổng chuyên cần 80%, chấm điểm lý thuyết, đánh giá checklist kỹ năng, và tải lên minh chứng có băm mã SHA-256 bảo vệ tính toàn vẹn.\n\n'
        'Giai đoạn 3 - Thẩm định và Phê duyệt (gồm 22 Use Cases từ UC-26 đến UC-47): Giảng viên ký chốt môn học; Chuyên viên QA thẩm định độc lập theo nguyên tắc Maker-Checker (không thể tự duyệt bài của mình); và Trưởng ban Đào tạo phê duyệt cuối cùng, kích hoạt cơ chế Deep Freeze đóng băng vĩnh viễn hồ sơ ở tầng Database.\n\n'
        'Giai đoạn 4 - Kiểm toán và Khai thác (gồm 13 Use Cases từ UC-56 đến UC-68): Học viên theo dõi lộ trình và hạn chứng chỉ tránh bị cấm bay (Grounded); Thanh tra viên Cục Hàng không xuất trọn gói hồ sơ kiểm toán PDF và ZIP một chạm chỉ trong 3 giây; đồng thời hỗ trợ luồng mở khóa phúc khảo khẩn cấp có kiểm soát.'
    ),
    (
        'SLIDE 08: MAIN FUNCTIONS MAPPING (8 FEATURE GROUPS & SRS MATRIX)',
        'Bốn giai đoạn tác nghiệp vừa nêu được hiện thực hóa thành 8 nhóm tính năng cốt lõi trên toàn hệ thống, ánh xạ hoàn chỉnh tới 68 Use Cases trong tài liệu đặc tả SRS:\n\n'
        '1. Nhóm Quản trị hệ thống (UC-48 đến UC-55) do Administrator quản lý.\n'
        '2. Nhóm Chương trình & Lớp học (UC-02 đến UC-09) và Nhóm Vòng đời hồ sơ ETR (UC-07, UC-13, UC-14) do Học vụ phụ trách.\n'
        '3. Nhóm Điểm danh & Đánh giá (UC-15 đến UC-25) do Giảng viên bay tác nghiệp hàng ngày.\n'
        '4. Nhóm Minh chứng & Thẩm định QA (UC-26 đến UC-37) đảm bảo nguyên tắc kiểm soát độc lập Maker-Checker.\n'
        '5. Nhóm Phê duyệt & Đóng băng Deep Freeze (UC-38 đến UC-47) do Trưởng ban Đào tạo quyết định.\n'
        '6. Nhóm Kiểm toán & Xuất báo cáo CAA (UC-62 đến UC-68) phục vụ thanh tra an toàn hàng không.\n'
        '7. Nhóm Dashboard & Theo dõi hạn chứng chỉ (UC-01, UC-56 đến UC-61) cá nhân hóa cho từng vai trò và học viên.\n\n'
        'Ma trận phân bổ này chứng minh 100% yêu cầu chức năng đều được phân bổ đúng vai trò, không có bất kỳ chức năng nào bị bỏ sót hay chồng chéo thẩm quyền.'
    ),
    (
        'SLIDE 09: PROJECT SCOPE — IN-SCOPE VS. OUT-OF-SCOPE',
        'Về phạm vi triển khai, nhóm tự hào đạt tỷ lệ hoàn thành 100%, bàn giao trọn vẹn 68/68 Use Cases, 35 tính năng lớn, và vượt qua 235 test cases tự động.\n\n'
        'Trong phạm vi (In-Scope), hệ thống số hóa 100% vòng đời huấn luyện bay từ Lý thuyết đến Thực hành; '
        'đảm bảo tính toàn vẹn dữ liệu bằng mã băm SHA-256, khóa đóng băng đa tầng, nhật ký kiểm toán tự động, và hỗ trợ import/export Excel, PDF trọn gói.\n\n'
        'Để đảm bảo chiều sâu tối đa cho tiêu chuẩn tuân thủ hàng không, nhóm chủ động loại trừ (Out-of-Scope) 3 mảng: '
        'Không làm cổng thanh toán học phí vì thuộc về hệ thống ERP tài chính; '
        'Không thu thập dữ liệu cảm biến bay thời gian thực vì dữ liệu hộp đen thuộc về thiết bị Avionics trên máy bay; '
        'và tập trung phát triển nền tảng Web/PWA chuẩn hóa tối ưu cho máy tính bảng tại buồng lái thay vì đưa lên App Store công cộng.'
    ),
    (
        'SLIDE 10: SYSTEM DESIGN / ARCHITECTURE',
        'Về thiết kế hệ thống, nhóm áp dụng kiến trúc Clean Architecture chuẩn mực với 4 tầng phân tách độc lập:\n\n'
        'Tầng ngoài cùng là Web API đóng vai trò tiếp nhận yêu cầu, cấu hình xác thực JWT, phân quyền RBAC và xử lý lỗi tập trung qua Global Exception Handler.\n'
        'Tầng Application chứa toàn bộ logic nghiệp vụ, các DTOs, và các bộ quy tắc kiểm tra nghiêm ngặt như ClassOwnershipValidator hay ImmutabilityValidator.\n'
        'Tầng Domain định nghĩa các thực thể cốt lõi, Aggregate Roots và các quy tắc bất biến của ngành hàng không.\n'
        'Tầng Infrastructure chịu trách nhiệm giao tiếp cơ sở dữ liệu qua EF Core. Điểm đặc sắc là bộ EF Core Interceptor tự động bắt sự kiện SaveChangesAsync '
        'để serialize giá trị cũ và mới sang JSON lưu vào bảng AuditLogs, đồng thời chặn đứng câu lệnh UPDATE nếu hồ sơ đã bị khóa Deep Freeze.'
    ),
    (
        'SLIDE 11: TECHNOLOGY STACK & TECHNICAL RATIONALE',
        'Từng công nghệ trong hệ thống đều được nhóm lựa chọn có chủ đích nhằm đáp ứng các đòi hỏi khắt khe của ngành hàng không:\n\n'
        'Về Backend, chúng em sử dụng ASP.NET Core Web API trên nền tảng .NET 10 mới nhất, mang lại hiệu năng xử lý giao dịch cao và kiến trúc phân tầng Clean Architecture rõ ràng.\n'
        'Về Cơ sở dữ liệu, chúng em chọn Microsoft SQL Server để đảm bảo tính toàn vẹn ACID tuyệt đối, đồng thời xây dựng các Filtered Unique Index giải quyết triệt để xung đột khi xóa mềm (Soft-Delete).\n'
        'Về Frontend, nhóm dùng React 19 kết hợp Vite, đảm bảo tốc độ phản hồi nhanh, tối ưu hóa giao diện cho máy tính bảng của giáo viên bay.\n'
        'Về Minh chứng và Tệp tin, hệ thống kết hợp Cloudinary CDN với thuật toán băm mật mã SHA-256, giúp bảo vệ tính toàn vẹn của bằng chứng đào tạo.\n'
        'Về Báo cáo và Email, QuestPDF và ClosedXML giúp tạo gói kiểm toán PDF và Excel tức thì, trong khi MailKit tự động gửi email cảnh báo khi chứng chỉ phi công sắp hết hạn.'
    ),
    (
        'SLIDE 12: FLOW 1 — USER & ACCESS MANAGEMENT',
        'Bắt đầu chuỗi 6 luồng nghiệp vụ thực tế là Luồng 1: Quản trị Người dùng và Phân quyền do vai trò Administrator đảm nhiệm.\n\n'
        'Tại luồng này, Quản trị viên khởi tạo tài khoản người dùng với các ràng buộc dữ liệu chặt chẽ như định dạng email hàng không, '
        'số điện thoại và cơ chế mã hóa mật khẩu một chiều BCrypt. '
        'Hệ thống thực thi cơ chế Zero-Trust RBAC trên 7 vai trò, đảm bảo mỗi người dùng chỉ được cấp phát đúng thẩm quyền của mình. '
        'Đồng thời, mọi hành động thêm mới tài khoản, phân quyền hoặc khóa tài khoản đều được ghi vết tự động vào bảng Audit Log bất biến kèm địa chỉ IP và dấu thời gian thực.'
    ),
    (
        'SLIDE 13: FLOW 2 — LEARNER, COURSE & ENROLLMENT',
        'Tiếp theo là Luồng 2: Khởi tạo Đào tạo và Tự động sinh Hồ sơ ETR do Cán bộ Học vụ (Academic Staff) thực hiện.\n\n'
        'Học vụ quản lý khung giáo trình chuẩn gồm các môn Lý thuyết, Buồng lái mô phỏng và Bay thực hành. '
        'Khi mở lớp huấn luyện, hệ thống tự động định tuyến cơ sở vật chất theo tính chất môn học: phòng học lý thuyết, trung tâm SIM, hoặc bãi đỗ bay thực tế.\n\n'
        'Đặc biệt, ngay khi học viên được ghi danh vào lớp, hệ thống tự động sinh hồ sơ ETR ở trạng thái InProgress cùng toàn bộ cây môn học con, '
        'đồng thời chụp ảnh ngưỡng điểm đậu (Passing Score Snapshot) tại thời điểm ghi danh. '
        'Cơ chế này đảm bảo hồ sơ không bao giờ bị thất lạc, và việc thay đổi giáo trình sau này không làm sai lệch kết quả của các khóa học trước.'
    ),
    (
        'SLIDE 14: FLOW 3 — ATTENDANCE & ASSESSMENT',
        'Luồng thứ 3 là Tác nghiệp Chuyên cần, Đánh giá và Nộp minh chứng của Giảng viên bay (Instructor).\n\n'
        'Thứ nhất, hệ thống áp dụng cơ chế Phân quyền dữ liệu theo dòng (Data Isolation): Giảng viên chỉ được xem và chấm điểm trên đúng các lớp và môn học mà mình được phân công giảng dạy.\n\n'
        'Thứ hai là Quy tắc 48h Grace Period: Giảng viên chỉ được phép ghi nhận hoặc điều chỉnh điểm danh trong vòng 48 giờ kể từ khi buổi bay kết thúc, sau đó hệ thống sẽ tự động khóa cứng để chống sửa dữ liệu hồi tố.\n\n'
        'Thứ ba là Cổng chuyên cần 80%: Hệ thống tự động tính tỷ lệ chuyên cần và sẽ khóa quyền vào thi nếu học viên nghỉ quá 20% số buổi.\n\n'
        'Cuối cùng, khi chấm điểm lý thuyết và checklist kỹ năng, giảng viên tải lên bản scan sổ bay (Logbook). '
        'Tệp tin được băm mã SHA-256 ngay trên trình duyệt trước khi tải lên Cloudinary, đảm bảo tính toàn vẹn và chống tráo đổi file trước khi giảng viên ký chốt môn học.'
    ),
    (
        'SLIDE 15: FLOW 4 — EVIDENCE & QA VERIFICATION',
        'Luồng thứ 4 là Kiểm soát Chất lượng và Thẩm định Minh chứng do Chuyên viên QA đảm nhiệm.\n\n'
        'Trọng tâm của luồng này là thực thi triệt để nguyên tắc Maker-Checker: Người tạo minh chứng (Giảng viên) tuyệt đối không có quyền tự duyệt minh chứng của chính mình. '
        'Hệ thống tự động ngăn chặn điều này ở mức API Middleware.\n\n'
        'Chuyên viên QA mở hàng đợi thẩm định, trực tiếp xem tệp scan trên Cloudinary và kiểm tra mã băm SHA-256 đối soát. '
        'Khi QA bấm duyệt, minh chứng chuyển trạng thái Verified. '
        'Sau đó, QA kiểm tra tính hoàn chỉnh của toàn bộ hồ sơ ETR và xác nhận chuyển hồ sơ sang trạng thái Verified, '
        'hoặc sử dụng tính năng Return for Correction kèm lý do bắt buộc nếu phát hiện hồ sơ chưa đạt chuẩn.'
    ),
    (
        'SLIDE 16: FLOW 5 — ETR APPROVAL & DEEP FREEZE',
        'Luồng thứ 5 là Thẩm định Tối cao và Kích hoạt Khóa đóng băng vĩnh viễn (Deep Freeze) của Trưởng ban Đào tạo (Training Manager).\n\n'
        'Khi Trưởng ban đào tạo mở hồ sơ ETR đã được QA xác nhận, hệ thống kích hoạt bộ máy Pre-Validation Engine '
        'tự động rà soát toàn bộ các điều kiện tiên quyết: 100% chuyên cần đã ghi nhận, tất cả bài thi đạt điểm đậu, checklist thực hành hoàn tất, và minh chứng đã thẩm định.\n\n'
        'Khi Trưởng ban bấm Final Approve, hệ thống thực hiện đồng thời trong một giao dịch cơ sở dữ liệu duy nhất: '
        'Chuyển trạng thái ETR sang Approved, cấp số hiệu chứng chỉ tốt nghiệp phi công, và kích hoạt cờ IsLocked = true. '
        'Kể từ thời điểm này, cơ chế Deep Freeze ở tầng Database sẽ đóng băng vĩnh viễn hồ sơ, chặn đứng mọi hành vi sửa đổi dữ liệu trái phép.'
    ),
    (
        'SLIDE 17: FLOW 6 — REPORTING, EXPORT & AUDIT',
        'Luồng thứ 6 là Cổng Thanh tra Hàng không và Cổng Học viên do Thanh tra viên (Auditor) và Học viên sử dụng.\n\n'
        'Đối với Thanh tra viên Cục Hàng không (CAAV), hệ thống cung cấp cổng tra cứu chuyên biệt với quyền chỉ đọc tuyệt đối. '
        'Thanh tra viên có thể sử dụng bộ lọc tìm kiếm nâng cao để tra cứu hồ sơ và bấm nút xuất gói kiểm toán CAA Training Package. '
        'Chỉ mất đúng 3 giây, toàn bộ hồ sơ điện tử, bảng điểm, chữ ký và file minh chứng sẽ được biên dịch thành 1 file ZIP và PDF Dossier trọn gói đạt chuẩn kiểm toán quốc tế.\n\n'
        'Đối với Học viên, cổng cá nhân giúp phi công theo dõi tiến độ tích lũy giờ bay, tải chứng chỉ tốt nghiệp, '
        'và hệ thống sẽ tự động cảnh báo cấm bay (Grounded) nếu các chứng chỉ định kỳ hoặc giờ huấn luyện lặp lại bị quá hạn.'
    ),
    (
        'SLIDE 18: TESTING & QUALITY ASSURANCE',
        'Kính thưa Hội đồng, để đảm bảo độ tin cậy tuyệt đối cho một hệ thống phần mềm trong ngành hàng không, '
        'nhóm chúng em đã triển khai chiến lược kiểm thử tự động toàn diện với tổng số 235 test cases:\n\n'
        'Bao gồm 129 White-Box Unit Tests được lập trình bằng xUnit và Moq theo mô hình AAA (Arrange-Act-Assert), '
        'bao phủ 31 hàm nghiệp vụ cốt lõi từ tính giờ bay, kiểm tra cổng chuyên cần, mã băm minh chứng, cho đến giao dịch rollback dữ liệu. '
        'Các test cases bao gồm 40 ca bình thường, 64 ca bất thường và 25 ca biên.\n\n'
        'Bên cạnh đó là 106 Black-Box System & Integration Tests thực hiện qua 3 vòng kiểm thử nghiêm ngặt: '
        'Vòng 1 phát hiện lỗi, Vòng 2 sửa lỗi và kiểm thử lại, và Vòng 3 kiểm thử hồi quy toàn diện.\n\n'
        'Kết quả đạt tỷ lệ Pass tuyệt đối 100% (235 trên 235 tests) và hệ thống được bàn giao với 0 lỗi mở (Zero Open Defects).'
    ),
    (
        'SLIDE 19: TEAM ROLES & CONTRIBUTION MATRIX',
        'Khối lượng công việc đồ sộ của dự án được phân bổ cân đối và minh bạch dựa trên năng lực chuyên môn của 4 thành viên, '
        'được ghi nhận rõ ràng qua lịch sử commit trên GitHub:\n\n'
        'Bạn Nguyễn Hữu Nhật Triều đảm nhiệm vai trò Team Leader, chịu trách nhiệm thiết kế kiến trúc Clean Architecture, '
        'bảo mật Zero-Trust, bộ EF Core Interceptor ghi Audit Log và luồng vòng đời ETR cốt lõi với 116 commits.\n\n'
        'Bạn Đoàn Trọng Khôi xây dựng toàn bộ phân hệ Import/Export Excel và PDF kiểm toán, dịch vụ gửi Email thông báo tự động, '
        'bộ tính toán hạn chứng chỉ phi công và 129 Unit Tests với 63 commits.\n\n'
        'Bạn Trần Trọng Nhân phụ trách Frontend Lead, xây dựng giao diện cổng Học vụ, Ban Đào tạo, quản lý State và bộ kiểm thử Vitest với 102 commits.\n\n'
        'Bạn Võ Trọng Nhân phát triển giao diện cổng Giảng viên, cổng Thẩm định QA, tích hợp upload Cloudinary và mã hóa băm SHA-256 với 64 commits.'
    ),
    (
        'SLIDE 20: LIMITATIONS & FUTURE DEVELOPMENT',
        'Nhóm cũng thẳng thắn nhìn nhận các giới hạn hiện tại của đề tài: '
        'Hệ thống hiện hoạt động trên môi trường Web có kết nối mạng; '
        'chưa kết nối API trực tiếp với cơ sở dữ liệu quốc gia của Cục Hàng không; '
        'và chưa áp dụng chữ ký số USB Token PKI hoặc sinh trắc học vân tay khi ký chốt.\n\n'
        'Về định hướng phát triển trong tương lai, nhóm sẽ mở rộng ứng dụng di động cho phép giảng viên ghi nhận bài bay ngoại tuyến (Offline) ngay trên đường băng; '
        'tích hợp hệ thống quản lý học tập LMS; và áp dụng trí tuệ nhân tạo (AI) để phân tích dữ liệu, đưa ra cảnh báo sớm cho các học viên có nguy cơ không đạt yêu cầu bay.'
    ),
    (
        'SLIDE 21: CONCLUSION & Q&A',
        'Tóm lại, Hệ thống ETR đã giải quyết trọn vẹn bài toán chuyển đổi số trong quản lý hồ sơ đào tạo phi công: '
        'Thay thế hoàn toàn phương thức sổ sách giấy tờ thủ công, xóa bỏ nguy cơ sửa điểm và hợp thức hóa giờ bay hồi tố, '
        'thực thi nghiêm ngặt nguyên tắc kiểm soát độc lập Maker-Checker, và bảo vệ hồ sơ an toàn tuyệt đối bằng cơ chế đóng băng Deep Freeze.\n\n'
        'Thay mặt nhóm phát triển SU26SE104, em xin chân thành cảm ơn Quý Thầy/Cô trong Hội đồng chấm đồ án tốt nghiệp Capstone SEP490 đã chú ý lắng nghe. '
        'Nhóm chúng em rất mong nhận được những ý kiến đóng góp quý báu và các câu hỏi phản biện từ Quý Thầy/Cô!'
    )
]

for idx, (title, content) in enumerate(slides_data):
    # Slide Title Heading
    h = doc.add_paragraph()
    h.paragraph_format.space_before = Pt(14)
    h.paragraph_format.space_after = Pt(4)
    h.paragraph_format.keep_with_next = True
    
    run_h = h.add_run(f'▶  {title}')
    run_h.bold = True
    run_h.font.size = Pt(13)
    run_h.font.color.rgb = RGBColor(0x0A, 0x36, 0x63)
    
    # Sub-box for Content to read
    callout_p = doc.add_paragraph()
    callout_p.paragraph_format.space_before = Pt(2)
    callout_p.paragraph_format.space_after = Pt(12)
    callout_p.paragraph_format.line_spacing = 1.25
    
    label_run = callout_p.add_run('Nội dung cần đọc:\n')
    label_run.bold = True
    label_run.font.size = Pt(11)
    label_run.font.color.rgb = RGBColor(0x8B, 0x00, 0x00) # Dark red accent
    
    body_run = callout_p.add_run(content)
    body_run.font.size = Pt(11.5)
    body_run.font.color.rgb = RGBColor(0x1A, 0x1A, 0x1A)
    
    # Divider between slides (except last)
    if idx < len(slides_data) - 1:
        div_p = doc.add_paragraph()
        div_p.paragraph_format.space_before = Pt(2)
        div_p.paragraph_format.space_after = Pt(8)
        div_run = div_p.add_run('―' * 55)
        div_run.font.color.rgb = RGBColor(0xCC, 0xCC, 0xCC)

# Save to Desktop
dest_desktop = os.path.join(desktop_path, 'Kich_Ban_Thuyet_Trinh_21_Slide_ETR.docx')
doc.save(dest_desktop)
print('SUCCESS_DESKTOP: ' + dest_desktop)

# Also save to Report folder as backup
report_dir = r'D:\Project\CapStone\ETR\ETR_Record_BE\Report'
dest_report = os.path.join(report_dir, 'Kich_Ban_Thuyet_Trinh_21_Slide_ETR.docx')
doc.save(dest_report)
print('SUCCESS_REPORT: ' + dest_report)
