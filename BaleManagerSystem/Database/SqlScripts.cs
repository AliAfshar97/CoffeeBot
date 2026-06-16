namespace BaleManagerSystem.Database
{
    public static class SqlScripts
    {
        public const string CreateDatabase = """
            -- Coffee Bot Database Setup
            -- Run this script on your SQL Server instance

            IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'CoffeeBotDB')
            BEGIN
                CREATE DATABASE CoffeeBotDB;
            END
            GO

            USE CoffeeBotDB;
            GO

            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'BotUserTransactions')
            BEGIN
                CREATE TABLE BotUserTransactions
                (
                    Id          INT IDENTITY(1,1) PRIMARY KEY,
                    ChatId      BIGINT NOT NULL UNIQUE,
                    Username    NVARCHAR(100) NULL,
                    DisplayName NVARCHAR(100) NULL,
                    FirstSeen   DATETIME2 NOT NULL DEFAULT GETDATE()
                );
            END
            GO

            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'CoffeeOrders')
            BEGIN
                CREATE TABLE CoffeeOrders
                (
                    Id             INT IDENTITY(1,1) PRIMARY KEY,
                    ChatId         BIGINT NOT NULL,
                    DisplayName    NVARCHAR(100) NOT NULL,
                    DrinkType      NVARCHAR(50) NOT NULL,
                    ShotCount      TINYINT NOT NULL,
                    WithChocolate  BIT NOT NULL DEFAULT 0,
                    CreatedAt      DATETIME2 NOT NULL DEFAULT GETDATE()
                );

                CREATE INDEX IX_CoffeeOrders_ChatId ON CoffeeOrders (ChatId);
                CREATE INDEX IX_CoffeeOrders_CreatedAt ON CoffeeOrders (CreatedAt DESC);
            END
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
            """;
    }
}
