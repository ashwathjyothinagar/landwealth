# ADR 0004: Secure Document Storage Strategy & Path Traversal Prevention

## Context & Problem Statement
Land management heavily involves high-resolution legal and survey documents: Sale Deeds, Mutation certificates, Encumbrance Certificates (EC), RTC/Pahani records, and surveyor sketches. These documents contain confidential government IDs, personal addresses, and financial purchase prices. We must design a document storage strategy that:
1. Prevents path traversal vulnerabilities (`../../etc/passwd` or arbitrary disk writes).
2. Protects against malicious file upload attacks (executable payloads masquerading as PDFs).
3. Obfuscates file system locations completely from clients.
4. Allows seamless migration from local disk storage to cloud object storage (S3 / Azure Blob) in the future.

## Options Considered

### Option A: Database BLOB Storage (Storing raw binary bytes in MySQL `LONGBLOB`)
- **Pros**: Backup consistency tied directly to database dumps.
- **Cons**: Massive database bloat, poor query performance, high memory consumption in EF Core, slow backup/restore times.

### Option B: Direct File System Storage with User Filenames
- Storing files in `/uploads/{propertyId}/{originalFilename}`.
- **Pros**: Human readable on disk.
- **Cons**: Severe path traversal risks, unicode/special character filesystem bugs, race conditions with duplicate names, and exposing storage details to clients.

### Option C: Storage Provider Abstraction with Cryptographic GUID Keys (Selected)
- Files are saved to an isolated storage root (configurable via `IFileStorageService`).
- Files on disk or in bucket are renamed to a deterministic GUID storage key (e.g., `a7f9b4...d12.bin` or partitioned `/documents/{year}/{month}/{guid}`).
- Only the original filename (sanitized) and MIME type are kept in the database for client download representation (`Content-Disposition: attachment; filename="SaleDeed_Chitnahalli.pdf"`).
- Strict validation:
  1. Whitelisted file extensions: `.pdf`, `.jpg`, `.jpeg`, `.png`, `.webp`.
  2. Maximum size threshold: 25 MB per document.
  3. Magic-byte signature verification (e.g., verifying `%PDF-` header for PDFs).

## Decision
We select **Option C: Storage Provider Abstraction with Cryptographic GUID Keys and Magic-Byte Validation**.

## Consequences
- **Positive**:
  - Zero path traversal risk: the user has zero influence over the storage path on disk.
  - Zero executable execution risk: web server will never serve uploads as executable scripts.
  - Easy abstraction: swapping local disk storage for AWS S3 or Azure Blob requires only writing a new `IFileStorageService` implementation.
- **Negative**:
  - Administrators browsing the raw filesystem cannot identify files by original title without consulting the database.
