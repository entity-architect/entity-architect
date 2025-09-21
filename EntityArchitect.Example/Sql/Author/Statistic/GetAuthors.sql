SELECT 
    a.id AS Id:GUID:KEY,
    a.name AS Name:STRING
FROM 
    author a
WHERE a.name like CONCAT('%', @Filter:STRING, '%')