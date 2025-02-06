using EntityArchitect.CRUD.Enumerations;

namespace EntityArchitect.Example;

public class BookType(int id, string name) : Enumeration(id, name)
{
    public static BookType Novel = new(0, "Novel");
    public static BookType Biography = new(1, "Biography");
    public static BookType Science = new(2, "Science");
    public static BookType History = new(3, "History");
    public static BookType Poetry = new(4, "Poetry");  
}