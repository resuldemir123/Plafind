-- Force add Amount column (ignore if exists)
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Reservations' AND COLUMN_NAME = 'Amount')
BEGIN
    ALTER TABLE [Reservations] ADD [Amount] decimal(18,2) NULL;
    PRINT 'Amount column added';
END
ELSE
BEGIN
    PRINT 'Amount column already exists';
END
GO

-- Force add BranchId column (ignore if exists)
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Reservations' AND COLUMN_NAME = 'BranchId')
BEGIN
    ALTER TABLE [Reservations] ADD [BranchId] int NULL;
    PRINT 'BranchId column added';
END
ELSE
BEGIN
    PRINT 'BranchId column already exists';
END
GO

-- Verify columns exist
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Reservations' 
AND COLUMN_NAME IN ('Amount', 'BranchId')
ORDER BY COLUMN_NAME;
GO
