-- Add binary image storage columns to PaymentReceipts (existing databases)

USE CoffeeBotDB;
GO

IF COL_LENGTH('PaymentReceipts', 'ImageContent') IS NULL
BEGIN
    ALTER TABLE PaymentReceipts
    ADD ImageContent VARBINARY(MAX) NULL;
END
GO

IF COL_LENGTH('PaymentReceipts', 'ImageContentType') IS NULL
BEGIN
    ALTER TABLE PaymentReceipts
    ADD ImageContentType NVARCHAR(100) NULL;
END
GO
