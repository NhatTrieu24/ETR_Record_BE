using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClassesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public ClassesController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllClasses(CancellationToken cancellationToken)
    {
        var classes = await _unitOfWork.ClassRepository.GetAllAsync(cancellationToken);
        return Ok(classes);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetClass(int id, CancellationToken cancellationToken)
    {
        var cls = await _unitOfWork.ClassRepository.GetByIdAsync(id, cancellationToken);
        if (cls == null) return NotFound();
        return Ok(cls);
    }

    [HttpPost]
    public async Task<IActionResult> CreateClass([FromBody] Class cls, CancellationToken cancellationToken)
    {
        await _unitOfWork.ClassRepository.AddAsync(cls, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);
        return CreatedAtAction(nameof(GetClass), new { id = cls.ClassId }, cls);
    }
}
