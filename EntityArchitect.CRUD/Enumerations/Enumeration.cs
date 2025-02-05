using System.Reflection;

namespace EntityArchitect.CRUD.Enumerations;

public abstract class Enumeration : IComparable
{
    protected Enumeration(int id) : this(id, "")
    {
    }

    protected Enumeration(int id, string name)
    {
        Name = name;
        Id = id;
    }

    public string Name { get; set; }
    public int Id { get; private set; }

    public override string ToString() => Name;

    public static TEnumeration? Default<TEnumeration>() where TEnumeration : Enumeration =>
        Activator.CreateInstance(typeof(TEnumeration), -1, "Default")
            as TEnumeration;

    public bool IsDefault<TEnumeration>() where TEnumeration : Enumeration
        => Equals(this, Default<TEnumeration>());

    public static IEnumerable<T?> GetAll<T>() where T : Enumeration
    {
        IEnumerable<T?> x = typeof(T).GetFields(BindingFlags.Public |
                                                BindingFlags.Static |
                                                BindingFlags.DeclaredOnly)
            .Where(f => f.FieldType == typeof(T))
            .Select(f => f.GetValue(null))
            .Cast<T?>();

        var y = x.ToList();
        if (!y.Any(c => c?.Id == -1))
            y.Add(Default<T>());
        return y;
    }

    public static TEnumeration GetById<TEnumeration>(int id)
        where TEnumeration : Enumeration
    {
        var x = GetAll<TEnumeration>();
        return x.FirstOrDefault(x => x.Id == id) ?? null;
    }

    public override bool Equals(object obj)
    {
        if (obj is not Enumeration otherValue)
        {
            return false;
        }

        var typeMatches = GetType().Equals(obj.GetType());
        var valueMatches = Id.Equals(otherValue.Id);

        return typeMatches && valueMatches;
    }

    public override int GetHashCode() => Id.GetHashCode();

    public int CompareTo(object other) => Id.CompareTo(((Enumeration)other).Id);

}
