-- Run this if you already created CoffeeBotDB without admin/broadcast tables

USE CoffeeBotDB;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'BotUsers')
BEGIN
    CREATE TABLE BotUsers
    (
        Id          INT IDENTITY(1,1) PRIMARY KEY,
        PhoneNumber NVARCHAR(20) NOT NULL,
        Username    NVARCHAR(100) NULL,
        FirstSeen   DATETIME2 NOT NULL DEFAULT GETDATE()
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'BroadcastLogs')
BEGIN
    CREATE TABLE BroadcastLogs
    (
        Id           INT IDENTITY(1,1) PRIMARY KEY,
        PhoneNumber  NVARCHAR(20) NULL,
        ChatId       BIGINT NULL,
        MessageText  NVARCHAR(MAX) NOT NULL,
        IsSuccess    BIT NOT NULL,
        ErrorMessage NVARCHAR(500) NULL,
        SentAt       DATETIME2 NOT NULL DEFAULT GETDATE()
    );
END
GO
