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
        return sessions.Select(s => MapToResponse(s, assessments.FirstOrDefault(a => a.AssessmentId == s.AssessmentId))).ToList();
    }

    public async Task<IEnumerable<SessionResponse>> GetSessionsByClassIdAsync(int classId, CancellationToken cancellationToken = default)
    {
        var sessions = await _unitOfWork.SessionRepository.GetAllAsync(cancellationToken);
        var classSessions = sessions.Where(s => s.ClassId == classId).ToList();
        var assessments = await _unitOfWork.AssessmentRepository.GetAllAsync(cancellationToken);
        return classSessions.Select(s => MapToResponse(s, assessments.FirstOrDefault(a => a.AssessmentId == s.AssessmentId))).ToList();
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

        return MapToResponse(session, assessment);
    }

    public async Task<SessionResponse> CreateSessionAsync(CreateSessionRequest request, int createdByAccountId, CancellationToken cancellationToken = default)
    {
        // Simple validation as per user requirements
        var classExists = await _unitOfWork.ClassRepository.GetByIdAsync(request.ClassId, cancellationToken);
        if (classExists == null)
            throw new ValidationException($"Class with ID {request.ClassId} does not exist.");

        var subjectExists = await _unitOfWork.SubjectRepository.GetByIdAsync(request.SubjectId, cancellationToken);
        if (subjectExists == null)
            throw new ValidationException($"Subject with ID {request.SubjectId} does not exist.");

        Assessment? assessment = null;
        if (request.AssessmentId.HasValue)
        {
            assessment = await _unitOfWork.AssessmentRepository.GetByIdAsync(request.AssessmentId.Value, cancellationToken);
            if (assessment == null)
                throw new ValidationException($"Assessment with ID {request.AssessmentId.Value} does not exist.");
            if (assessment.CourseId != classExists.CourseId || assessment.SubjectId != request.SubjectId)
                throw new ValidationException("Assessment does not match the class's course or the specified subject.");
        }

        var session = new Session
        {
            ClassId = request.ClassId,
            SubjectId = request.SubjectId,
            SessionTitle = request.SessionTitle,
            SessionDate = request.SessionDate,
            Location = request.Location,
            IsConfirmed = false, // Default value
            IsAssessmentRequired = request.IsAssessmentRequired,
            IsChecklistRequired = request.IsChecklistRequired,
            AssessmentId = request.AssessmentId,
            CreatedAt = DateTime.UtcNow,
            CreatedByAccountId = createdByAccountId
        };

        await _unitOfWork.SessionRepository.AddAsync(session, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return MapToResponse(session, assessment);
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

        session.SessionTitle = request.SessionTitle;
        session.SessionDate = request.SessionDate;
        session.Location = request.Location;
        session.IsAssessmentRequired = request.IsAssessmentRequired;
        session.IsChecklistRequired = request.IsChecklistRequired;
        session.AssessmentId = request.AssessmentId;
        session.UpdatedAt = DateTime.UtcNow;
        session.UpdatedByAccountId = updatedByAccountId;

        _unitOfWork.SessionRepository.Update(session);
        await _unitOfWork.SaveAsync(cancellationToken);

        return MapToResponse(session, assessment);
    }

    public async Task DeleteSessionAsync(int id, int deletedByAccountId, CancellationToken cancellationToken = default)
    {
        var session = await _unitOfWork.SessionRepository.GetByIdAsync(id, cancellationToken);
        if (session == null)
            throw new KeyNotFoundException($"Session with ID {id} not found.");

        // Instead of hard delete, we perform soft delete according to BaseEntity pattern if applicable.
        // Assuming Delete method handles it or we manually set IsDeleted. 
        // Using GenericRepository Delete pattern:
        _unitOfWork.SessionRepository.Delete(session);
        await _unitOfWork.SaveAsync(cancellationToken);
    }

    private SessionResponse MapToResponse(Session session, Assessment? assessment = null)
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
            AssessmentName = assessment?.ComponentName,
            AssessmentType = assessment?.AssessmentType
        };
    }
}
