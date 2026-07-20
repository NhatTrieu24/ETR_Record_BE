using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClassStudentsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public ClassStudentsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var classStudents = await _unitOfWork.ClassStudentRepository.GetAllAsync(cancellationToken);
        return Ok(classStudents);
    }
}
