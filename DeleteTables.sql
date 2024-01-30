USE LeiloesBD

-- Drop the Payment table
IF OBJECT_ID('Payment', 'U') IS NOT NULL
    DROP TABLE Payment;

-- Drop the Product table
IF OBJECT_ID('Product', 'U') IS NOT NULL
    DROP TABLE Product;

-- Drop the Auction table
IF OBJECT_ID('Auction', 'U') IS NOT NULL
    DROP TABLE Auction;

-- Drop the Bid table
IF OBJECT_ID('Bid', 'U') IS NOT NULL
    DROP TABLE Bid;

-- Drop the Client table
IF OBJECT_ID('Client', 'U') IS NOT NULL
    DROP TABLE Client;
