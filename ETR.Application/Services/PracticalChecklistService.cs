using ETR.Application.DTOs.PracticalChecklist;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;

namespace ETR.Application.Services;

public class PracticalChecklistService : IPracticalChecklistService
{
    private readonly IUnitOfWork _unitOfWork;

    public PracticalChecklistService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<PracticalChecklistResponse>> GetAllPracticalChecklistsAsync(CancellationToken cancellationToken = default)
    {
        var items = await _unitOfWork.PracticalChecklistRepository.GetAllAsync(cancellationToken);
        return items.Select(MapToResponse);
    }

    public async Task<PracticalChecklistResponse> GetPracticalChecklistByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = await _unitOfWork.PracticalChecklistRepository.GetByIdAsync(id, cancellationToken);
        if (item == null) throw new KeyNotFoundException("PracticalChecklist not found.");

        return MapToResponse(item);
    }

    public async Task<IEnumerable<PracticalChecklistResponse>> GetPracticalChecklistsBySubjectAsync(int courseId, int subjectId, CancellationToken cancellationToken = default)
    {
        var items = await _unitOfWork.PracticalChecklistRepository.GetAllAsync(cancellationToken);
        return items.Where(x => x.CourseId == courseId && x.SubjectId == subjectId).Select(MapToResponse);
    }

    public async Task<PracticalChecklistResponse> CreatePracticalChecklistAsync(CreatePracticalChecklistRequest request, int createdByAccountId, CancellationToken cancellationToken = default)
    {
        // Basic validation for course/subject
        var course = await _unitOfWork.CourseRepository.GetByIdAsync(request.CourseId, cancellationToken);
        if (course == null) throw new KeyNotFoundException("Course not found.");
        
        var subject = await _unitOfWork.SubjectRepository.GetByIdAsync(request.SubjectId, cancellationToken);
        if (subject == null) throw new KeyNotFoundException("Subject not found.");

        var item = new PracticalChecklist
        {
            CourseId = request.CourseId,
            SubjectId = request.SubjectId,
            ItemName = request.ItemName,
            Description = request.Description,
            IsRequired = request.IsRequired,
            DisplayOrder = request.DisplayOrder,
            CreatedAt = DateTime.UtcNow,
            CreatedByAccountId = createdByAccountId
        };

        await _unitOfWork.PracticalChecklistRepository.AddAsync(item, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return MapToResponse(item);
    }

    public async Task<PracticalChecklistResponse> UpdatePracticalChecklistAsync(int id, UpdatePracticalChecklistRequest request, int updatedByAccountId, CancellationToken cancellationToken = default)
    {
        var item = await _unitOfWork.PracticalChecklistRepository.GetByIdAsync(id, cancellationToken);
        if (item == null) throw new KeyNotFoundException("PracticalChecklist not found.");

        item.ItemName = request.ItemName;
        item.Description = request.Description;
        item.IsRequired = request.IsRequired;
        item.DisplayOrder = request.DisplayOrder;
        item.UpdatedAt = DateTime.UtcNow;
        item.UpdatedByAccountId = updatedByAccountId;

        _unitOfWork.PracticalChecklistRepository.Update(item);
        await _unitOfWork.SaveAsync(cancellationToken);

        return MapToResponse(item);
    }

    public async Task DeletePracticalChecklistAsync(int id, int deletedByAccountId, CancellationToken cancellationToken = default)
    {
        var item = await _unitOfWork.PracticalChecklistRepository.GetByIdAsync(id, cancellationToken);
        if (item == null) throw new KeyNotFoundException("PracticalChecklist not found.");

        item.IsDeleted = true;
        item.DeletedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;
        item.UpdatedByAccountId = deletedByAccountId;

        _unitOfWork.PracticalChecklistRepository.Update(item);
        await _unitOfWork.SaveAsync(cancellationToken);
    }

    private static PracticalChecklistResponse MapToResponse(PracticalChecklist entity)
    {
        return new PracticalChecklistResponse
        {
            PracticalChecklistId = entity.PracticalChecklistId,
            CourseId = entity.CourseId,
            SubjectId = entity.SubjectId,
            ItemName = entity.ItemName,
            Description = entity.Description,
            IsRequired = entity.IsRequired,
            DisplayOrder = entity.DisplayOrder
        };
    }
}
