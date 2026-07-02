-- Account ledger and payment receipts for CoffeeBotDB

USE CoffeeBotDB;
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
        LocalFilePath     NVARCHAR(500) NULL,
        ImageContent      VARBINARY(MAX) NULL,
        ImageContentType  NVARCHAR(100) NULL,
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

-- Backfill debit entries from existing orders (run once)
IF NOT EXISTS (SELECT 1 FROM AccountLedger)
BEGIN
    INSERT INTO AccountLedger (ChatId, TransactionType, Amount, Description, OrderId, CreatedAt)
    SELECT
        o.ChatId,
        'Debit',
        COALESCE(NULLIF(o.PriceInToman, 0), p.PriceInToman, 0),
        CONCAT('Order: ', o.DrinkType, ' ', o.ShotCount, ' shot(s)'),
        o.Id,
        o.CreatedAt
    FROM CoffeeOrders o
    LEFT JOIN CoffeePrices p
        ON p.DrinkType = o.DrinkType
       AND p.ShotCount = o.ShotCount
       AND p.WithChocolate = o.WithChocolate;
END
GO
