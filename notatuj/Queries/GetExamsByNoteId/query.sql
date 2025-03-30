SELECT
    e.id AS Id:GUID:Key,
    e.title AS Title:STRING,
    questions:(
        q.id AS Id:GUID,
        q.content AS Content:STRING,
        answers:(
            a.id AS Id:GUID,
            a.content AS Content:STRING,
            a.is_correct AS IsCorrect:BOOLEAN
        )[]:Answers
    )[]:Questions
FROM "exam" e 
    JOIN question q ON e.id = q.exam_id
    JOIN answer a ON q.id = a.question_id
WHERE e.note_id = @noteId:GUID