using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using CommunityToolkit.Diagnostics;

namespace LibProtodec;

public sealed class Protodec
{
    public delegate bool LookupFunc(string key, [MaybeNullWhen(false)] out string value);

    private readonly Dictionary<string, Protobuf> _protobufs      = [];
    private readonly HashSet<string>              _currentDescent = [];

    public LookupFunc? CustomTypeLookup { get; init; }

    public LookupFunc? CustomNameLookup { get; init; }

    public IReadOnlyDictionary<string, Protobuf> Protobufs =>
        _protobufs;

    public void WriteAllTo(IndentedTextWriter writer)
    {
        Protobuf.WritePreambleTo(writer);

        foreach (Protobuf proto in _protobufs.Values)
        {
            proto.WriteTo(writer);
            writer.WriteLine();
            writer.WriteLine();
        }
    }

    public void ParseMessage(Type type, bool skipEnums = false, bool skipPropertiesWithoutProtocAttribute = false)
    {
        Guard.IsTrue(type.IsClass);

        ParseMessageInternal(type, skipEnums, skipPropertiesWithoutProtocAttribute, null);
        _currentDescent.Clear();
    }

    public void ParseEnum(Type type)
    {
        Guard.IsTrue(type.IsEnum);

        ParseEnumInternal(type, null);
        _currentDescent.Clear();
    }

    private bool IsParsed(Type type, Message? parentMessage, out Dictionary<string, Protobuf> protobufs)
    {
        protobufs = parentMessage is not null && type.IsNested
            ? parentMessage.Nested
            : _protobufs;

        return protobufs.ContainsKey(type.Name)
            || !_currentDescent.Add(type.Name);
    }

    private void ParseMessageInternal(Type messageClass, bool skipEnums, bool skipPropertiesWithoutProtocAttribute, Message? parentMessage)
    {
        if (IsParsed(messageClass, parentMessage, out Dictionary<string, Protobuf> protobufs))
        {
            return;
        }

        Message message = new()
        {
            Name         = TranslateProtobufName(messageClass.Name),
            AssemblyName = messageClass.Assembly.FullName,
            Namespace    = messageClass.Namespace
        };

        FieldInfo[]    idFields   = messageClass.GetFields(BindingFlags.Public     | BindingFlags.Static);
        PropertyInfo[] properties = messageClass.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        for (int pi = 0, fi = 0; pi < properties.Length; pi++, fi++)
        {
            PropertyInfo property = properties[pi];

            if (property.GetMethod is null
             || property.GetMethod.IsVirtual 
             || (skipPropertiesWithoutProtocAttribute && !HasProtocAttribute(property)))
            {
                fi--;
                continue;
            }

            Type propertyType = property.PropertyType;

            // only OneOf enums are defined nested directly in the message class
            if (propertyType.IsEnum && propertyType.DeclaringType?.Name == message.Name)
            {
                string oneOfName = TranslateOneOfName(property.Name);
                int[] oneOfProtoFieldIds = propertyType.GetFields(BindingFlags.Public | BindingFlags.Static)
                                                       .Select(field => (int)field.GetRawConstantValue()!)
                                                       .Where(id => id > 0)
                                                       .ToArray();

                message.OneOfs.Add(oneOfName, oneOfProtoFieldIds);
                
                fi--;
                continue;
            }

            FieldInfo idField = idFields[fi];
            Guard.IsTrue(idField.IsLiteral);
            Guard.IsEqualTo(idField.FieldType.Name, nameof(Int32));

            int    msgFieldId         = (int)idField.GetRawConstantValue()!;
            bool   msgFieldIsOptional = false;
            string msgFieldType       = ParseFieldType(propertyType, skipEnums, skipPropertiesWithoutProtocAttribute, message);
            string msgFieldName       = TranslateMessageFieldName(property.Name);

            // optional protobuf fields will generate an additional "Has" get-only boolean property immediately after the real property
            if (properties.Length > pi + 1 && properties[pi + 1].PropertyType.Name == nameof(Boolean) && !properties[pi + 1].CanWrite)
            {
                msgFieldIsOptional = true;
                pi++;
            }

            message.Fields.Add(msgFieldId, (msgFieldIsOptional, msgFieldType, msgFieldName));
        }

        protobufs.Add(message.Name, message);
    }

