CREATE TABLE [dbo].[ExpenseAttachments]
(
    [Id]          INT            IDENTITY (1, 1) NOT NULL,
    [ExpenseId]   INT            NOT NULL,
    [Kind]        NVARCHAR (20)  NOT NULL,
    [BlobName]    NVARCHAR (400) NOT NULL,
    [ContentType] NVARCHAR (100) NOT NULL,
    [SizeBytes]   BIGINT         NOT NULL,
    [UploadedAt]  DATETIME2      NOT NULL CONSTRAINT [DF_ExpenseAttachments_UploadedAt] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_ExpenseAttachments] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_ExpenseAttachments_Expenses] FOREIGN KEY ([ExpenseId]) REFERENCES [dbo].[Expenses] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [CK_ExpenseAttachments_Kind] CHECK ([Kind] IN (N'Receipt', N'VoiceNote')),
    CONSTRAINT [UQ_ExpenseAttachments_Expense_Kind] UNIQUE ([ExpenseId], [Kind])
);
