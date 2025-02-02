WITH CH AS
(SELECT Id, OrderId, RecordDate, BlackListReason, Code FROM intobj.ConsolidatedHierarchiesArchive
UNION ALL 
SELECT Id, OrderId, RecordDate, BlackListReason, Code FROM intobj.ConsolidatedHierarchies)


SELECT CH.Code AS ExternalDbCode,
CASE WHEN M.CountryCode = 'TJ' AND CH.BlackListReason = 0 THEN 1 ELSE 0 END AS ExternalDbStatus,
CASE WHEN CH.BlackListReason <> 0 THEN 'Incorrect status' WHEN M.CountryCode <> 'TJ' THEN 'Incorrect market' ELSE 'OK' END AS ExternalDbStatusMessage
FROM CH
LEFT JOIN intobj.Orders AS O ON O.Id = CH.OrderId 
LEFT JOIN mda.Markets AS M ON M.Id = O.MarketId
WHERE CH.Code IN @Codes