    private void ParseEnumInternal(Type enumEnum, Message? parentMessage)
    {
        if (IsParsed(enumEnum, parentMessage, out Dictionary<string, Protobuf> protobufs))
        {
            return;
        }

        Enum @enum = new()
        {
            Name         = TranslateProtobufName(enumEnum.Name),
            AssemblyName = enumEnum.Assembly.FullName,
            Namespace    = enumEnum.Namespace
        };

        foreach (FieldInfo field in enumEnum.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            int    enumFieldId   = (int)field.GetRawConstantValue()!;
            string enumFieldName = TranslateEnumFieldName(field, @enum.Name);

            @enum.Fields.Add(enumFieldId, enumFieldName);
        }

        protobufs.Add(@enum.Name, @enum);
    }

    private string ParseFieldType(Type type, bool skipEnums, bool skipPropertiesWithoutProtocAttribute, Message message)
    {
        switch (type.Name)
        {
            case "ByteString":
                return "bytes";
            case nameof(String):
                return "string";
            case nameof(Boolean):
                return "bool";
            case nameof(Double):
                return "double";
            case nameof(UInt32):
                return "uint32";
            case nameof(UInt64):
                return "uint64";
            case nameof(Int32):
                return "int32";
            case nameof(Int64):
                return "int64";
            case nameof(Single):
                return "float";
        }

        switch (type.GenericTypeArguments.Length)
        {
            case 1:
                string t = ParseFieldType(type.GenericTypeArguments[0], skipEnums, skipPropertiesWithoutProtocAttribute, message);
                return "repeated " + t;
            case 2:
                string t1 = ParseFieldType(type.GenericTypeArguments[0], skipEnums, skipPropertiesWithoutProtocAttribute, message);
                string t2 = ParseFieldType(type.GenericTypeArguments[1], skipEnums, skipPropertiesWithoutProtocAttribute, message);
                return $"map<{t1}, {t2}>";
        }

        if (CustomTypeLookup?.Invoke(type.Name, out string? fieldType) == true)
        {
            return fieldType;
        }

        if (type.IsEnum)
        {
            if (skipEnums)
            {
                return "int32";
            }

            ParseEnumInternal(type, message);
        }
        else
        {
            ParseMessageInternal(type, skipEnums, skipPropertiesWithoutProtocAttribute, message);
        }

        if (!type.IsNested)
        {
            message.Imports.Add(type.Name);
        }

        return type.Name;
    }

    private static bool HasProtocAttribute(PropertyInfo property) =>
        property.GetCustomAttributesData()
                .Any(attr =>
                         attr.AttributeType.Name                      == "GeneratedCodeAttribute"
                      && attr.ConstructorArguments[0].Value as string == "protoc");

    private string TranslateProtobufName(string name) =>
        CustomNameLookup?.Invoke(name, out string? translatedName) == true
            ? translatedName
            : name;

    private string TranslateOneOfName(string oneOfEnumName) =>
        TranslateName(oneOfEnumName, out string translatedName)
            ? translatedName.TrimEnd("Case")
            : oneOfEnumName.TrimEnd("Case")
                           .ToSnakeCaseLower();

    private string TranslateMessageFieldName(string fieldName) =>
        TranslateName(fieldName, out string translatedName)
            ? translatedName
            : fieldName.ToSnakeCaseLower();

    private string TranslateEnumFieldName(FieldInfo field, string enumName)
    {
        if (field.GetCustomAttributesData()
                 .SingleOrDefault(attr => attr.AttributeType.Name == "OriginalNameAttribute")
                ?.ConstructorArguments[0]
                 .Value
            is string originalName)
        {
            return originalName;
        }

        if (TranslateName(field.Name, out string translatedName))
        {
            return translatedName;
        }

        if (!enumName.IsBeebyted())
        {
            enumName = enumName.ToSnakeCaseUpper();
        }

        return enumName + '_' + field.Name.ToSnakeCaseUpper();
    }

    private bool TranslateName(string name, out string translatedName)
    {
        if (CustomNameLookup?.Invoke(name, out translatedName!) == true)
        {
            return true;
        }

        translatedName = name;
        return name.IsBeebyted();
    }
}