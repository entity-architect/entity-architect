SELECT a.id as Id, a.name, as Name b.title as Title FROM author a left join book b on b.author_id = a.id;

a.id as Id:STRING:TableName
id as Id:STRING:TableName
id:STRING:TableName