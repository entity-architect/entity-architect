SELECT r.id as Id:GUID, 
       client:(c.id as ClientId:GUID, c.name as ClientName:STRING):Client, 
       book:(b.id as BookId:GUID, b.title as Title:STRING):Book
FROM
    rental r
        LEFT JOIN
    client c ON c.id = r.client_id
        LEFT JOIN
    book b ON b.id = r.book_id;