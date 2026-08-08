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
        // "Current rules for this course" — used by course-setup UI — only the still-effective
        // version of each requirement. Historical (superseded) rows remain queryable via
        // GetAllCompletionRequirementsAsync/GetByIdAsync for audit purposes, just not shown here.
        return items.Where(x => x.CourseId == courseId && x.EffectiveTo == null).Select(MapToResponse);
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
            RequirementType = request.RequirementType,
            ThresholdValue = request.ThresholdValue,
            VersionNo = course.VersionNo,
            EffectiveFrom = DateTime.UtcNow,
            EffectiveTo = null,
            CreatedAt = DateTime.UtcNow,
            CreatedByAccountId = createdByAccountId
        };

        await _unitOfWork.CompletionRequirementRepository.AddAsync(item, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return MapToResponse(item);
    }

    public async Task<CompletionRequirementResponse> UpdateCompletionRequirementAsync(int id, UpdateCompletionRequirementRequest request, int updatedByAccountId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteInStrategyAsync(async (ct) =>
        {
            await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var item = await _unitOfWork.CompletionRequirementRepository.GetByIdAsync(id, ct)
                    ?? throw new KeyNotFoundException("CompletionRequirement not found.");

                // These three fields are what EtrService.SubmitEtrAsync/GetCompletionProgressAsync
                // actually evaluate Pass/Fail against — changing any of them must NOT retroactively
                // reopen the verdict for learners already evaluated under the old rule. Cosmetic
                // fields (name/description/order) are safe to overwrite in place.
                var isOutcomeAffectingChange =
                    item.RequirementType != request.RequirementType ||
                    item.ThresholdValue != request.ThresholdValue ||
                    item.IsMandatory != request.IsMandatory;

                if (!isOutcomeAffectingChange)
                {
                    item.RequirementName = request.RequirementName;
                    item.Description = request.Description;
                    item.DisplayOrder = request.DisplayOrder;
                    item.UpdatedAt = DateTime.UtcNow;
                    item.UpdatedByAccountId = updatedByAccountId;

                    _unitOfWork.CompletionRequirementRepository.Update(item);
                    await _unitOfWork.SaveAsync(ct);
                    await _unitOfWork.CommitTransactionAsync(ct);

                    return MapToResponse(item);
                }

                var course = await _unitOfWork.CourseRepository.GetByIdAsync(item.CourseId, ct)
                    ?? throw new KeyNotFoundException("Course not found.");

                // Close the current row instead of overwriting it — any ETR whose CourseVersionNo
                // still points at item.VersionNo keeps reading this exact (now-historical) row.
                item.EffectiveTo = DateTime.UtcNow;
                item.UpdatedAt = DateTime.UtcNow;
                item.UpdatedByAccountId = updatedByAccountId;
                _unitOfWork.CompletionRequirementRepository.Update(item);

                course.VersionNo += 1;
                course.EffectiveFrom = DateTime.UtcNow;
                course.UpdatedAt = DateTime.UtcNow;
                course.UpdatedByAccountId = updatedByAccountId;
                _unitOfWork.CourseRepository.Update(course);

                var newVersion = new CompletionRequirement
                {
                    CourseId = item.CourseId,
                    RequirementName = request.RequirementName,
                    Description = request.Description,
                    IsMandatory = request.IsMandatory,
                    DisplayOrder = request.DisplayOrder,
                    RequirementType = request.RequirementType,
                    ThresholdValue = request.ThresholdValue,
                    VersionNo = course.VersionNo,
                    EffectiveFrom = DateTime.UtcNow,
                    EffectiveTo = null,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByAccountId = updatedByAccountId
                };
                await _unitOfWork.CompletionRequirementRepository.AddAsync(newVersion, ct);

                await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
                {
                    AccountId = updatedByAccountId,
                    ActionType = "UPDATE",
                    EntityName = nameof(CompletionRequirement),
                    RecordId = id,
                    OldValue = $"RequirementType={item.RequirementType}, ThresholdValue={item.ThresholdValue}, IsMandatory={item.IsMandatory} (VersionNo={item.VersionNo})",
                    NewValue = $"RequirementType={newVersion.RequirementType}, ThresholdValue={newVersion.ThresholdValue}, IsMandatory={newVersion.IsMandatory} (VersionNo={newVersion.VersionNo})",
                    Description = $"CompletionRequirement #{id} superseded by new row (Course #{item.CourseId} VersionNo {item.VersionNo} -> {course.VersionNo}) — outcome-affecting field changed, old row kept for history."
                }, ct);

                await _unitOfWork.SaveAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                // Deliberate: returns the NEW row (new RequirementId), not the one the caller PUT
                // to — this endpoint now behaves as "create the next version" rather than "overwrite
                // in place" whenever an outcome-affecting field changes. See maintain doc for the
                // FE-facing implication (the id in the response may differ from the id in the URL).
                return MapToResponse(newVersion);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }, cancellationToken);
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
            DisplayOrder = entity.DisplayOrder,
            RequirementType = entity.RequirementType,
            ThresholdValue = entity.ThresholdValue,
            VersionNo = entity.VersionNo,
            EffectiveFrom = entity.EffectiveFrom,
            EffectiveTo = entity.EffectiveTo
        };
    }
}
