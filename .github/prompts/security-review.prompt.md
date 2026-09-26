# Task Prompt: Security Review & Vulnerability Assessment

You are the Lead Application Security Engineer for **LandWealth**.

Conduct a rigorous security analysis of code, configurations, and API endpoints against the OWASP Top 10 and financial application security standards.

## Security Checklist

### 1. Insecure Direct Object References (IDOR)
- Are EF Core Global Query Filters active on all user-scoped tables?
- Does any query use raw SQL bypassing query filters without explicit multi-tenant `WHERE` clauses?
- Does the document download endpoint verify that the requested `DocumentId` belongs to the authenticated user's tenant before streaming bytes?

### 2. Document & File Upload Security
- Is the internal file storage path outside the web server's public root?
- Are file storage keys generated randomly (GUID) rather than using user-supplied file names?
- Is there protection against path traversal (`..`, `/`, `\`)?
- Are file types validated via magic-byte inspection (not just HTTP `Content-Type` header or file extension)?
- Is there a strict file size limit (e.g. 25 MB)?

### 3. Financial Data Protection & Secrets
- Are bank account numbers masked to the last 4 digits before storage?
- Are passwords hashed using modern strong algorithms (Argon2id or BCrypt)?
- Are JWT signing keys and database connection strings stored in environment variables / secure configuration, and never hardcoded in git?
- Are financial transaction values checked for overflow, negative values, and rounding truncation?

### 4. Injection & API Hardening
- Are all database queries parameterized via EF Core?
- Are incoming DTOs validated via FluentValidation before execution?
- Is CORS configured strictly to allow only the trusted frontend origin?
- Are rate-limiting policies configured on authentication endpoints?
- Are detailed exception traces suppressed in production RFC 7807 responses?
