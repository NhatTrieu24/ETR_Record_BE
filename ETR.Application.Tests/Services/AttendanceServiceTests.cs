using ETR.Application.Compliance;
using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Application.Services;
using ETR.Domain.Entities;
using Moq;

namespace ETR.Application.Tests.Services;

public class AttendanceServiceTests
{
    private static AttendanceService BuildService(Session session, Class trainingClass, ClassStudent? classStudent)
    {
        var unitOfWork = new Mock<IUnitOfWork>();

        unitOfWork.Setup(u => u.ExecuteInStrategyAsync(It.IsAny<Func<CancellationToken, Task<AttendanceRecordResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<AttendanceRecordResponse>> op, CancellationToken ct) => op(ct));
        unitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWork.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWork.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var sessionRepo = new Mock<IGenericRepository<Session>>();
        sessionRepo.Setup(r => r.GetByIdAsync(session.SessionId, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        unitOfWork.SetupGet(u => u.SessionRepository).Returns(sessionRepo.Object);

        var classRepo = new Mock<IGenericRepository<Class>>();
        classRepo.Setup(r => r.GetByIdAsync(trainingClass.ClassId, It.IsAny<CancellationToken>())).ReturnsAsync(trainingClass);
        unitOfWork.SetupGet(u => u.ClassRepository).Returns(classRepo.Object);

        var classStudentRepo = new Mock<IGenericRepository<ClassStudent>>();
        classStudentRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(classStudent);
        unitOfWork.SetupGet(u => u.ClassStudentRepository).Returns(classStudentRepo.Object);

        var etrRepo = new Mock<IETRCourseRecordRepository>();
        etrRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ETRCourseRecord>());
        unitOfWork.SetupGet(u => u.ETRCourseRecordRepository).Returns(etrRepo.Object);

        var subjectResultRepo = new Mock<IGenericRepository<SubjectResult>>();
        subjectResultRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<SubjectResult>());
        unitOfWork.SetupGet(u => u.SubjectResultRepository).Returns(subjectResultRepo.Object);

        var attendanceRecordRepo = new Mock<IGenericRepository<AttendanceRecord>>();
        attendanceRecordRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<AttendanceRecord>());
        attendanceRecordRepo.Setup(r => r.AddAsync(It.IsAny<AttendanceRecord>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWork.SetupGet(u => u.AttendanceRecordRepository).Returns(attendanceRecordRepo.Object);

        return new AttendanceService(unitOfWork.Object);
    }

    [Fact]
    public async Task RecordAttendanceAsync_InstructorNotAssignedToClass_ThrowsForbidden()
    {
        // "Sân nhà ai nấy đá" (team decision 2026-08-08, docs/todo/addition.md).
        var session = new Session { SessionId = 1, ClassId = 10, IsConfirmed = false };
        var trainingClass = new Class { ClassId = 10, InstructorAccountId = 99 };
        var classStudent = new ClassStudent { ClassStudentId = 1, ClassId = 10, CourseEnrollmentId = 5 };
        var service = BuildService(session, trainingClass, classStudent);

        var request = new CreateAttendanceRecordRequest(1, 1, "Present", null);

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => service.RecordAttendanceAsync(request, recordedByAccountId: 42, recordedByRoleName: "Instructor", CancellationToken.None));
    }

    [Fact]
    public async Task RecordAttendanceAsync_AssignedInstructor_Succeeds()
    {
        var session = new Session { SessionId = 1, ClassId = 10, IsConfirmed = false };
        var trainingClass = new Class { ClassId = 10, InstructorAccountId = 42 };
        var classStudent = new ClassStudent { ClassStudentId = 1, ClassId = 10, CourseEnrollmentId = 5 };
        var service = BuildService(session, trainingClass, classStudent);

        var request = new CreateAttendanceRecordRequest(1, 1, "Present", null);

        var response = await service.RecordAttendanceAsync(request, recordedByAccountId: 42, recordedByRoleName: "Instructor", CancellationToken.None);

        Assert.Equal("Present", response.Status);
    }
}
