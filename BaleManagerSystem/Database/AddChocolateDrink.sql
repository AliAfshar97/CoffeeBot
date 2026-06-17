-- Add standalone Chocolate drink (no shot / add-on options)

USE CoffeeBotDB;
GO

IF NOT EXISTS (
    SELECT 1 FROM CoffeePrices
    WHERE DrinkType = N'Chocolate' AND ShotCount = 1 AND WithChocolate = 0)
BEGIN
    INSERT INTO CoffeePrices (DrinkType, ShotCount, WithChocolate, PriceInToman)
    VALUES (N'Chocolate', 1, 0, 15);
END
GO
