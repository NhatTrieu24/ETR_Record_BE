using ClosedXML.Excel;
using ETR.Application.Interfaces;
using ETR.Application.Services;
using ETR.Domain.Entities;
using ETR.Domain.Enums;
using Moq;
using Xunit;

namespace ETR.Application.Tests.Services;

public class ImportServiceAccountTests
{
    private static (Mock<IUnitOfWork> uow, Mock<IClassService> clsSvc, Mock<IEnrollmentService> enrSvc) BuildMocks()
    {
        var uow = new Mock<IUnitOfWork>();
        var clsSvc = new Mock<IClassService>();
        var enrSvc = new Mock<IEnrollmentService>();

        var roleRepo = new Mock<IGenericRepository<Role>>();
        roleRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Role>
            {
                new() { RoleId = 1, RoleName = "Admin" },
                new() { RoleId = 6, RoleName = "Student" }
            });

        var deptRepo = new Mock<IGenericRepository<Department>>();
        deptRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Department>
            {
                new() { DepartmentId = 1, DepartmentName = "Training" }
            });

        var accRepo = new Mock<IGenericRepository<Account>>();
        accRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account>());

        var profRepo = new Mock<IGenericRepository<UserProfile>>();
        profRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserProfile>
            {
                new() { AccountId = 10, UserCode = "STU-001", FullName = "Old Student", Email = "old@etr.com" }
            });

        var auditRepo = new Mock<IAuditLogRepository>();
        auditRepo.Setup(r => r.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        uow.Setup(u => u.RoleRepository).Returns(roleRepo.Object);
        uow.Setup(u => u.DepartmentRepository).Returns(deptRepo.Object);
        uow.Setup(u => u.AccountRepository).Returns(accRepo.Object);
        uow.Setup(u => u.UserProfileRepository).Returns(profRepo.Object);
        uow.Setup(u => u.AuditLogRepository).Returns(auditRepo.Object);
        uow.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        uow.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        uow.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        uow.Setup(u => u.ExecuteInStrategyAsync(It.IsAny<Func<CancellationToken, Task<ETR.Application.DTOs.Import.ImportCommitResult>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<ETR.Application.DTOs.Import.ImportCommitResult>>, CancellationToken>((action, ct) => action(ct));

        return (uow, clsSvc, enrSvc);
    }

    [Fact]
    public async Task GenerateAccountImportTemplateAsync_CreatesWorkbookWithAll9Columns()
    {
        var (uow, clsSvc, enrSvc) = BuildMocks();
        var service = new ImportService(uow.Object, clsSvc.Object, enrSvc.Object);

        var bytes = await service.GenerateAccountImportTemplateAsync();

        Assert.NotNull(bytes);
        Assert.NotEmpty(bytes);

        using var ms = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(ms);
        var ws = workbook.Worksheet("Tài khoản");
        Assert.NotNull(ws);

        Assert.Equal("Username (email)*", ws.Cell(2, 1).GetString());
        Assert.Equal("Mật khẩu*", ws.Cell(2, 2).GetString());
        Assert.Equal("Vai trò (Role)*", ws.Cell(2, 3).GetString());
        Assert.Equal("Phòng ban (Department)*", ws.Cell(2, 4).GetString());
        Assert.Equal("Họ và tên (FullName)*", ws.Cell(2, 5).GetString());
        Assert.Equal("Ngày sinh (dd/MM/yyyy)", ws.Cell(2, 6).GetString());
        Assert.Equal("Giới tính (Gender)", ws.Cell(2, 7).GetString());
        Assert.Equal("Số điện thoại (Phone)", ws.Cell(2, 8).GetString());
        Assert.Equal("Đơn vị/Tổ chức (Organization)", ws.Cell(2, 9).GetString());
    }

    [Fact]
    public async Task ValidateAccountImportAsync_FlagsMissingFullName()
    {
        var (uow, clsSvc, enrSvc) = BuildMocks();
        var service = new ImportService(uow.Object, clsSvc.Object, enrSvc.Object);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Tài khoản");
        ws.Cell(3, 1).Value = "student1@etr.com";
        ws.Cell(3, 2).Value = "P@ssw0rd123";
        ws.Cell(3, 3).Value = "Student";
        ws.Cell(3, 4).Value = "Training";
        ws.Cell(3, 5).Value = ""; // Missing FullName

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var result = await service.ValidateAccountImportAsync(stream, isCallerAdmin: true);

        Assert.False(result.CanCommit);
        Assert.Contains(result.Errors, e => e.Column == "FullName");
    }

    [Fact]
    public async Task CommitAccountImportAsync_CreatesBothAccountAndUserProfile_WithAutoGeneratedUserCode()
    {
        var (uow, clsSvc, enrSvc) = BuildMocks();

        Account? createdAccount = null;
        UserProfile? createdProfile = null;

        uow.Setup(u => u.AccountRepository.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
            .Callback<Account, CancellationToken>((a, _) =>
            {
                a.AccountId = 200;
                createdAccount = a;
            })
            .Returns(Task.CompletedTask);

        uow.Setup(u => u.UserProfileRepository.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()))
            .Callback<UserProfile, CancellationToken>((p, _) =>
            {
                createdProfile = p;
            })
            .Returns(Task.CompletedTask);

        var service = new ImportService(uow.Object, clsSvc.Object, enrSvc.Object);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Tài khoản");
        ws.Cell(3, 1).Value = "student2@etr.com";
        ws.Cell(3, 2).Value = "P@ssw0rd123";
        ws.Cell(3, 3).Value = "Student";
        ws.Cell(3, 4).Value = "Training";
        ws.Cell(3, 5).Value = "Nguyen Van B";
        ws.Cell(3, 6).Value = "15/05/2001";
        ws.Cell(3, 7).Value = "Nam";
        ws.Cell(3, 8).Value = "0912345678";
        ws.Cell(3, 9).Value = "Vietnam Airlines";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var result = await service.CommitAccountImportAsync(stream, createdByAccountId: 1, isCallerAdmin: true);

        Assert.Equal(1, result.Imported);
        Assert.Empty(result.Errors);

        Assert.NotNull(createdAccount);
        Assert.Equal("student2@etr.com", createdAccount.Username);

        Assert.NotNull(createdProfile);
        Assert.Equal(200, createdProfile.AccountId);
        Assert.Equal("Nguyen Van B", createdProfile.FullName);
        Assert.Equal("STU-002", createdProfile.UserCode);
        Assert.Equal("student2@etr.com", createdProfile.Email);
        Assert.Equal(new DateTime(2001, 5, 15), createdProfile.DateOfBirth);
        Assert.Equal("Nam", createdProfile.Gender);
        Assert.Equal("0912345678", createdProfile.Phone);
        Assert.Equal("Vietnam Airlines", createdProfile.Organization);
    }

    [Fact]
    public async Task GenerateStudentImportTemplateAsync_CreatesWorkbookWithAll8Columns()
    {
        var (uow, clsSvc, enrSvc) = BuildMocks();
        var service = new ImportService(uow.Object, clsSvc.Object, enrSvc.Object);

        var bytes = await service.GenerateStudentImportTemplateAsync();

        Assert.NotNull(bytes);
        Assert.NotEmpty(bytes);

        using var ms = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(ms);
        var ws = workbook.Worksheet("Học viên");
        Assert.NotNull(ws);

        Assert.Equal("Username (email)*", ws.Cell(2, 1).GetString());
        Assert.Equal("Mật khẩu*", ws.Cell(2, 2).GetString());
        Assert.Equal("Phòng ban (Department)*", ws.Cell(2, 3).GetString());
        Assert.Equal("Họ và tên (FullName)*", ws.Cell(2, 4).GetString());
        Assert.Equal("Ngày sinh (dd/MM/yyyy)", ws.Cell(2, 5).GetString());
        Assert.Equal("Giới tính (Gender)", ws.Cell(2, 6).GetString());
        Assert.Equal("Số điện thoại (Phone)", ws.Cell(2, 7).GetString());
        Assert.Equal("Đơn vị/Tổ chức (Organization)", ws.Cell(2, 8).GetString());
    }

    [Fact]
    public async Task ValidateStudentImportAsync_FlagsMissingDepartment()
    {
        var (uow, clsSvc, enrSvc) = BuildMocks();
        var service = new ImportService(uow.Object, clsSvc.Object, enrSvc.Object);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Học viên");
        ws.Cell(3, 1).Value = "student1@etr.com";
        ws.Cell(3, 2).Value = "P@ssw0rd123";
        ws.Cell(3, 3).Value = "NonExistentDept";
        ws.Cell(3, 4).Value = "Nguyen Van A";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var result = await service.ValidateStudentImportAsync(stream);

        Assert.False(result.CanCommit);
        Assert.Contains(result.Errors, e => e.Column == "DepartmentName");
    }

    [Fact]
    public async Task CommitStudentImportAsync_CreatesStudentAccountAndProfile()
    {
        var (uow, clsSvc, enrSvc) = BuildMocks();

        Account? createdAccount = null;
        UserProfile? createdProfile = null;

        uow.Setup(u => u.AccountRepository.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
            .Callback<Account, CancellationToken>((a, _) =>
            {
                a.AccountId = 300;
                createdAccount = a;
            })
            .Returns(Task.CompletedTask);

        uow.Setup(u => u.UserProfileRepository.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()))
            .Callback<UserProfile, CancellationToken>((p, _) =>
            {
                createdProfile = p;
            })
            .Returns(Task.CompletedTask);

        var service = new ImportService(uow.Object, clsSvc.Object, enrSvc.Object);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Học viên");
        ws.Cell(3, 1).Value = "student99@etr.com";
        ws.Cell(3, 2).Value = "P@ssw0rd123";
        ws.Cell(3, 3).Value = "Training";
        ws.Cell(3, 4).Value = "Tran Thi C";
        ws.Cell(3, 5).Value = "20/10/2002";
        ws.Cell(3, 6).Value = "Nữ";
        ws.Cell(3, 7).Value = "0987654321";
        ws.Cell(3, 8).Value = "Bamboo Airways";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var result = await service.CommitStudentImportAsync(stream, createdByAccountId: 1);

        Assert.Equal(1, result.Imported);
        Assert.Empty(result.Errors);

        Assert.NotNull(createdAccount);
        Assert.Equal("student99@etr.com", createdAccount.Username);
        Assert.Equal(6, createdAccount.RoleId); // RoleId 6 is Student

        Assert.NotNull(createdProfile);
        Assert.Equal(300, createdProfile.AccountId);
        Assert.Equal("Tran Thi C", createdProfile.FullName);
        Assert.Equal("STU-002", createdProfile.UserCode);
        Assert.Equal("student99@etr.com", createdProfile.Email);
        Assert.Equal(new DateTime(2002, 10, 20), createdProfile.DateOfBirth);
        Assert.Equal("Nữ", createdProfile.Gender);
        Assert.Equal("0987654321", createdProfile.Phone);
        Assert.Equal("Bamboo Airways", createdProfile.Organization);
    }

    [Theory]
    [InlineData("123456")] // Too short
    [InlineData("abcdefghij")] // Non-numeric
    [InlineData("0123")] // Too short
    [InlineData("01234567890123456")] // Too long
    public async Task ValidateAccountImportAsync_FlagsInvalidPhone(string invalidPhone)
    {
        var (uow, clsSvc, enrSvc) = BuildMocks();
        var service = new ImportService(uow.Object, clsSvc.Object, enrSvc.Object);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Tài khoản");
        ws.Cell(3, 1).Value = "student1@etr.com";
        ws.Cell(3, 2).Value = "P@ssw0rd123";
        ws.Cell(3, 3).Value = "Student";
        ws.Cell(3, 4).Value = "Training";
        ws.Cell(3, 5).Value = "Nguyen Van A";
        ws.Cell(3, 8).Value = invalidPhone;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var result = await service.ValidateAccountImportAsync(stream, isCallerAdmin: true);

        Assert.False(result.CanCommit);
        Assert.Contains(result.Errors, e => e.Column == "Phone");
    }

    [Theory]
    [InlineData("0912345678")] // Standard 10 digits
    [InlineData("+84912345678")] // Standard international
    [InlineData("02812345678")] // 11 digits
    [InlineData("0912-345-678")] // With hyphens
    public async Task ValidateAccountImportAsync_AcceptsValidPhone(string validPhone)
    {
        var (uow, clsSvc, enrSvc) = BuildMocks();
        var service = new ImportService(uow.Object, clsSvc.Object, enrSvc.Object);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Tài khoản");
        ws.Cell(3, 1).Value = "student1@etr.com";
        ws.Cell(3, 2).Value = "P@ssw0rd123";
        ws.Cell(3, 3).Value = "Student";
        ws.Cell(3, 4).Value = "Training";
        ws.Cell(3, 5).Value = "Nguyen Van A";
        ws.Cell(3, 8).Value = validPhone;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var result = await service.ValidateAccountImportAsync(stream, isCallerAdmin: true);

        Assert.True(result.CanCommit);
        Assert.DoesNotContain(result.Errors, e => e.Column == "Phone");
    }

    [Fact]
    public async Task GenerateAccountImportTemplateAsync_HasPhoneDataValidation()
    {
        var (uow, clsSvc, enrSvc) = BuildMocks();
        var service = new ImportService(uow.Object, clsSvc.Object, enrSvc.Object);

        var bytes = await service.GenerateAccountImportTemplateAsync();
        using var ms = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(ms);
        var ws = workbook.Worksheet("Tài khoản");

        var dataValidations = ws.DataValidations.GetAllInRange(ws.Range(3, 8, 503, 8).RangeAddress);
        Assert.NotEmpty(dataValidations);
    }
}
