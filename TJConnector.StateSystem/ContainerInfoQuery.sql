SELECT [ExternalDbCode]
      ,[ExternalDbStatus]
      ,[ExternalDbStatusMessage]
  FROM [externaldb].[dbo].[StatusTable]
  WHERE ExternalDbCode IN @Codes