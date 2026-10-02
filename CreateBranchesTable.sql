-- Check if Branches table exists and create it if not
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Branches')
BEGIN
    CREATE TABLE Branches (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        BranchName NVARCHAR(100) NOT NULL,
        BranchCode NVARCHAR(20) NOT NULL UNIQUE,
        Address NVARCHAR(255),
        City NVARCHAR(100),
        State NVARCHAR(100),
        Country NVARCHAR(100) DEFAULT 'Philippines',
        Phone NVARCHAR(20),
        Email NVARCHAR(100),
        ManagerName NVARCHAR(100),
        IsActive BIT DEFAULT 1,
        CreatedDate DATETIME DEFAULT GETDATE(),
        OpenDate DATETIME
    )
    
    PRINT 'Branches table created successfully.'
END
ELSE
BEGIN
    PRINT 'Branches table already exists.'
END

-- Check if table has data
DECLARE @RowCount INT
SELECT @RowCount = COUNT(*) FROM Branches
PRINT 'Current row count in Branches: ' + CAST(@RowCount AS NVARCHAR(10))
