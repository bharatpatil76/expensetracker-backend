-- Runs after every publish, so it must be safe to run repeatedly
MERGE [dbo].[Categories] AS target
USING (VALUES (N'Food'), (N'Travel'), (N'Bills'), (N'Shopping'), (N'Other')) AS source ([Name])
ON target.[Name] = source.[Name]
WHEN NOT MATCHED THEN
    INSERT ([Name]) VALUES (source.[Name]);
