# Architecture Refactor Summary

This document serves as the Technical Source of Truth for the July 2026 ETR API Architecture Refactor.

## 1. Executive Summary
The system underwent a major architectural refactor to resolve technical debt, enforce stricter security boundaries (Zero Trust), and align the data models with real-world business domains. 
Key drivers for this refactor included:
*   **Security & Zero Trust**: Moving away from trusting client-provided IDs for sensitive operations, ensuring that actions are strictly tied to the authenticated user's JWT claims.
*   **Decoupling Identity from Demographics**: The legacy system relied on disparate `User` and `Learner` tables for authentication, leading to duplicated logic and fragmented identity management. 
*   **ETR Master-Detail Structure**: Establishing a strict hierarchical relationship between an overarching `ETRCourseRecord` and its granular `SubjectResult` details.

## 2. Schema Overview
The legacy `User` and `Learner` entities have been entirely deprecated and removed. They have been replaced by a unified, 1-to-1 Identity Architecture:
*   **`Account`**: Represents the system-level identity and security boundaries. Contains authentication credentials (`Username`, `PasswordHash`), Authorization mappings (`RoleId`, `DepartmentId`), and system flags (`Status`).
*   **`UserProfile`**: Represents the physical person's demographic information (`FullName`, `Email`, `DateOfBirth`, `LearnerTypeId`). It maintains a strict 1-to-1 Foreign Key mapping (`AccountId`) back to the `Account` table.

**Semantic Foreign Key Approach:**
All audit-related tracking fields across the domain model have been standardized to use the `...ByAccountId` semantic suffix (e.g., `CreatedByAccountId`, `UpdatedByAccountId`, `GradedByAccountId`, `RecordedByAccountId`). This establishes a ubiquitous language across the schema that unequivocally points to the `Account` table.

## 3. Data Migration Guide
Transitioning from the fragmented `User` and `Learner` tables to the unified `Account` table required a careful SQL migration strategy to prevent Primary Key collisions, as both legacy tables utilized auto-incrementing integers starting from 1.
*   **Legacy User Migration**: Existing records from the `User` table were migrated into the `Account` table, retaining their original IDs via `SET IDENTITY_INSERT ON`.
*   **Legacy Learner Migration**: To resolve collisions, existing records from the `Learner` table were migrated into the `Account` table with an **ID offset of 1,000,000** (e.g., Learner ID `14` became Account ID `1000014`). 
*   **Foreign Key Updates**: All historical records (Audit Logs, Enrollments, Assessments) pointing to legacy Learner IDs were dynamically updated to reference the new offset IDs during the migration transaction.

## 4. Service-Layer Changes
*   **`ICurrentUserService.AccountId`**: Services no longer rely on `UserId` or `LearnerId` parameters passed via API Request DTOs. Instead, operations inherently fetch the `AccountId` directly from the authenticated HttpContext claims via `ICurrentUserService`.
*   **ETR Master-Detail Data Loading**: The ETR data retrieval logic (`EtrService.cs`) was refactored to query against the `ETRCourseRecord` (Master) and actively `.Include(etr => etr.SubjectResults)` (Detail). The Entity Framework mapping (`AppDbContext`) was explicitly configured (`.HasOne().WithMany().HasForeignKey()`) to enforce this hierarchical boundary and prevent auto-derivation errors.

## 5. API Contract Stability
Despite profound structural changes to the underlying database and EF Core Entities, the API layer remains fully decoupled via our DTO layer. 
*   The newly created `AccountsController` and `UserProfilesController` provide unified administrative interfaces.
*   The JSON response shapes for demographic data (`UserProfileResponse`) remain completely backward compatible with the shapes expected by the frontend UI (formerly `LearnerResponse` and `UserResponse`), ensuring a seamless transition without breaking the client applications.

## 6. Maintenance Rules
To preserve the integrity of the new architecture, all developers must adhere to the following rules:
> [!IMPORTANT]
> 1. **No Legacy References**: New code must NOT reference the deprecated `User` or `Learner` entities, terminology, or paradigms.
> 2. **Semantic Audit Fields**: All future audit-related fields (creation, modification, approval tracking) must strictly use the `...ByAccountId` suffix.
> 3. **Zero Trust Implementation**: Always use `ICurrentUserService.AccountId` for user identification in Services and Controllers. Never accept a target User ID from the client payload when attempting to identify the *actor* of an operation.
