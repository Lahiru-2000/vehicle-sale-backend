-- Performance Optimization: Add indexes for commonly queried vehicle fields
-- Run this migration to improve vehicle query performance

-- Index on Status (most common filter)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Vehicles_Status' AND object_id = OBJECT_ID('Vehicles'))
BEGIN
    CREATE INDEX IX_Vehicles_Status ON Vehicles(Status);
END
GO

-- Index on UserId (for filtering by owner)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Vehicles_UserId' AND object_id = OBJECT_ID('Vehicles'))
BEGIN
    CREATE INDEX IX_Vehicles_UserId ON Vehicles(UserId);
END
GO

-- Index on CreatedAt (for sorting)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Vehicles_CreatedAt' AND object_id = OBJECT_ID('Vehicles'))
BEGIN
    CREATE INDEX IX_Vehicles_CreatedAt ON Vehicles(CreatedAt DESC);
END
GO

-- Composite index for common sorting pattern (CreatedAt, IsPremium, ApprovedAt)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Vehicles_Sorting' AND object_id = OBJECT_ID('Vehicles'))
BEGIN
    CREATE INDEX IX_Vehicles_Sorting ON Vehicles(CreatedAt DESC, IsPremium DESC, ApprovedAt DESC);
END
GO

-- Index on Type (for filtering)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Vehicles_Type' AND object_id = OBJECT_ID('Vehicles'))
BEGIN
    CREATE INDEX IX_Vehicles_Type ON Vehicles(Type);
END
GO

-- Index on Price (for range queries)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Vehicles_Price' AND object_id = OBJECT_ID('Vehicles'))
BEGIN
    CREATE INDEX IX_Vehicles_Price ON Vehicles(Price);
END
GO

-- Index on Year (for range queries)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Vehicles_Year' AND object_id = OBJECT_ID('Vehicles'))
BEGIN
    CREATE INDEX IX_Vehicles_Year ON Vehicles(Year);
END
GO

-- Composite index for Status + CreatedAt (common filter + sort combination)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Vehicles_Status_CreatedAt' AND object_id = OBJECT_ID('Vehicles'))
BEGIN
    CREATE INDEX IX_Vehicles_Status_CreatedAt ON Vehicles(Status, CreatedAt DESC);
END
GO

-- Index on Subscriptions for premium user checks
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Subscriptions_UserId_Status_EndDate' AND object_id = OBJECT_ID('Subscriptions'))
BEGIN
    CREATE INDEX IX_Subscriptions_UserId_Status_EndDate ON Subscriptions(UserId, Status, EndDate);
END
GO

-- Index on Users.Email (for lookups)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_Email' AND object_id = OBJECT_ID('Users'))
BEGIN
    CREATE INDEX IX_Users_Email ON Users(Email);
END
GO

PRINT 'Vehicle performance indexes created successfully!';
GO

