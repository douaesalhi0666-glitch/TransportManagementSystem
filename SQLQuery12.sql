-- Remove IDENTITY from Driver_tbl
DECLARE @SQL2 NVARCHAR(MAX) = ''

SELECT @SQL2 = @SQL2 + 'ALTER TABLE [' + OBJECT_SCHEMA_NAME(parent_object_id) + '].[' + OBJECT_NAME(parent_object_id) + '] DROP CONSTRAINT [' + name + '];' + CHAR(13)
FROM sys.foreign_keys
WHERE referenced_object_id = OBJECT_ID('[Security].[Driver_tbl]')

EXEC sp_executesql @SQL2

DROP TABLE [Security].[Driver_tbl];

CREATE TABLE [Security].[Driver_tbl](
    Driver_id INT NOT NULL PRIMARY KEY,
    Driver_FirstName NVARCHAR(100) NOT NULL,
    Driver_LastName NVARCHAR(100) NOT NULL,
    Driver_PhoneNumber NVARCHAR(30),
    Driver_Email NVARCHAR(255),
    Driver_LicenseNumber NVARCHAR(50) NOT NULL,
    Driver_LicenseExpiryDate DATE,
    Driver_ExperienceYears INT,
    Driver_Status NVARCHAR(20) DEFAULT 'Available',
    Driver_AssignedBusId INT NULL,
    Driver_PasswordHash NVARCHAR(255) NULL,
    Driver_ResetToken NVARCHAR(255) NULL,
    Driver_ResetTokenExpiry DATETIME NULL,
    Driver_EmailConfirmed BIT DEFAULT 0,
    Driver_CreatedAt DATETIME DEFAULT GETDATE(),
    Driver_UpdatedAt DATETIME DEFAULT GETDATE()
);

ALTER TABLE [Assignment].[DriverBusAssignment_tbl] 
ADD CONSTRAINT FK_DriverBusAssignment_tbl_Driver_tbl 
FOREIGN KEY (DBA_DriverId) REFERENCES [Security].[Driver_tbl](Driver_id);

PRINT 'Driver_tbl recreated successfully WITHOUT IDENTITY!';