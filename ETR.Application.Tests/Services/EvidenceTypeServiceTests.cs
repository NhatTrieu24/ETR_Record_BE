using ETR.Application.Compliance;
using ETR.Application.DTOs.EvidenceType;
using ETR.Application.Interfaces;
using ETR.Application.Services;
using ETR.Domain.Entities;
using Moq;
using Xunit;

namespace ETR.Application.Tests.Services;

public class EvidenceTypeServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IGenericRepository<EvidenceType>> _mockEvidenceTypeRepo;
    private readonly Mock<IGenericRepository<EvidenceFile>> _mockEvidenceFileRepo;
    private readonly EvidenceTypeService _service;

    public EvidenceTypeServiceTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockEvidenceTypeRepo = new Mock<IGenericRepository<EvidenceType>>();
        _mockEvidenceFileRepo = new Mock<IGenericRepository<EvidenceFile>>();

        _mockUow.Setup(u => u.EvidenceTypeRepository).Returns(_mockEvidenceTypeRepo.Object);
        _mockUow.Setup(u => u.EvidenceFileRepository).Returns(_mockEvidenceFileRepo.Object);

        _service = new EvidenceTypeService(_mockUow.Object);
    }

    [Fact]
    public async Task GetAllEvidenceTypesAsync_ShouldMapAllScopeFieldsAndTypeCode()
    {
        var sampleTypes = new List<EvidenceType>
        {
            new EvidenceType
            {
                EvidenceTypeId = 1,
                TypeCode = "AML",
                TypeName = "Aircraft Maintenance Log (AML)",
                Description = "Aviation line maintenance log",
                DepartmentScope = "ENG",
                SubjectTypeScope = "Practical",
                IsMandatory = false,
                Category = "SubjectEvidence"
            },
            new EvidenceType
            {
                EvidenceTypeId = 4,
                TypeCode = "MED_ELP",
                TypeName = "Medical & English Proficiency",
                Description = "Confidential aviation health credential",
                DepartmentScope = "ALL",
                SubjectTypeScope = "ALL",
                IsMandatory = false,
                Category = "Credential"
            }
        };

        _mockEvidenceTypeRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(sampleTypes);

        var result = (await _service.GetAllEvidenceTypesAsync()).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("AML", result[0].TypeCode);
        Assert.Equal("ENG", result[0].DepartmentScope);
        Assert.Equal("Practical", result[0].SubjectTypeScope);
        Assert.False(result[0].IsMandatory);
        Assert.Equal("SubjectEvidence", result[0].Category);

        Assert.Equal("MED_ELP", result[1].TypeCode);
        Assert.Equal("Credential", result[1].Category);
    }

    [Fact]
    public async Task CreateEvidenceTypeAsync_ShouldThrowBusinessRuleViolation_WhenTypeCodeAlreadyExists()
    {
        var existingTypes = new List<EvidenceType>
        {
            new EvidenceType { EvidenceTypeId = 1, TypeCode = "SIM_LOG", TypeName = "Flight Simulator Session Log" }
        };
        _mockEvidenceTypeRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTypes);

        var request = new CreateEvidenceTypeRequest
        {
            TypeCode = "SIM_LOG",
            TypeName = "New Simulator Log Name",
            DepartmentScope = "FC",
            SubjectTypeScope = "Practical"
        };

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _service.CreateEvidenceTypeAsync(request, createdByAccountId: 1));

        Assert.Contains("SIM_LOG", ex.Message);
    }

    [Fact]
    public async Task CreateEvidenceTypeAsync_ShouldThrowBusinessRuleViolation_WhenTypeNameAlreadyExists()
    {
        var existingTypes = new List<EvidenceType>
        {
            new EvidenceType { EvidenceTypeId = 1, TypeCode = "AML", TypeName = "Aircraft Maintenance Log (AML)" }
        };
        _mockEvidenceTypeRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTypes);

        var request = new CreateEvidenceTypeRequest
        {
            TypeCode = "AML_CUSTOM",
            TypeName = "Aircraft Maintenance Log (AML)",
            DepartmentScope = "ENG",
            SubjectTypeScope = "Practical"
        };

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _service.CreateEvidenceTypeAsync(request, createdByAccountId: 1));

        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public async Task CreateEvidenceTypeAsync_ShouldPersistAllFields_WhenValid()
    {
        _mockEvidenceTypeRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EvidenceType>());

        EvidenceType? saved = null;
        _mockEvidenceTypeRepo.Setup(r => r.AddAsync(It.IsAny<EvidenceType>(), It.IsAny<CancellationToken>()))
            .Callback<EvidenceType, CancellationToken>((et, _) => saved = et)
            .Returns(Task.CompletedTask);

        var request = new CreateEvidenceTypeRequest
        {
            TypeCode = "OJT_LOG",
            TypeName = "Practical OJT Task Sign-off Sheet",
            Description = "On-the-job training sign-off",
            DepartmentScope = "ENG",
            SubjectTypeScope = "Practical",
            IsMandatory = false,
            Category = "SubjectEvidence"
        };

        var response = await _service.CreateEvidenceTypeAsync(request, createdByAccountId: 5);

        Assert.NotNull(saved);
        Assert.Equal("OJT_LOG", saved!.TypeCode);
        Assert.Equal("Practical OJT Task Sign-off Sheet", saved.TypeName);
        Assert.Equal("ENG", saved.DepartmentScope);
        Assert.Equal("Practical", saved.SubjectTypeScope);
        Assert.False(saved.IsMandatory);
        Assert.Equal("SubjectEvidence", saved.Category);
        Assert.Equal(5, saved.CreatedByAccountId);

        _mockUow.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateEvidenceTypeAsync_ShouldThrowBusinessRuleViolation_WhenTypeCodeConflicts()
    {
        var existingTypes = new List<EvidenceType>
        {
            new EvidenceType { EvidenceTypeId = 1, TypeCode = "AML", TypeName = "AML" },
            new EvidenceType { EvidenceTypeId = 2, TypeCode = "SIM_LOG", TypeName = "SIM" }
        };

        _mockEvidenceTypeRepo.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTypes[1]);
        _mockEvidenceTypeRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTypes);

        var request = new UpdateEvidenceTypeRequest
        {
            TypeCode = "AML", // Conflict with ID 1
            TypeName = "Updated SIM",
            DepartmentScope = "FC",
            SubjectTypeScope = "Practical"
        };

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _service.UpdateEvidenceTypeAsync(2, request, updatedByAccountId: 1));

        Assert.Contains("AML", ex.Message);
    }
}
