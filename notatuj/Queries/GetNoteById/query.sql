SELECT
    n.id AS Id:GUID:Key,
    n.title AS Title:STRING:Title,
    n.description AS Description:STRING:Content,
    tags:(
        t.id AS Id:GUID, 
        t.tag_name AS Tag:STRING
    )[]:Tags,
    contributors:(
        u.id AS Id:GUID, 
        u.first_name AS FirstName:STRING, 
        u.last_name AS LastName:STRING
    )[]:Contributors,
    sections:(
        s.id AS Id:GUID, 
        s.content AS Content:STRING
    )[]:Sections
FROM note n
    LEFT JOIN contribution c ON n.id = c.note_id
    LEFT JOIN "user" u ON c.user_id = u.id
    LEFT JOIN section s ON n.id = s.note_id
    RIGHT JOIN  tag t ON n.id = t.note_id
WHERE n.id = @noteId:GUID
