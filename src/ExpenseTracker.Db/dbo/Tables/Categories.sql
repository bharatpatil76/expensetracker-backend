CREATE TABLE [dbo].[Categories]
(
    [Id]   INT           IDENTITY (1, 1) NOT NULL,
    [Name] NVARCHAR (50) NOT NULL,
    CONSTRAINT [PK_Categories] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UQ_Categories_Name] UNIQUE ([Name])
);
