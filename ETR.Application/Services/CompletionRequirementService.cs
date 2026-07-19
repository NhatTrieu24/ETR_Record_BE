using ETR.Application.DTOs.CompletionRequirement;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;

namespace ETR.Application.Services;

public class CompletionRequirementService : ICompletionRequirementService
{
    private readonly IUnitOfWork _unitOfWork;

    public CompletionRequirementService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<CompletionRequirementResponse>> GetAllCompletionRequirementsAsync(CancellationToken cancellationToken = default)
    {
        var items = await _unitOfWork.CompletionRequirementRepository.GetAllAsync(cancellationToken);
        return items.Select(MapToResponse);
    }

    public async Task<CompletionRequirementResponse> GetCompletionRequirementByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = await _unitOfWork.CompletionRequirementRepository.GetByIdAsync(id, cancellationToken);
        if (item == null) throw new KeyNotFoundException("CompletionRequirement not found.");

        return MapToResponse(item);
    }

    public async Task<IEnumerable<CompletionRequirementResponse>> GetCompletionRequirementsByCourseAsync(int courseId, CancellationToken cancellationToken = default)
    {
        var items = await _unitOfWork.CompletionRequirementRepository.GetAllAsync(cancellationToken);
        return items.Where(x => x.CourseId == courseId).Select(MapToResponse);
    }

    public async Task<CompletionRequirementResponse> CreateCompletionRequirementAsync(CreateCompletionRequirementRequest request, int createdByAccountId, CancellationToken cancellationToken = default)
    {
        var course = await _unitOfWork.CourseRepository.GetByIdAsync(request.CourseId, cancellationToken);
        if (course == null) throw new KeyNotFoundException("Course not found.");

        var item = new CompletionRequirement
        {
            CourseId = request.CourseId,
            RequirementName = request.RequirementName,
            Description = request.Description,
            IsMandatory = request.IsMandatory,
            DisplayOrder = request.DisplayOrder,
            CreatedAt = DateTime.UtcNow,
            CreatedByAccountId = createdByAccountId
        };

        await _unitOfWork.CompletionRequirementRepository.AddAsync(item, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return MapToResponse(item);
    }

    public async Task<CompletionRequirementResponse> UpdateCompletionRequirementAsync(int id, UpdateCompletionRequirementRequest request, int updatedByAccountId, CancellationToken cancellationToken = default)
    {
        var item = await _unitOfWork.CompletionRequirementRepository.GetByIdAsync(id, cancellationToken);
        if (item == null) throw new KeyNotFoundException("CompletionRequirement not found.");

        item.RequirementName = request.RequirementName;
        item.Description = request.Description;
        item.IsMandatory = request.IsMandatory;
        item.DisplayOrder = request.DisplayOrder;
        item.UpdatedAt = DateTime.UtcNow;
        item.UpdatedByAccountId = updatedByAccountId;

        _unitOfWork.CompletionRequirementRepository.Update(item);
        await _unitOfWork.SaveAsync(cancellationToken);

        return MapToResponse(item);
    }

    public async Task DeleteCompletionRequirementAsync(int id, int deletedByAccountId, CancellationToken cancellationToken = default)
    {
        var item = await _unitOfWork.CompletionRequirementRepository.GetByIdAsync(id, cancellationToken);
        if (item == null) throw new KeyNotFoundException("CompletionRequirement not found.");

        item.IsDeleted = true;
        item.DeletedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;
        item.UpdatedByAccountId = deletedByAccountId;

        _unitOfWork.CompletionRequirementRepository.Update(item);
        await _unitOfWork.SaveAsync(cancellationToken);
    }

    private static CompletionRequirementResponse MapToResponse(CompletionRequirement entity)
    {
        return new CompletionRequirementResponse
        {
            RequirementId = entity.RequirementId,
            CourseId = entity.CourseId,
            RequirementName = entity.RequirementName,
            Description = entity.Description,
            IsMandatory = entity.IsMandatory,
            DisplayOrder = entity.DisplayOrder
        };
    }
}
