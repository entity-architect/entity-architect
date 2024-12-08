SELECT r.id as Id:GUID:Key, r.rent_date as Date:DATETIME,
       client:(c.id as Id:GUID, c.name as Name:STRING):Client, 
       book:(b.id as Id:GUID, b.title as Title:STRING, author:(a.id as Id:GUID, a.name as Name:STRING):Author):Book
FROM
    rental r
        LEFT JOIN
    client c ON c.id = r.client_id
        LEFT JOIN
    book b ON b.id = r.book_id
        LEFT JOIN
    author a ON a.id = b.author_id;