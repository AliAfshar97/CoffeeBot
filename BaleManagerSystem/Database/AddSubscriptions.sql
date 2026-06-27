-- Adds subscription support:
--   * BotUserTransactions.IsSubscriber  : whether the user has a subscription (set by admin)
--   * MenuItems.Visibility              : which group sees the item
--        0 = both, 1 = subscribers only, 2 = non-subscribers only

USE CoffeeBotDB;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE Name = N'IsSubscriber'
      AND Object_ID = Object_ID(N'BotUserTransactions'))
BEGIN
    ALTER TABLE BotUserTransactions
        ADD IsSubscriber BIT NOT NULL DEFAULT 0;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE Name = N'Visibility'
      AND Object_ID = Object_ID(N'MenuItems'))
BEGIN
    ALTER TABLE MenuItems
        ADD Visibility INT NOT NULL DEFAULT 0;
END
GO
