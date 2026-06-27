-- Add MenuItems table so menu items can be defined from the admin panel.
-- Seeds the five built-in drinks with their existing behaviour.
-- Chocolate is a standalone item (its own row), so there is no chocolate add-on.

USE CoffeeBotDB;
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
        IsActive      BIT NOT NULL DEFAULT 1
    );

    INSERT INTO MenuItems (ItemKey, NamePersian, SupportsShots, Unit, DisplayOrder, IsActive) VALUES
    (N'Espresso',   N'اسپرسو',   1, N'شات',   1, 1),
    (N'Latte',      N'لاته',     1, N'شات',   2, 1),
    (N'Cappuccino', N'کاپوچینو', 1, N'شات',   3, 1),
    (N'Milk',       N'شیر',      1, N'لیوان', 4, 1),
    (N'Chocolate',  N'شکلات',    0, N'شات',   5, 1);
END
GO

-- If an earlier version created the table with a SupportsChocolate column, drop it.
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE Name = N'SupportsChocolate'
      AND Object_ID = Object_ID(N'MenuItems'))
BEGIN
    ALTER TABLE MenuItems DROP COLUMN SupportsChocolate;
END
GO
