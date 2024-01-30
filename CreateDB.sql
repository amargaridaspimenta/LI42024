-- Check if the database exists
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'LeiloesBD')
BEGIN
    -- Create the database
    CREATE DATABASE LeiloesBD;
END

-- Use the database
USE LeiloesBD;

CREATE TABLE [Auction] (
    [AuctionId]  INT IDENTITY(1,1) NOT NULL,
    [Name]        VARCHAR(MAX)     NOT NULL,
    [Price]       INT              NOT NULL,
    [AuctionTime] INT              NOT NULL,
    [StartTime]   DATETIME         NOT NULL,
    [EndTime]     DATETIME         NOT NULL,
    [Description] VARCHAR(MAX)     NOT NULL,
    [MinimumBid]  INT              NOT NULL,
    [ClientId]    INT              NOT NULL,
    [Licitacao]   INT              NOT NULL,
    [IsAuctionEnded] AS (CASE WHEN GETDATE() > [EndTime] THEN 1 ELSE 0 END), -- Coluna calculada
    PRIMARY KEY CLUSTERED ([AuctionId] ASC)
);

CREATE TABLE [Bid] (
    [BidId]     INT IDENTITY(1,1) NOT NULL,
    [Value]     INT               NOT NULL,
    [Status]    VARCHAR (MAX)     NOT NULL,
    [Time]      DATETIME          NOT NULL,
    [ClientId]  INT               NOT NULL,
    [AuctionID] INT               NOT NULL,
    PRIMARY KEY CLUSTERED ([BidId] ASC)
);

CREATE TABLE [Client] (
    [ClientId]      INT IDENTITY(1,1) NOT NULL,
    [FullName]      NVARCHAR(MAX)     NOT NULL,
    [Username]      NVARCHAR(MAX)     NOT NULL,
    [Email]         NVARCHAR(MAX)     NOT NULL,
    [Password]      NVARCHAR(MAX)     NOT NULL,
    PRIMARY KEY CLUSTERED ([ClientId] ASC)
);

CREATE TABLE [Payment] (
    [PaymentId]       INT IDENTITY(1,1) NOT NULL,
    [ClientId]        INT               NOT NULL,
	[FullName]        VARCHAR (MAX)     NOT NULL,
	[Email]			  VARCHAR (MAX)     NOT NULL, 
	[City]            VARCHAR (MAX)     NOT NULL, 
	[District]        VARCHAR (MAX)     NOT NULL, 
	[Code]            VARCHAR (MAX)     NOT NULL, 
	[CardName]        VARCHAR (MAX)     NOT NULL, 
    [CardNumber]      VARCHAR (MAX)     NOT NULL,    
    [MonthExp]        VARCHAR (MAX)     NOT NULL,
	[YearExp]         INT		        NOT NULL, 
    [CvvNumber]       INT               NOT NULL,
    PRIMARY KEY CLUSTERED ([PaymentId] ASC),
    -- FOREIGN KEY ([ClientId]) REFERENCES [Client]([ClientId])
);   