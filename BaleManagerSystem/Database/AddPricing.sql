-- Run this if you already have CoffeeBotDB and need pricing support

USE CoffeeBotDB;
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
END
GO

IF COL_LENGTH('CoffeeOrders', 'PriceInToman') IS NULL
BEGIN
    ALTER TABLE CoffeeOrders
    ADD PriceInToman INT NOT NULL CONSTRAINT DF_CoffeeOrders_PriceInToman DEFAULT 0;
END
GO

IF NOT EXISTS (SELECT 1 FROM CoffeePrices)
BEGIN
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
