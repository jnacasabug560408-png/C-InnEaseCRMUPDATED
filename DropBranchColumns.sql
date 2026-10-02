-- Drop unnecessary columns from Branches table
-- Columns to remove: ManagerName, CreatedDate, OpenDate

-- Check if columns exist and drop them
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Branches' AND COLUMN_NAME = 'ManagerName')
BEGIN
    ALTER TABLE Branches DROP COLUMN ManagerName;
    PRINT 'ManagerName column dropped successfully.';
END
ELSE
BEGIN
    PRINT 'ManagerName column does not exist.';
END

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Branches' AND COLUMN_NAME = 'CreatedDate')
BEGIN
    -- Drop default constraint if exists
    DECLARE @CreatedDateConstraint NVARCHAR(128)
    SELECT @CreatedDateConstraint = d.name
    FROM sys.default_constraints d
    JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
    WHERE c.object_id = OBJECT_ID('Branches') AND c.name = 'CreatedDate'
    
    IF @CreatedDateConstraint IS NOT NULL
    BEGIN
        EXEC('ALTER TABLE Branches DROP CONSTRAINT ' + @CreatedDateConstraint)
        PRINT 'Default constraint dropped for CreatedDate.'
    END
    
    ALTER TABLE Branches DROP COLUMN CreatedDate;
    PRINT 'CreatedDate column dropped successfully.';
END
ELSE
BEGIN
    PRINT 'CreatedDate column does not exist.';
END

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Branches' AND COLUMN_NAME = 'OpenDate')
BEGIN
    -- Drop default constraint if exists
    DECLARE @OpenDateConstraint NVARCHAR(128)
    SELECT @OpenDateConstraint = d.name
    FROM sys.default_constraints d
    JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
    WHERE c.object_id = OBJECT_ID('Branches') AND c.name = 'OpenDate'
    
    IF @OpenDateConstraint IS NOT NULL
    BEGIN
        EXEC('ALTER TABLE Branches DROP CONSTRAINT ' + @OpenDateConstraint)
        PRINT 'Default constraint dropped for OpenDate.'
    END
    
    ALTER TABLE Branches DROP COLUMN OpenDate;
    PRINT 'OpenDate column dropped successfully.';
END
ELSE
BEGIN
    PRINT 'OpenDate column does not exist.';
END

-- Verify current columns
PRINT '';
PRINT 'Current columns in Branches table:';
SELECT COLUMN_NAME, DATA_TYPE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Branches' 
ORDER BY ORDINAL_POSITION;
