# ETR (Electronic Training Record) API System

Welcome to the ETR API System repository. This system serves as the robust backend engine for managing Aviation Electronic Training Records, handling everything from course enrollments and practical assessments to secure identity management and automated compliance auditing.

## 🚀 Project Overview

The ETR API is built on a modern **.NET Core** and **Entity Framework Core** stack, utilizing a highly structured, domain-driven architecture. It provides a secure, scalable, and maintainable foundation for tracking learner progress, instructor signoffs, and institutional compliance reporting.

## 🏗️ Architecture Highlights

### Decoupled Identity Architecture
The system employs a unified Identity model, completely separating authentication mechanics from demographic data:
- **`Account`**: Represents the system-level identity boundary (Authentication, Roles, Departments, Status).
- **`UserProfile`**: A strict 1-to-1 extension of the Account that houses demographic and personal data (FullName, Email, DateOfBirth, LearnerType).

### Master-Detail ETR Structure
Data retrieval and state workflows are strictly hierarchical:
- **`ETRCourseRecord`** acts as the overarching Master record linking a learner's enrollment to their final outcomes.
- **`SubjectResult`** details (and their child assessments/practical checklists) are inherently tied to this Master record, ensuring data integrity and enforcing structured loading across the Service Layer.

## 🔒 Security Model

### Zero Trust & JWT Claims
We enforce a **Zero Trust** security posture at the controller and service levels.
- Operations that mutate data **never** trust client-provided User IDs to identify the actor.
- Instead, the identity of the actor is inherently extracted from the authenticated JWT HttpContext claims via the `ICurrentUserService.AccountId`.

### Semantic Audit Keys
To maintain a ubiquitous language and ensure an unbreakable audit trail, all domain entities utilize semantic foreign keys for tracking modifications. 
- You will exclusively see fields such as `CreatedByAccountId`, `UpdatedByAccountId`, `GradedByAccountId`, and `RecordedByAccountId` bridging the gap between operational data and the core Identity tables.

## 🛠️ Getting Started / Local Setup

Follow these steps to get your local development environment up and running:

### 1. Build the Project
Ensure you have the latest .NET SDK installed, then restore and build the solution:
```bash
dotnet build
```

### 2. Reset and Seed the Database
We utilize EF Core Migrations to maintain schema integrity and provide a rich set of localized mock data for development. Apply the latest migrations to scaffold the database and run the `ResetAndSeedSystemData` scripts:
```bash
dotnet ef database update -p ETR.Infrastructure -s ETR.API
```
*(Note: This migration safely wipes existing operational data (excluding history) and reseeds standard Departments, Roles, Admin/Student Accounts, and foundational ETR structures).*

### 3. Run the Application
Boot up the API server:
```bash
dotnet run --project ETR.API
```

## 📚 API Documentation & Swagger

The API is fully documented using OpenAPI (Swagger) specifications.
- Once the application is running, navigate to `https://localhost:<port>/swagger` in your browser.
- Every controller is systematically annotated by **`[Module/Flow]`**, detailing its **`[Core Responsibility]`** and intended **`[Target Audience]`**, allowing developers to understand the business workflows at a glance.

---
*Developed for the CapStone Aviation Training Initiative - 2026*
