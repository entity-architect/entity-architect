SELECT 
    n.id as Id:GUID:Key, 
    n.title as Title:STRING:Title, 
    n.description as Deescription:STRING:Content,
    tags:(t.id as Id:GUID, t.tag_name as Tag:STRING:Tag)[]:Tags

FROM note n
LEFT JOIN tag t ON n.id = t.note_id
LEFT JOIN contribution c ON n.id = c.note_id
WHERE c.user_id = @userId:GUID