using ETR.Application.DTOs.Session;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace ETR.Application.Services;

public class SessionService : ISessionService
{
    private readonly IUnitOfWork _unitOfWork;

    public SessionService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<SessionResponse>> GetAllSessionsAsync(CancellationToken cancellationToken = default)
    {
        var sessions = await _unitOfWork.SessionRepository.GetAllAsync(cancellationToken);
        var assessments = await _unitOfWork.AssessmentRepository.GetAllAsync(cancellationToken);
        var checklists = await _unitOfWork.PracticalChecklistRepository.GetAllAsync(cancellationToken);
        return sessions.Select(s => MapToResponse(
            s, 
            assessments.FirstOrDefault(a => a.AssessmentId == s.AssessmentId),
            checklists.FirstOrDefault(c => c.PracticalChecklistId == s.PracticalChecklistId)
        )).ToList();
    }

    public async Task<IEnumerable<SessionResponse>> GetSessionsByClassIdAsync(int classId, CancellationToken cancellationToken = default)
    {
        var sessions = await _unitOfWork.SessionRepository.GetAllAsync(cancellationToken);
        var classSessions = sessions.Where(s => s.ClassId == classId).ToList();
        var assessments = await _unitOfWork.AssessmentRepository.GetAllAsync(cancellationToken);
        var checklists = await _unitOfWork.PracticalChecklistRepository.GetAllAsync(cancellationToken);
        return classSessions.Select(s => MapToResponse(
            s, 
            assessments.FirstOrDefault(a => a.AssessmentId == s.AssessmentId),
            checklists.FirstOrDefault(c => c.PracticalChecklistId == s.PracticalChecklistId)
        )).ToList();
    }

    public async Task<SessionResponse> GetSessionByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var session = await _unitOfWork.SessionRepository.GetByIdAsync(id, cancellationToken);
        if (session == null)
            throw new KeyNotFoundException($"Session with ID {id} not found.");

        Assessment? assessment = null;
        if (session.AssessmentId.HasValue)
        {
            assessment = await _unitOfWork.AssessmentRepository.GetByIdAsync(session.AssessmentId.Value, cancellationToken);
        }

        PracticalChecklist? checklist = null;
        if (session.PracticalChecklistId.HasValue)
        {
            checklist = await _unitOfWork.PracticalChecklistRepository.GetByIdAsync(session.PracticalChecklistId.Value, cancellationToken);
        }

        return MapToResponse(session, assessment, checklist);
    }

    public Task<SessionResponse> CreateSessionAsync(CreateSessionRequest request, int createdByAccountId, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Sessions are auto-provisioned based on Course configuration and cannot be manually created.");
    }

    public async Task<SessionResponse> UpdateSessionAsync(int id, UpdateSessionRequest request, int updatedByAccountId, CancellationToken cancellationToken = default)
    {
        var session = await _unitOfWork.SessionRepository.GetByIdAsync(id, cancellationToken);
        if (session == null)
            throw new KeyNotFoundException($"Session with ID {id} not found.");

        Assessment? assessment = null;
        if (request.AssessmentId.HasValue)
        {
            var classExists = await _unitOfWork.ClassRepository.GetByIdAsync(session.ClassId, cancellationToken);
            assessment = await _unitOfWork.AssessmentRepository.GetByIdAsync(request.AssessmentId.Value, cancellationToken);
            if (assessment == null)
                throw new ValidationException($"Assessment with ID {request.AssessmentId.Value} does not exist.");
            if (assessment.CourseId != classExists?.CourseId || assessment.SubjectId != session.SubjectId)
                throw new ValidationException("Assessment does not match the class's course or the specified subject.");
        }

        PracticalChecklist? checklist = null;
        if (request.PracticalChecklistId.HasValue)
        {
            var classExists = await _unitOfWork.ClassRepository.GetByIdAsync(session.ClassId, cancellationToken);
            checklist = await _unitOfWork.PracticalChecklistRepository.GetByIdAsync(request.PracticalChecklistId.Value, cancellationToken);
            if (checklist == null)
                throw new ValidationException($"PracticalChecklist with ID {request.PracticalChecklistId.Value} does not exist.");
            if (checklist.CourseId != classExists?.CourseId || checklist.SubjectId != session.SubjectId)
                throw new ValidationException("PracticalChecklist does not match the class's course or the specified subject.");
        }

        // Only these fields can be updated by Instructor/Admin
        session.SessionDate = request.SessionDate;
        session.Location = request.Location;
        session.IsAssessmentRequired = request.IsAssessmentRequired;
        session.IsChecklistRequired = request.IsChecklistRequired;
        session.AssessmentId = request.AssessmentId;
        session.PracticalChecklistId = request.PracticalChecklistId;
        session.UpdatedAt = DateTime.UtcNow;
        session.UpdatedByAccountId = updatedByAccountId;

        _unitOfWork.SessionRepository.Update(session);
        await _unitOfWork.SaveAsync(cancellationToken);

        return MapToResponse(session, assessment, checklist);
    }

    public Task DeleteSessionAsync(int id, int deletedByAccountId, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Sessions are auto-provisioned and cannot be manually deleted.");
    }

    private SessionResponse MapToResponse(Session session, Assessment? assessment = null, PracticalChecklist? practicalChecklist = null)
    {
        return new SessionResponse
        {
            SessionId = session.SessionId,
            ClassId = session.ClassId,
            SubjectId = session.SubjectId,
            SessionTitle = session.SessionTitle,
            SessionDate = session.SessionDate,
            Location = session.Location,
            IsConfirmed = session.IsConfirmed,
            ConfirmedByAccountId = session.ConfirmedByAccountId,
            ConfirmedAt = session.ConfirmedAt,
            IsAssessmentRequired = session.IsAssessmentRequired,
            IsChecklistRequired = session.IsChecklistRequired,
            AssessmentId = session.AssessmentId,
            Assessment = assessment != null ? new ETR.Application.DTOs.Assessment.Responses.AssessmentResponse(
                assessment.AssessmentId, assessment.CourseId, assessment.SubjectId, assessment.ComponentName,
                assessment.AssessmentType, assessment.Weight, assessment.PassingScore, assessment.IsRequired,
                assessment.DisplayOrder) : null,
            PracticalChecklistId = session.PracticalChecklistId,
            PracticalChecklist = practicalChecklist != null ? new ETR.Application.DTOs.PracticalChecklist.PracticalChecklistResponse
            {
                PracticalChecklistId = practicalChecklist.PracticalChecklistId,
                CourseId = practicalChecklist.CourseId,
                SubjectId = practicalChecklist.SubjectId,
                ItemName = practicalChecklist.ItemName,
                Description = practicalChecklist.Description,
                IsRequired = practicalChecklist.IsRequired,
                DisplayOrder = practicalChecklist.DisplayOrder
            } : null
        };
    }
}
