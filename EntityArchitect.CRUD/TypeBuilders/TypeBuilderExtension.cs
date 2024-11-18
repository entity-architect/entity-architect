using System.CodeDom;
using System.Reflection;
using System.Reflection.Emit;
using EntityArchitect.CRUD.Attributes;

namespace EntityArchitect.CRUD.TypeBuilders;

internal static class TypeBuilderExtension
{
    internal static System.Reflection.Emit.TypeBuilder GetTypeBuilder(string typeName, Type? parentType = null, CustomAttributeBuilder? customAttributeBuilder = null)
    {
        var assemblyName = new AssemblyName(typeName);
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
        
        if(customAttributeBuilder is not null)
            moduleBuilder.SetCustomAttribute(customAttributeBuilder);
        return parentType is not null
            ? moduleBuilder.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class, parentType)
            : moduleBuilder.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class);
    }

    internal static void CreateProperty(System.Reflection.Emit.TypeBuilder typeBuilder, string propertyName,
        Type propertyType)
    {
        var fieldBuilder = typeBuilder.DefineField($"_{propertyName}", propertyType, FieldAttributes.Private);

        var propertyBuilder =
            typeBuilder.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);
        var getMethodBuilder = typeBuilder.DefineMethod($"get_{propertyName}",
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
            propertyType, Type.EmptyTypes);

        var getIl = getMethodBuilder.GetILGenerator();
        getIl.Emit(OpCodes.Ldarg_0);
        getIl.Emit(OpCodes.Ldfld, fieldBuilder);
        getIl.Emit(OpCodes.Ret);

        var setMethodBuilder = typeBuilder.DefineMethod($"set_{propertyName}",
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
            null, new[] { propertyType });

        var setIl = setMethodBuilder.GetILGenerator();
        setIl.Emit(OpCodes.Ldarg_0);
        setIl.Emit(OpCodes.Ldarg_1);
        setIl.Emit(OpCodes.Stfld, fieldBuilder);
        setIl.Emit(OpCodes.Ret);

        propertyBuilder.SetGetMethod(getMethodBuilder);
        propertyBuilder.SetSetMethod(setMethodBuilder);
    }
}