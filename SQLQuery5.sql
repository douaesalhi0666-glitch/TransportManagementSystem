-- Add missing columns to Personnel_tbl
ALTER TABLE [Security].[Personnel_tbl]
ADD Personnel_PasswordHash NVARCHAR(255) NULL,
    Personnel_ResetToken NVARCHAR(255) NULL,
    Personnel_ResetTokenExpiry DATETIME NULL,
    Personnel_EmailConfirmed BIT DEFAULT 0;

-- Add missing columns to Driver_tbl (if not already added)
ALTER TABLE [Security].[Driver_tbl]
ADD Driver_PasswordHash NVARCHAR(255) NULL,
    Driver_ResetToken NVARCHAR(255) NULL,
    Driver_ResetTokenExpiry DATETIME NULL,
    Driver_EmailConfirmed BIT DEFAULT 0;

PRINT 'Password columns added successfully!';