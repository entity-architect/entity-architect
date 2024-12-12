SELECT a.id as Id:GUID:Key, 
       a.name as Name:STRING,
       books:(b.id as Id:GUID, b.title as Title:STRING, rentals:(r.id as Id:GUID, client:(c.id as Id:GUID, c.name as Name:STRING):Client)[]:Rentlas)[]:Books 
FROM 
    author a
        left join 
    book b on b.author_id = a.id 
        left join
    rental r on r.book_id = b.id
        left join
    client c on c.id = r.client_id;
