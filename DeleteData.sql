-- Delete data from BookingChannels, ConsentRecords, and Subscriptions tables
-- Database: db71296

USE db71296;
GO

-- Delete all records from BookingChannels
DELETE FROM dbo.BookingChannels;
GO

-- Delete all records from ConsentRecords
DELETE FROM dbo.ConsentRecords;
GO

-- Delete all records from Subscriptions
DELETE FROM dbo.Subscriptions;
GO

PRINT 'Data deleted successfully from BookingChannels, ConsentRecords, and Subscriptions';
