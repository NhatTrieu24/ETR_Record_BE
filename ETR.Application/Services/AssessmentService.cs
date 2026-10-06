using ETR.Application.Compliance;
using ETR.Application.DTOs.Assessment.Requests;
using ETR.Application.DTOs.Assessment.Responses;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ETR.Application.Services;

public class AssessmentService : IAssessmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICourseService _courseService;

    public AssessmentService(IUnitOfWork unitOfWork, ICourseService courseService)
    {
        _unitOfWork = unitOfWork;
        _courseService = courseService;
    }

    public async Task<IEnumerable<AssessmentResponse>> GetAllAssessmentsAsync(CancellationToken cancellationToken = default)
    {
        var items = await _unitOfWork.AssessmentRepository.GetAllAsync(cancellationToken);
        return items.Select(MapToResponse);
    }

    public async Task<AssessmentResponse> GetAssessmentByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = await _unitOfWork.AssessmentRepository.GetByIdAsync(id, cancellationToken);
        if (item == null) throw new KeyNotFoundException("Assessment not found.");
        return MapToResponse(item);
    }

    public async Task<AssessmentResponse> CreateAssessmentAsync(CreateAssessmentRequest request, int createdByAccountId, CancellationToken cancellationToken = default)
    {
        var existingAssessments = (_unitOfWork.AssessmentRepository != null
            ? await _unitOfWork.AssessmentRepository.GetAllAsync(cancellationToken)
            : null) ?? Enumerable.Empty<Assessment>();
        var subjectAssessments = existingAssessments
            .Where(a => a.CourseId == request.CourseId && a.SubjectId == request.SubjectId && !a.IsDeleted)
            .ToList();

        var existingAssessmentIds = subjectAssessments.Select(a => a.AssessmentId).ToHashSet();
        var allResults = _unitOfWork.AssessmentResultRepository != null
            ? await _unitOfWork.AssessmentResultRepository.GetAllAsync(cancellationToken)
            : null;
        var hasRecordedResults = (allResults ?? Enumerable.Empty<AssessmentResult>())
            .Any(ar => existingAssessmentIds.Contains(ar.AssessmentId) && !ar.IsDeleted);

        if (hasRecordedResults)
        {
            throw new BusinessRuleViolationException(
                "Không thể tạo thêm bài kiểm tra cho môn học này vì đã có học viên có kết quả điểm số được ghi nhận trong hệ thống.");
        }

        var totalWeight = subjectAssessments.Sum(a => a.Weight);
        if (totalWeight >= 100)
        {
            await _courseService.EnsureCourseNotLockedAsync(request.CourseId, cancellationToken);
        }

        var course = await _unitOfWork.CourseRepository.GetByIdAsync(request.CourseId, cancellationToken);
        if (course == null || course.IsDeleted)
            throw new KeyNotFoundException($"Course with ID {request.CourseId} not found.");

        var subject = await _unitOfWork.SubjectRepository.GetByIdAsync(request.SubjectId, cancellationToken);
        if (subject == null || subject.IsDeleted)
            throw new KeyNotFoundException($"Subject with ID {request.SubjectId} not found.");

        var isSubjectInCourse = _unitOfWork.CourseSubjectRepository.GetQueryable()
            .Any(cs => cs.CourseId == request.CourseId && cs.SubjectId == request.SubjectId && !cs.IsDeleted);
        if (!isSubjectInCourse)
        {
            throw new System.ComponentModel.DataAnnotations.ValidationException($"Môn học #{request.SubjectId} ({subject.SubjectCode}) không thuộc về Khóa học #{request.CourseId} ({course.CourseCode}).");
        }

        var entity = new Assessment
        {
            CourseId = request.CourseId,
            SubjectId = request.SubjectId,
            ComponentName = request.ComponentName,
            AssessmentType = request.AssessmentType,
            Weight = request.Weight,
            PassingScore = request.PassingScore,
            IsRequired = request.IsRequired,
            DisplayOrder = request.DisplayOrder,
            CreatedByAccountId = createdByAccountId
        };

        await _unitOfWork.AssessmentRepository!.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return MapToResponse(entity);
    }

    public async Task<AssessmentResponse> UpdateAssessmentAsync(int id, UpdateAssessmentRequest request, int updatedByAccountId, CancellationToken cancellationToken = default)
    {
        var item = await _unitOfWork.AssessmentRepository.GetByIdAsync(id, cancellationToken);
        if (item == null) throw new KeyNotFoundException("Assessment not found.");

        var allResults = _unitOfWork.AssessmentResultRepository != null
            ? await _unitOfWork.AssessmentResultRepository.GetAllAsync(cancellationToken)
            : null;
        var hasRecordedResults = (allResults ?? Enumerable.Empty<AssessmentResult>())
            .Any(ar => ar.AssessmentId == id && !ar.IsDeleted);

        if (hasRecordedResults)
        {
            throw new BusinessRuleViolationException(
                $"Bài kiểm tra '{item.ComponentName}' đã có kết quả điểm số của học viên nên không thể sửa đổi cấu hình.");
        }

        await _courseService.EnsureCourseNotLockedAsync(item.CourseId, cancellationToken);

        var subject = await _unitOfWork.SubjectRepository.GetByIdAsync(request.SubjectId, cancellationToken);
        if (subject == null || subject.IsDeleted)
            throw new KeyNotFoundException($"Subject with ID {request.SubjectId} not found.");

        var isSubjectInCourse = _unitOfWork.CourseSubjectRepository.GetQueryable()
            .Any(cs => cs.CourseId == item.CourseId && cs.SubjectId == request.SubjectId && !cs.IsDeleted);
        if (!isSubjectInCourse)
        {
            throw new System.ComponentModel.DataAnnotations.ValidationException($"Môn học #{request.SubjectId} ({subject.SubjectCode}) không thuộc về Khóa học #{item.CourseId}.");
        }

        item.SubjectId = request.SubjectId;
        item.ComponentName = request.ComponentName;
        item.AssessmentType = request.AssessmentType;
        item.Weight = request.Weight;
        item.PassingScore = request.PassingScore;
        item.IsRequired = request.IsRequired;
        item.DisplayOrder = request.DisplayOrder;
        item.UpdatedByAccountId = updatedByAccountId;

        _unitOfWork.AssessmentRepository.Update(item);
        await _unitOfWork.SaveAsync(cancellationToken);

        return MapToResponse(item);
    }

    public async Task DeleteAssessmentAsync(int id, int deletedByAccountId, CancellationToken cancellationToken = default)
    {
        var item = await _unitOfWork.AssessmentRepository.GetByIdAsync(id, cancellationToken);
        if (item == null) throw new KeyNotFoundException("Assessment not found.");

        var allResults = _unitOfWork.AssessmentResultRepository != null
            ? await _unitOfWork.AssessmentResultRepository.GetAllAsync(cancellationToken)
            : null;
        var hasRecordedResults = (allResults ?? Enumerable.Empty<AssessmentResult>())
            .Any(ar => ar.AssessmentId == id && !ar.IsDeleted);

        if (hasRecordedResults)
        {
            throw new BusinessRuleViolationException(
                $"Bài kiểm tra '{item.ComponentName}' đã có kết quả điểm số của học viên nên không thể xóa.");
        }

        await _courseService.EnsureCourseNotLockedAsync(item.CourseId, cancellationToken);

        _unitOfWork.AssessmentRepository.Delete(item);
        await _unitOfWork.SaveAsync(cancellationToken);
    }

    private static AssessmentResponse MapToResponse(Assessment entity)
    {
        return new AssessmentResponse(
            entity.AssessmentId,
            entity.CourseId,
            entity.SubjectId,
            entity.ComponentName,
            entity.AssessmentType,
            entity.Weight,
            entity.PassingScore,
            entity.IsRequired,
            entity.DisplayOrder
        );
    }
}
