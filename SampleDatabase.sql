-- Example SQL Scripts for Testing DBAccess.Api

-- ============================================
-- Create Sample Database
-- ============================================
CREATE DATABASE SampleDB;
GO

USE SampleDB;
GO

-- ============================================
-- Create Sample Table
-- ============================================
CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) NOT NULL,
    Status NVARCHAR(50) DEFAULT 'Active',
    CreatedDate DATETIME DEFAULT GETDATE()
);
GO

-- Insert Sample Data
INSERT INTO Users (Name, Email, Status, CreatedDate)
VALUES 
    ('John Doe', 'john@example.com', 'Active', '2024-01-15'),
    ('Jane Smith', 'jane@example.com', 'Active', '2024-01-20'),
    ('Bob Johnson', 'bob@example.com', 'Inactive', '2023-12-10'),
    ('Alice Williams', 'alice@example.com', 'Active', '2024-02-01'),
    ('Charlie Brown', 'charlie@example.com', 'Active', '2024-01-25');
GO

-- ============================================
-- Create Sample Stored Procedure
-- ============================================
CREATE PROCEDURE sp_GetUsers
    @UserId INT = NULL,
    @Status NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        Id,
        Name,
        Email,
        Status,
        CreatedDate
    FROM Users
    WHERE 
        (@UserId IS NULL OR Id = @UserId)
        AND (@Status IS NULL OR Status = @Status)
    ORDER BY CreatedDate DESC;
END
GO

-- Test the stored procedure
EXEC sp_GetUsers @Status = 'Active';
GO

-- ============================================
-- Create Sample Scalar Function
-- ============================================
CREATE FUNCTION fn_CalculateTotal
(
    @Amount DECIMAL(18,2),
    @TaxRate DECIMAL(5,2)
)
RETURNS DECIMAL(18,2)
AS
BEGIN
    DECLARE @Total DECIMAL(18,2);
    SET @Total = @Amount * (1 + @TaxRate);
    RETURN @Total;
END
GO

-- Test the function
SELECT dbo.fn_CalculateTotal(100, 0.15) AS Total;
GO

-- ============================================
-- Create Sample Table-Valued Function
-- ============================================
CREATE FUNCTION fn_GetActiveUsers()
RETURNS TABLE
AS
RETURN
(
    SELECT 
        Id,
        Name,
        Email,
        Status,
        CreatedDate
    FROM Users
    WHERE Status = 'Active'
);
GO

-- Test the table-valued function
SELECT * FROM dbo.fn_GetActiveUsers();
GO

-- ============================================
-- Create More Complex Stored Procedure
-- ============================================
CREATE PROCEDURE sp_GetUserStatistics
    @StartDate DATETIME = NULL,
    @EndDate DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        Status,
        COUNT(*) AS UserCount,
        MIN(CreatedDate) AS FirstCreated,
        MAX(CreatedDate) AS LastCreated
    FROM Users
    WHERE 
        (@StartDate IS NULL OR CreatedDate >= @StartDate)
        AND (@EndDate IS NULL OR CreatedDate <= @EndDate)
    GROUP BY Status;
END
GO

-- Test the statistics procedure
EXEC sp_GetUserStatistics @StartDate = '2024-01-01', @EndDate = '2024-12-31';
GO
