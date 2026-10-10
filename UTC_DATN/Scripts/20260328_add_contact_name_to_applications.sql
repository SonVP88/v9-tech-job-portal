-- Add snapshot contact name for each application
IF COL_LENGTH('dbo.Applications', 'ContactName') IS NULL
BEGIN
    ALTER TABLE dbo.Applications
    ADD ContactName NVARCHAR(200) NULL;
END;
GO

-- Optional backfill for historical rows from linked candidate profile.
-- Keep only rows that are currently empty to avoid overwriting existing snapshots.
UPDATE a
SET a.ContactName = c.FullName
FROM dbo.Applications a
INNER JOIN dbo.Candidates c ON c.CandidateID = a.CandidateID
WHERE (a.ContactName IS NULL OR LTRIM(RTRIM(a.ContactName)) = '')
  AND c.FullName IS NOT NULL
  AND LTRIM(RTRIM(c.FullName)) <> '';
GO
