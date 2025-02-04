DECLARE @LocalMC AS VARCHAR(100) = @Code; 

WITH CH AS
(SELECT Id, Code, REPLACE(FullCode,']','') AS FullCode, ParentId FROM intobj.ConsolidatedHierarchiesArchive
UNION ALL 
SELECT Id, Code, REPLACE(FullCode,']','') AS FullCode, ParentId FROM intobj.ConsolidatedHierarchies),
CI AS
(SELECT Id, Code, REPLACE(FullCode,']','') AS FullCode, ParentId FROM intobj.ConsolidatedItemsArchive
UNION ALL 
SELECT Id, Code, REPLACE(FullCode,']','') AS FullCode, ParentId FROM intobj.ConsolidatedItems)

SELECT Bundles.FullCode AS Bundle, Packs.FullCode AS Pack FROM CH AS MC
INNER JOIN CH AS Bundles ON Bundles.ParentId = MC.Id 
INNER JOIN CI AS Packs ON Packs.ParentId = Bundles.Id
WHERE MC.Code = @LocalMC