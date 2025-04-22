using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Reflection.Emit;

namespace CFW.Core.Builders.RuntimeTypeBuilders;

public class RuntimeTypeDefinition
{
    public string TypeName { get; set; } = string.Empty;

    public ModuleBuilder ModuleBuilder { get; set; } = null!;
}

public class RuntimePropertyDefinition
{
    public string Name { get; set; } = string.Empty;

    public Type Type { get; set; } = null!;

    public bool IsKey { get; set; }

    public bool IsRequired { get; set; }
}

public static class RuntimeTypeBuilder
{
    public static Type CreateType(RuntimeTypeDefinition runtimeTypeDefinition, IEnumerable<RuntimePropertyDefinition> properties)
    {
        var moduleBuilder = runtimeTypeDefinition.ModuleBuilder;

        var typeBuilder = moduleBuilder.DefineType(runtimeTypeDefinition.TypeName,
            TypeAttributes.Public | TypeAttributes.Class);

        var keyAttrCtor = typeof(KeyAttribute).GetConstructor(Type.EmptyTypes)!;
        var requiredAttrCtor = typeof(RequiredAttribute).GetConstructor(Type.EmptyTypes)!;

        foreach (var propertyDefinition in properties)
        {
            var name = propertyDefinition.Name;
            var type = propertyDefinition.Type;
            var isKey = propertyDefinition.IsKey;
            var isRequired = propertyDefinition.IsRequired;

            var field = typeBuilder.DefineField($"_{name.ToLower()}", type, FieldAttributes.Private);
            var prop = typeBuilder.DefineProperty(name, PropertyAttributes.HasDefault, type, null);

            var getter = typeBuilder.DefineMethod($"get_{name}",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                type, Type.EmptyTypes);
            var getterIL = getter.GetILGenerator();
            getterIL.Emit(OpCodes.Ldarg_0);
            getterIL.Emit(OpCodes.Ldfld, field);
            getterIL.Emit(OpCodes.Ret);

            var setter = typeBuilder.DefineMethod($"set_{name}",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                null, [type]);
            var setterIL = setter.GetILGenerator();
            setterIL.Emit(OpCodes.Ldarg_0);
            setterIL.Emit(OpCodes.Ldarg_1);
            setterIL.Emit(OpCodes.Stfld, field);
            setterIL.Emit(OpCodes.Ret);

            prop.SetGetMethod(getter);
            prop.SetSetMethod(setter);

            if (isKey)
                prop.SetCustomAttribute(new CustomAttributeBuilder(keyAttrCtor, []));

            if (isRequired)
                prop.SetCustomAttribute(new CustomAttributeBuilder(requiredAttrCtor, Array.Empty<object>()));
        }

        return typeBuilder.CreateType();
    }
}
