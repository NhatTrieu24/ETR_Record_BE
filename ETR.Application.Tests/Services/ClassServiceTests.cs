using ETR.Application.Interfaces;
using ETR.Application.Services;
using ETR.Domain.Entities;
using Moq;

namespace ETR.Application.Tests.Services;

public class ClassServiceTests
{
    private static ClassService BuildService(string? roleName, int? accountId, List<Class> classes)
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        var classRepo = new Mock<IGenericRepository<Class>>();
        classRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(classes);
        unitOfWork.SetupGet(u => u.ClassRepository).Returns(classRepo.Object);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(c => c.RoleName).Returns(roleName);
        currentUser.SetupGet(c => c.AccountId).Returns(accountId);

        return new ClassService(unitOfWork.Object, currentUser.Object);
    }

    [Fact]
    public async Task GetAllClassesAsync_Instructor_OnlySeesOwnAssignedClasses()
    {
        // "Sân nhà ai nấy đá" (team decision 2026-08-08, docs/todo/addition.md).
        var classes = new List<Class>
        {
            new() { ClassId = 1, ClassCode = "C1", InstructorAccountId = 42 },
            new() { ClassId = 2, ClassCode = "C2", InstructorAccountId = 99 },
            new() { ClassId = 3, ClassCode = "C3", InstructorAccountId = 42 },
        };
        var service = BuildService("Instructor", accountId: 42, classes);

        var result = (await service.GetAllClassesAsync(CancellationToken.None)).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, c => Assert.Equal(42, c.InstructorAccountId));
    }

    [Fact]
    public async Task GetAllClassesAsync_Admin_SeesEveryClass()
    {
        var classes = new List<Class>
        {
            new() { ClassId = 1, ClassCode = "C1", InstructorAccountId = 42 },
            new() { ClassId = 2, ClassCode = "C2", InstructorAccountId = 99 },
        };
        var service = BuildService("Admin", accountId: 1, classes);

        var result = (await service.GetAllClassesAsync(CancellationToken.None)).ToList();

        Assert.Equal(2, result.Count);
    }
}
