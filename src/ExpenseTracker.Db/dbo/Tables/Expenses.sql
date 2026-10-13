CREATE TABLE [dbo].[Expenses]
(
    [Id]         INT             IDENTITY (1, 1) NOT NULL,
    [UserId]     NVARCHAR (128)  NOT NULL,
    [CategoryId] INT             NOT NULL,
    [Title]      NVARCHAR (200)  NOT NULL,
    [Amount]     DECIMAL (10, 2) NOT NULL,
    [SpentOn]    DATE            NOT NULL,
    [CreatedAt]  DATETIME2       NOT NULL CONSTRAINT [DF_Expenses_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_Expenses] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_Expenses_Categories] FOREIGN KEY ([CategoryId]) REFERENCES [dbo].[Categories] ([Id]),
    CONSTRAINT [CK_Expenses_Amount] CHECK ([Amount] > 0)
);
GO

CREATE INDEX [IX_Expenses_UserId_SpentOn] ON [dbo].[Expenses] ([UserId], [SpentOn] DESC);
