using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using CommunityToolkit.Diagnostics;

namespace protodec;

public sealed class Protodec
{
    public readonly Dictionary<string, ProtobufMessage> Messages = new();
    public readonly Dictionary<string, ProtobufEnum>    Enums    = new();

    private const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;

    public void ParseMessage(Type messageClass, bool skipEnums = false)
    {
        Guard.IsTrue(messageClass.IsClass);
        if (Messages.ContainsKey(messageClass.Name))
            return;

        ProtobufMessage message    = new(messageClass.Name);
        FieldInfo[]     idFields   = messageClass.GetFields(PublicStatic);
        PropertyInfo[]  properties = messageClass.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        for (int i = 0; i < properties.Length; i++)
        {
            Type propertyType = properties[i].PropertyType;

            // only OneOf enums are defined nested directly in the message class
            if (propertyType.IsEnum && propertyType.DeclaringType?.Name == messageClass.Name)
            {
                string oneOfName = TranslateMessageFieldName(properties[i].Name);
                int[] oneOfProtoFieldIds = propertyType.GetFields(PublicStatic)
                                                       .Select(field => (int)field.GetRawConstantValue()!)
                                                       .Where(id => id > 0)
                                                       .ToArray();

                message.OneOfs.Add(oneOfName, oneOfProtoFieldIds);
                continue;
            }

            FieldInfo idField = idFields[i];
            Guard.IsTrue(idField.IsLiteral);
            Guard.IsEqualTo(idField.FieldType.Name, nameof(Int32));

            int    msgFieldId   = (int)idField.GetRawConstantValue()!;
            string msgFieldType = ParseType(propertyType, skipEnums, message);
            string msgFieldName = TranslateMessageFieldName(properties[i].Name);

            message.Fields.Add(msgFieldId, (msgFieldType, msgFieldName));
        }

        Messages.Add(message.Name, message);
    }

    private string ParseType(Type type, bool skipEnums, ProtobufMessage message)
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
            case "RepeatedField`1":
                string typeName = ParseType(type.GenericTypeArguments[0], skipEnums, message);
                return "repeated " + typeName;
            case "MapField`2":
                string t1 = ParseType(type.GenericTypeArguments[0], skipEnums, message);
                string t2 = ParseType(type.GenericTypeArguments[1], skipEnums, message);
                return $"map<{t1}, {t2}>";
            default:
            {
                if (type.IsEnum)
                {
                    if (skipEnums)
                        return "int32";
                    ParseEnum(type, message);
                }
                else
                {
                    ParseMessage(type, skipEnums);
                    message.Imports.Add(type.Name);
                }

                return type.Name;
            }
        }
    }

    private void ParseEnum(Type enumEnum, ProtobufMessage message)
    {
        if ((enumEnum.IsNested && message.Nested.ContainsKey(enumEnum.Name)) 
         || Enums.ContainsKey(enumEnum.Name))
            return;

        ProtobufEnum protoEnum = new(enumEnum.Name);
        foreach (FieldInfo field in enumEnum.GetFields(PublicStatic))
        {
            int    enumFieldId   = (int)field.GetRawConstantValue()!;
            string enumFieldName = field.GetCustomAttributesData()
                                        .SingleOrDefault(attr => attr.AttributeType.Name == "OriginalNameAttribute")
                                       ?.ConstructorArguments[0]
                                        .Value
                                       as string
                                ?? TranslateEnumFieldName(field.Name);

            protoEnum.Fields.Add(enumFieldId, enumFieldName);
        }

        if (enumEnum.IsNested)
        {
            message.Nested.Add(protoEnum.Name, protoEnum);
        }
        else
        {
            message.Imports.Add(protoEnum.Name);
            Enums.Add(protoEnum.Name, protoEnum);
        }
    }

    public void WriteAllTo(IndentedTextWriter writer)
    {
        WritePreambleTo(writer);

        foreach (IWritable proto in Messages.Values.Concat<IWritable>(Enums.Values))
        {
            proto.WriteTo(writer);
            writer.WriteLine();
            writer.WriteLine();
        }
    }

    internal static void WritePreambleTo(TextWriter writer)
    {
        writer.WriteLine("// Decompiled with protodec");
        writer.WriteLine();
        writer.WriteLine("""syntax = "proto3";""");
        writer.WriteLine();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string TranslateMessageFieldName(string name) =>
        name.IsBeebyted() ? name : name.ToSnakeCaseLower();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string TranslateEnumFieldName(string name) =>
        name.IsBeebyted() ? name : name.ToSnakeCaseUpper();

    private bool TryParseWriteToMethod(Type targetClass)
    {
        //MethodInfo method = targetClass.GetInterface("Google.Protobuf.IBufferMessage")?.GetMethod("InternalWriteTo", BindingFlags.Public | BindingFlags.Instance)!;

        byte[] cil = targetClass.GetMethod("WriteTo", BindingFlags.Public | BindingFlags.Instance)!
                                .GetMethodBody()!
                                .GetILAsByteArray()!;

        if (cil[0] == 0x2A) // ret
        {
            return false;
        }

        throw new NotImplementedException();
    }
}