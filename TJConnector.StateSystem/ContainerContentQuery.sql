SELECT TOP (1000) [Bundle]
      ,[Pack]
  FROM [externaldb].[dbo].[ContentTable]
WHERE MC = @Code