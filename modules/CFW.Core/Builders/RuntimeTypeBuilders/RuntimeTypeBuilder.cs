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

    public bool IsNullable { get; set; } = true;
}

public static class RuntimeTypeBuilder
{
    public static Type CreateType(
        RuntimeTypeDefinition runtimeTypeDefinition,
        IEnumerable<RuntimePropertyDefinition> properties)
    {
        if (runtimeTypeDefinition is null) throw new ArgumentNullException(nameof(runtimeTypeDefinition));
        if (properties is null) throw new ArgumentNullException(nameof(properties));

        var moduleBuilder = runtimeTypeDefinition.ModuleBuilder;

        var typeBuilder = moduleBuilder.DefineType(
            runtimeTypeDefinition.TypeName,
            TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.BeforeFieldInit);

        // Common attributes
        var keyAttrCtor = typeof(KeyAttribute).GetConstructor(Type.EmptyTypes)
                         ?? throw new InvalidOperationException("KeyAttribute .ctor not found.");
        var requiredAttrCtor = typeof(RequiredAttribute).GetConstructor(Type.EmptyTypes)
                             ?? throw new InvalidOperationException("RequiredAttribute .ctor not found.");

        // Nullable metadata attributes (optional – present in modern runtimes)
        // System.Runtime.CompilerServices.NullableAttribute(byte) / NullableAttribute(byte[])
        // System.Runtime.CompilerServices.NullableContextAttribute(byte)
        var nullableAttrType = Type.GetType("System.Runtime.CompilerServices.NullableAttribute");
        var nullableContextAttrType = Type.GetType("System.Runtime.CompilerServices.NullableContextAttribute");

        var nullableAttrCtorByte = nullableAttrType?
            .GetConstructor([typeof(byte)]);
        var nullableAttrCtorBytes = nullableAttrType?
            .GetConstructor([typeof(byte[])]);
        var nullableContextCtor = nullableContextAttrType?
            .GetConstructor([typeof(byte)]);

        // If you want a default context for the whole type (e.g., "non-nullable by default"),
        // uncomment the next block to apply [NullableContext(1)] on the type.
        // 1 = non-nullable context, 2 = nullable context.
        // if (nullableContextCtor is not null)
        // {
        //     var contextAttr = new CustomAttributeBuilder(nullableContextCtor, new object[] { (byte)1 });
        //     typeBuilder.SetCustomAttribute(contextAttr);
        // }

        foreach (var propertyDefinition in properties)
        {
            var name = propertyDefinition.Name ?? throw new ArgumentNullException(nameof(propertyDefinition.Name));
            var original = propertyDefinition.Type ?? throw new ArgumentNullException(nameof(propertyDefinition.Type));
            var isKey = propertyDefinition.IsKey;
            var isRequired = propertyDefinition.IsRequired;
            var isNullable = propertyDefinition.IsNullable;

            // Resolve CLR "nullable" type to use for the backing field & property
            // - For value types: wrap with Nullable<T> when isNullable = true
            // - For reference types: CLR already allows null; we add metadata attribute later
            var clrType = original;
            if (isNullable && original.IsValueType && Nullable.GetUnderlyingType(original) is null)
            {
                clrType = typeof(Nullable<>).MakeGenericType(original);
            }

            // private <clrType> _name;
            var field = typeBuilder.DefineField($"_{char.ToLowerInvariant(name[0])}{name.Substring(1)}",
                                                clrType,
                                                FieldAttributes.Private);

            // public <clrType> Name { get; set; }
            var prop = typeBuilder.DefineProperty(name, PropertyAttributes.HasDefault, clrType, null);

            // getter
            var getter = typeBuilder.DefineMethod($"get_{name}",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                clrType, Type.EmptyTypes);
            var getterIL = getter.GetILGenerator();
            getterIL.Emit(OpCodes.Ldarg_0);
            getterIL.Emit(OpCodes.Ldfld, field);
            getterIL.Emit(OpCodes.Ret);

            // setter
            var setter = typeBuilder.DefineMethod($"set_{name}",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                null, new[] { clrType });
            var setterIL = setter.GetILGenerator();
            setterIL.Emit(OpCodes.Ldarg_0);
            setterIL.Emit(OpCodes.Ldarg_1);
            setterIL.Emit(OpCodes.Stfld, field);
            setterIL.Emit(OpCodes.Ret);

            prop.SetGetMethod(getter);
            prop.SetSetMethod(setter);

            // [Key]
            if (isKey)
            {
                prop.SetCustomAttribute(new CustomAttributeBuilder(keyAttrCtor, Array.Empty<object>()));
            }

            // [Required] — typically you want this when NOT nullable (and not a nullable<T>)
            // Keep your existing flag, but many folks tie this to isNullable == false
            if (isRequired)
            {
                prop.SetCustomAttribute(new CustomAttributeBuilder(requiredAttrCtor, Array.Empty<object>()));
            }

            // Emit nullable metadata for reference types (so tools can know string? vs string)
            //  - 2 => nullable, 1 => non-nullable
            // For value types, the runtime type already conveys nullability (Nullable<T> vs T).
            if (!original.IsValueType && nullableAttrType is not null)
            {
                byte flag = isNullable ? (byte)2 : (byte)1;

                // Prefer single-byte ctor if available; fall back to byte[] signature.
                if (nullableAttrCtorByte is not null)
                {
                    var attr = new CustomAttributeBuilder(nullableAttrCtorByte, new object[] { flag });
                    prop.SetCustomAttribute(attr);
                }
                else if (nullableAttrCtorBytes is not null)
                {
                    var attr = new CustomAttributeBuilder(nullableAttrCtorBytes, new object[] { new byte[] { flag } });
                    prop.SetCustomAttribute(attr);
                }
            }
        }

        return typeBuilder.CreateType()
               ?? throw new InvalidOperationException("Failed to create dynamic type.");
    }
}
