using ETR.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ETR.Infrastructure.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        var now = DateTime.UtcNow;

        // 1. Seed Base Tables (Departments, Roles, LearnerTypes)
        if (!await context.Departments.AnyAsync())
        {
            context.Departments.Add(new Department { DepartmentName = "IT", Description = "Information Technology", CreatedAt = now });
            context.Departments.Add(new Department { DepartmentName = "HR", Description = "Human Resources", CreatedAt = now });
            await context.SaveChangesAsync();
        }

        if (!await context.Roles.AnyAsync())
        {
            context.Roles.Add(new Role { RoleName = "Admin", Description = "System Administrator", CreatedAt = now });
            context.Roles.Add(new Role { RoleName = "Instructor", Description = "Course Instructor", CreatedAt = now });
            await context.SaveChangesAsync();
        }

        if (!await context.LearnerTypes.AnyAsync())
        {
            context.LearnerTypes.Add(new LearnerType { TypeName = "Internal", Description = "Internal Employee", CreatedAt = now });
            context.LearnerTypes.Add(new LearnerType { TypeName = "External", Description = "External Contractor", CreatedAt = now });
            await context.SaveChangesAsync();
        }

        // 2. Seed Users and Learners
        if (!await context.Users.AnyAsync())
        {
            var adminRole = await context.Roles.FirstAsync(r => r.RoleName == "Admin");
            var itDept = await context.Departments.FirstAsync(d => d.DepartmentName == "IT");

            context.Users.Add(new User
            {
                Username = "admin",
                PasswordHash = "123456", // Mock plain text for testing only
                FullName = "System Administrator",
                Email = "admin@example.com",
                IsActive = true,
                RoleId = adminRole.RoleId,
                DepartmentId = itDept.DepartmentId,
                CreatedAt = now
            });
            await context.SaveChangesAsync();
        }

        if (!await context.Learners.AnyAsync())
        {
            var internalType = await context.LearnerTypes.FirstAsync(l => l.TypeName == "Internal");

            context.Learners.Add(new Learner
            {
                LearnerCode = "L-001",
                FullName = "John Doe",
                DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Gender = "Male",
                Email = "john.doe@example.com",
                Status = "Active",
                LearnerTypeId = internalType.LearnerTypeId,
                CreatedAt = now
            });
            await context.SaveChangesAsync();
        }

        // 3. Seed Course, Subjects, and CourseSubjects
        if (!await context.Courses.AnyAsync())
        {
            context.Courses.Add(new Course
            {
                CourseCode = "C-NET-101",
                CourseName = ".NET Backend Mastery",
                Description = "A complete guide to .NET backend development.",
                DurationHours = 120,
                Status = "Active",
                CreatedAt = now
            });
            await context.SaveChangesAsync();
        }

        if (!await context.Subjects.AnyAsync())
        {
            context.Subjects.AddRange(
                new Subject { SubjectCode = "SUB-CS", SubjectName = "C# Fundamentals", SubjectType = "Theory", DefaultHours = 40, Status = "Active", CreatedAt = now },
                new Subject { SubjectCode = "SUB-EF", SubjectName = "Entity Framework Core", SubjectType = "Theory", DefaultHours = 40, Status = "Active", CreatedAt = now },
                new Subject { SubjectCode = "SUB-API", SubjectName = "Web API Development", SubjectType = "Practical", DefaultHours = 40, Status = "Active", CreatedAt = now }
            );
            await context.SaveChangesAsync();
        }

        if (!await context.CourseSubjects.AnyAsync())
        {
            var course = await context.Courses.FirstAsync(c => c.CourseCode == "C-NET-101");
            var subjects = await context.Subjects.ToListAsync();

            int seq = 1;
            foreach (var subject in subjects)
            {
                context.CourseSubjects.Add(new CourseSubject
                {
                    CourseId = course.CourseId,
                    SubjectId = subject.SubjectId,
                    SequenceNo = seq++,
                    RequiredHours = subject.DefaultHours,
                    PassingScore = 50.0m,
                    IsMandatory = true,
                    CreatedAt = now
                });
            }
            await context.SaveChangesAsync();
        }

        // 4. Seed Classes
        if (!await context.Classes.AnyAsync())
        {
            var course = await context.Courses.FirstAsync(c => c.CourseCode == "C-NET-101");

            context.Classes.Add(new Class
            {
                ClassCode = "CLS-2026",
                ClassName = ".NET Summer Bootcamp",
                CourseId = course.CourseId,
                StartDate = now.AddDays(7),
                EndDate = now.AddDays(37),
                Capacity = 30,
                Status = "Scheduled",
                CreatedAt = now
            });
            await context.SaveChangesAsync();
        }
    }
}
