using ETR.Application.Interfaces;
using ETR.Domain.Entities;

namespace ETR.Application.Services;

public class ChecklistProgressInitializer
{
    private readonly IUnitOfWork _unitOfWork;

    public ChecklistProgressInitializer(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task InitializeForEtrRecordAsync(
        int etrRecordId,
        int classId,
        int createdByUserId,
        CancellationToken cancellationToken = default)
    {
        var trainingClass = await _unitOfWork.TrainingClassRepository.GetByIdAsync(classId, cancellationToken)
            ?? throw new InvalidOperationException($"Training class {classId} was not found.");

        var templates = await _unitOfWork.ETRChecklistTemplateRepository.GetAllAsync(cancellationToken);
        var activeTemplate = templates
            .Where(t => t.CourseId == trainingClass.CourseId && t.IsActive)
            .OrderByDescending(t => t.VersionNo)
            .FirstOrDefault()
            ?? throw new InvalidOperationException(
                $"No active ETR checklist template exists for course {trainingClass.CourseId}.");

        var checklistItems = (await _unitOfWork.ETRChecklistItemRepository.GetAllAsync(cancellationToken))
            .Where(i => i.TemplateId == activeTemplate.TemplateId)
            .OrderBy(i => i.DisplayOrder)
            .ToList();

        if (checklistItems.Count == 0)
        {
            throw new InvalidOperationException(
                $"Active checklist template {activeTemplate.TemplateId} does not contain any items.");
        }

        foreach (var item in checklistItems)
        {
            var progress = new ETRChecklistProgress
            {
                ETRRecordId = etrRecordId,
                ChecklistItemId = item.ChecklistItemId,
                IsCompleted = false,
                CompletedAt = null,
                VerifiedBy = null,
                VerificationComment = null,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdByUserId
            };

            await _unitOfWork.ETRChecklistProgressRepository.AddAsync(progress, cancellationToken);
        }
    }
}
