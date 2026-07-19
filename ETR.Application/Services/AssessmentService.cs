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

    public AssessmentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
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

        await _unitOfWork.AssessmentRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return MapToResponse(entity);
    }

    public async Task<AssessmentResponse> UpdateAssessmentAsync(int id, UpdateAssessmentRequest request, int updatedByAccountId, CancellationToken cancellationToken = default)
    {
        var item = await _unitOfWork.AssessmentRepository.GetByIdAsync(id, cancellationToken);
        if (item == null) throw new KeyNotFoundException("Assessment not found.");

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
