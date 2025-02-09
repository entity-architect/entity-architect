SELECT a.id as Id:GUID:Key, a.name as Name:STRING, books:(b.id as BookId:GUID, b.title as Title:STRING, b.bookt_type AS BookType:ENUMERATION)[]:Books
FROM author a left join book b
on b.author_id = a.id;