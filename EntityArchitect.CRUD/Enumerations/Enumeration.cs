using System;
using System.Collections.Generic;
using System.Linq;
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

    public static TEnumeration? Default<TEnumeration>() where TEnumeration : Enumeration
    {
        try
        {
            return Activator.CreateInstance(typeof(TEnumeration), -1, "Default") as TEnumeration;
        }
        catch
        {
            var constructors = typeof(TEnumeration).GetConstructors();
            foreach (var ctor in constructors.OrderBy(c => c.GetParameters().Length))
            {
                var parameters = ctor.GetParameters();
                if (parameters.Length < 2) continue;
                if (parameters[0].ParameterType != typeof(int)) continue;
                if (parameters[1].ParameterType != typeof(string)) continue;

                var args = new object[parameters.Length];
                args[0] = -1;
                args[1] = "Default";
                for (int i = 2; i < parameters.Length; i++)
                {
                    var paramType = parameters[i].ParameterType;
                    args[i] = paramType.IsValueType ? Activator.CreateInstance(paramType)! : null!;
                }

                try { return ctor.Invoke(args) as TEnumeration; } catch { }
            }

            return null;
        }
    }

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
        {
            var defaultValue = Default<T>();
            if (defaultValue != null)
                y.Add(defaultValue);
        }

        return y;
    }

    public static TEnumeration GetById<TEnumeration>(int id)
        where TEnumeration : Enumeration
    {
        var x = GetAll<TEnumeration>();
        return x.FirstOrDefault(x => x != null && x.Id == id);
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
