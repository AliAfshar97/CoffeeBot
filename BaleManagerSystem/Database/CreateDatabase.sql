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
        Id           INT IDENTITY(1,1) PRIMARY KEY,
        ChatId       BIGINT NOT NULL UNIQUE,
        Username     NVARCHAR(100) NULL,
        DisplayName  NVARCHAR(100) NULL,
        IsSubscriber BIT NOT NULL DEFAULT 0,
        FirstSeen    DATETIME2 NOT NULL DEFAULT GETDATE()
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
        PriceInToman   INT NOT NULL DEFAULT 0,
        CreatedAt      DATETIME2 NOT NULL DEFAULT GETDATE()
    );

    CREATE INDEX IX_CoffeeOrders_ChatId ON CoffeeOrders (ChatId);
    CREATE INDEX IX_CoffeeOrders_CreatedAt ON CoffeeOrders (CreatedAt DESC);
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'CoffeePrices')
BEGIN
    CREATE TABLE CoffeePrices
    (
        Id             INT IDENTITY(1,1) PRIMARY KEY,
        DrinkType      NVARCHAR(50) NOT NULL,
        ShotCount      TINYINT NOT NULL,
        WithChocolate  BIT NOT NULL,
        PriceInToman   INT NOT NULL DEFAULT 0,
        CONSTRAINT UQ_CoffeePrices_Item UNIQUE (DrinkType, ShotCount, WithChocolate)
    );

    INSERT INTO CoffeePrices (DrinkType, ShotCount, WithChocolate, PriceInToman) VALUES
    ('Espresso',   1, 0, 10),
    ('Espresso',   2, 0, 15),
    ('Espresso',   1, 1, 13),
    ('Espresso',   2, 1, 18),
    ('Latte',      1, 0, 12),
    ('Latte',      2, 0, 17),
    ('Latte',      1, 1, 15),
    ('Latte',      2, 1, 20),
    ('Cappuccino', 1, 0, 12),
    ('Cappuccino', 2, 0, 17),
    ('Cappuccino', 1, 1, 15),
    ('Cappuccino', 2, 1, 20),
    ('Milk',       1, 0, 8),
    ('Milk',       2, 0, 10),
    ('Milk',       1, 1, 10),
    ('Milk',       2, 1, 12),
    ('Chocolate',  1, 0, 15);
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'MenuItems')
BEGIN
    CREATE TABLE MenuItems
    (
        Id            INT IDENTITY(1,1) PRIMARY KEY,
        ItemKey       NVARCHAR(50)  NOT NULL UNIQUE,
        NamePersian   NVARCHAR(100) NOT NULL,
        SupportsShots BIT NOT NULL DEFAULT 0,
        Unit          NVARCHAR(20)  NOT NULL DEFAULT N'شات',
        DisplayOrder  INT NOT NULL DEFAULT 0,
        Visibility    INT NOT NULL DEFAULT 0,
        IsActive      BIT NOT NULL DEFAULT 1
    );

    INSERT INTO MenuItems (ItemKey, NamePersian, SupportsShots, Unit, DisplayOrder, IsActive) VALUES
    ('Espresso',   N'اسپرسو',   1, N'شات',   1, 1),
    ('Latte',      N'لاته',     1, N'شات',   2, 1),
    ('Cappuccino', N'کاپوچینو', 1, N'شات',   3, 1),
    ('Milk',       N'شیر',      1, N'لیوان', 4, 1),
    ('Chocolate',  N'شکلات',    0, N'شات',   5, 1);
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

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'AccountLedger')
BEGIN
    CREATE TABLE AccountLedger
    (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        ChatId          BIGINT NOT NULL,
        TransactionType NVARCHAR(10) NOT NULL,
        Amount          INT NOT NULL,
        Description     NVARCHAR(500) NULL,
        OrderId         INT NULL,
        ReceiptId       INT NULL,
        CreatedBy       NVARCHAR(100) NULL,
        CreatedAt       DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT CK_AccountLedger_Type CHECK (TransactionType IN ('Debit', 'Credit'))
    );

    CREATE INDEX IX_AccountLedger_ChatId ON AccountLedger (ChatId);
    CREATE INDEX IX_AccountLedger_CreatedAt ON AccountLedger (CreatedAt DESC);
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'PaymentReceipts')
BEGIN
    CREATE TABLE PaymentReceipts
    (
        Id             INT IDENTITY(1,1) PRIMARY KEY,
        ChatId         BIGINT NOT NULL,
        DisplayName    NVARCHAR(100) NOT NULL,
        TelegramFileId NVARCHAR(200) NOT NULL,
        LocalFilePath  NVARCHAR(500) NULL,
        UserCaption    NVARCHAR(1000) NULL,
        Status         NVARCHAR(20) NOT NULL DEFAULT 'Pending',
        CreditAmount   INT NULL,
        AdminNote      NVARCHAR(500) NULL,
        CreatedAt      DATETIME2 NOT NULL DEFAULT GETDATE(),
        ProcessedAt    DATETIME2 NULL,
        CONSTRAINT CK_PaymentReceipts_Status CHECK (Status IN ('Pending', 'Approved', 'Rejected'))
    );

    CREATE INDEX IX_PaymentReceipts_ChatId ON PaymentReceipts (ChatId);
    CREATE INDEX IX_PaymentReceipts_Status ON PaymentReceipts (Status);
END
GO
