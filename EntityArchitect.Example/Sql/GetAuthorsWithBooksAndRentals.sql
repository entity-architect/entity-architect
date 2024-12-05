SELECT a.id as Id:GUID, 
       a.name as Name:STRING,
       books:(b.id as BookId:GUID, b.title:STRING, rentals:(r.id:GUID, r.client_id:GUID)[]:Rentlas)[]:Books 
FROM 
    author a
        left join 
    book b on b.author_id = a.id 
        left join
    rental r on r.book_id = b.id;
