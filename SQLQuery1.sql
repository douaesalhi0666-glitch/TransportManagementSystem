-- Add missing columns to Driver_tbl
ALTER TABLE [Security].[Driver_tbl]
ADD Driver_PasswordHash NVARCHAR(255) NULL,
    Driver_ResetToken NVARCHAR(255) NULL,
    Driver_ResetTokenExpiry DATETIME NULL,
    Driver_EmailConfirmed BIT DEFAULT 0;

-- Check if columns were added
SELECT * FROM [Security].[Driver_tbl];