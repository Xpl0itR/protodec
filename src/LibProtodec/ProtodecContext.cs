// Copyright © 2023-2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using SystemEx;
using CommunityToolkit.Diagnostics;
using LibProtodec.Models;
using LibProtodec.Models.Fields;
using LibProtodec.Models.TopLevels;
using LibProtodec.Models.Types;

namespace LibProtodec;

public delegate bool TypeLookupFunc(Type type, [NotNullWhen(true)] out IType? fieldType, out string? import);
public delegate bool NameLookupFunc(string name, [MaybeNullWhen(false)] out string translatedName);

public sealed class ProtodecContext
{
    private const BindingFlags PublicStatic           = BindingFlags.Public | BindingFlags.Static;
    private const BindingFlags PublicInstanceDeclared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    private readonly Dictionary<string, TopLevel> _parsed = [];

    public readonly List<Protobuf> Protobufs = [];

    public NameLookupFunc? NameLookup { get; set; }

    public TypeLookupFunc TypeLookup { get; set; } =
        LookupScalarAndWellKnownTypes;

    public void WriteAllTo(IndentedTextWriter writer)
    {
        writer.WriteLine("// Decompiled with protodec");
        writer.WriteLine();
        writer.WriteLine("""syntax = "proto3";""");
        writer.WriteLine();

        foreach (TopLevel topLevel in Protobufs.SelectMany(static proto => proto.TopLevels))
        {
            topLevel.WriteTo(writer);
            writer.WriteLine();
            writer.WriteLine();
        }
    }

    public Message ParseMessage(Type messageClass, ParserOptions options = ParserOptions.None)
    {
        Guard.IsTrue(messageClass is { IsClass: true, IsSealed: true });

        if (_parsed.TryGetValue(messageClass.FullName ?? messageClass.Name, out TopLevel? parsedMessage))
        {
            return (Message)parsedMessage;
        }

        Message message = new()
        {
            Name       = TranslateTypeName(messageClass),
            IsObsolete = HasObsoleteAttribute(messageClass.GetCustomAttributesData())
        };
        _parsed.Add(messageClass.FullName ?? messageClass.Name, message);

        Protobuf protobuf = GetProtobuf(messageClass, message, options);

        FieldInfo[]    idFields   = messageClass.GetFields(PublicStatic);
        PropertyInfo[] properties = messageClass.GetProperties(PublicInstanceDeclared);

        for (int pi = 0, fi = 0; pi < properties.Length; pi++, fi++)
        {
            PropertyInfo               property   = properties[pi];
            IList<CustomAttributeData> attributes = property.GetCustomAttributesData();

            if (((options & ParserOptions.IncludePropertiesWithoutNonUserCodeAttribute) == 0 && !HasNonUserCodeAttribute(attributes))
             || property.GetMethod?.IsVirtual != false)
            {
                fi--;
                continue;
            }

            Type propertyType = property.PropertyType;

            // only OneOf enums are defined nested directly in the message class
            if (propertyType.IsEnum && propertyType.DeclaringType?.Name == messageClass.Name)
            {
                string oneOfName = TranslateOneOfPropName(property.Name);
                int[] oneOfProtoFieldIds = propertyType.GetFields(PublicStatic)
                                                       .Select(static field => (int)field.GetRawConstantValue()!)
                                                       .Where(static id => id > 0)
                                                       .ToArray();

                message.OneOfs.Add(oneOfName, oneOfProtoFieldIds);
                
                fi--;
                continue;
            }

            FieldInfo idField = idFields[fi];
            Guard.IsTrue(idField.IsLiteral);
            Guard.IsEqualTo(idField.FieldType.Name, nameof(Int32));

            bool msgFieldHasHasProp = false; // some field properties are immediately followed by an additional "Has" get-only boolean property
            if (properties.Length > pi + 1 && properties[pi + 1].PropertyType.Name == nameof(Boolean) && !properties[pi + 1].CanWrite)
            {
                msgFieldHasHasProp = true;
                pi++;
            }

            MessageField field = new()
            {
                Type       = ParseFieldType(propertyType, options, protobuf),
                Name       = TranslateMessageFieldName(property.Name),
                Id         = (int)idField.GetRawConstantValue()!,
                IsObsolete = HasObsoleteAttribute(attributes),
                HasHasProp = msgFieldHasHasProp
            };

            message.Fields.Add(field.Id, field);
        }

        return message;
    }

    public Enum ParseEnum(Type enumEnum, ParserOptions options = ParserOptions.None)
    {
        Guard.IsTrue(enumEnum.IsEnum);

        if (_parsed.TryGetValue(enumEnum.FullName ?? enumEnum.Name, out TopLevel? parsedEnum))
        {
            return (Enum)parsedEnum;
        }

        Enum @enum = new()
        {
            Name         = TranslateTypeName(enumEnum),
            IsObsolete   = HasObsoleteAttribute(enumEnum.GetCustomAttributesData())
        };
        _parsed.Add(enumEnum.FullName ?? enumEnum.Name, @enum);

        Protobuf protobuf = GetProtobuf(enumEnum, @enum, options);

        foreach (FieldInfo field in enumEnum.GetFields(PublicStatic))
        {
            @enum.Fields.Add(
                new EnumField
                {
                    Id         = (int)field.GetRawConstantValue()!,
                    Name       = TranslateEnumFieldName(field, @enum.Name),
                    IsObsolete = HasObsoleteAttribute(field.GetCustomAttributesData())
                });
        }

        if (@enum.Fields.All(static field => field.Id != 0))
        {
            protobuf.Edition = "2023";
            @enum.IsClosed   = true;
        }

        return @enum;
    }

    public Service ParseService(Type serviceClass, ParserOptions options = ParserOptions.None)
    {
        Guard.IsTrue(serviceClass.IsClass);

        bool? isClientClass = null;
        if (serviceClass.IsAbstract)
        {
            if (serviceClass is { IsSealed: true, IsNested: false })
            {
                Type[] nested = serviceClass.GetNestedTypes();
                serviceClass  = nested.SingleOrDefault(static nested => nested is { IsAbstract: true, IsSealed: false })
                             ?? nested.Single(static nested => nested is { IsClass: true, IsAbstract: false });
            }
            
            if (serviceClass is { IsNested: true, IsAbstract: true, IsSealed: false })
            {
                isClientClass = false;
            }
        }

        if (serviceClass is { IsAbstract: false, IsNested: true, DeclaringType: not null })
        {
            isClientClass = true;
        }

        Guard.IsNotNull(isClientClass);

        if (_parsed.TryGetValue(serviceClass.DeclaringType!.FullName ?? serviceClass.DeclaringType!.Name, out TopLevel? parsedService))
        {
            return (Service)parsedService;
        }

        Service service = new()
        {
            Name         = TranslateTypeName(serviceClass.DeclaringType),
            IsObsolete   = HasObsoleteAttribute(serviceClass.GetCustomAttributesData())
        };
        _parsed.Add(serviceClass.DeclaringType!.FullName ?? serviceClass.DeclaringType.Name, service);

        Protobuf protobuf = NewProtobuf(serviceClass, service);

        foreach (MethodInfo method in serviceClass.GetMethods(PublicInstanceDeclared))
        {
            IList<CustomAttributeData> attributes = method.GetCustomAttributesData();
            if ((options & ParserOptions.IncludeServiceMethodsWithoutGeneratedCodeAttribute) == 0
             && !HasGeneratedCodeAttribute(attributes, "grpc_csharp_plugin"))
            {
                continue;
            }

            Type requestType, responseType, returnType = method.ReturnType;
            bool streamReq,   streamRes;

            if (isClientClass.Value)
            {
                string returnTypeName = TranslateTypeName(returnType);
                if (returnTypeName == "AsyncUnaryCall`1")
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length > 2)
                {
                    continue;
                }

                Type firstParamType = parameters[0].ParameterType;
                switch (returnType.GenericTypeArguments.Length)
                {
                    case 2:
                        requestType  = returnType.GenericTypeArguments[0];
                        responseType = returnType.GenericTypeArguments[1];
                        streamReq    = true;
                        streamRes    = returnTypeName == "AsyncDuplexStreamingCall`2";
                        break;
                    case 1:
                        requestType  = firstParamType;
                        responseType = returnType.GenericTypeArguments[0];
                        streamReq    = false;
                        streamRes    = true;
                        break;
                    default:
                        requestType  = firstParamType;
                        responseType = returnType;
                        streamReq    = false;
                        streamRes    = false;
                        break;
                }
            }
            else
            {
                ParameterInfo[] parameters     = method.GetParameters();
                Type            firstParamType = parameters[0].ParameterType;

                if (firstParamType.GenericTypeArguments.Length == 1)
                {
                    streamReq   = true;
                    requestType = firstParamType.GenericTypeArguments[0];
                }
                else
                {
                    streamReq   = false;
                    requestType = firstParamType;
                }

                if (returnType.GenericTypeArguments.Length == 1)
                {
                    streamRes    = false;
                    responseType = returnType.GenericTypeArguments[0];
                }
                else
                {
                    streamRes    = true;
                    responseType = parameters[1].ParameterType.GenericTypeArguments[0];
                }
            }

            service.Methods.Add(
                new ServiceMethod
                {
                    Name               = TranslateMethodName(method.Name),
                    IsObsolete         = HasObsoleteAttribute(attributes),
                    RequestType        = ParseFieldType(requestType,  options, protobuf),
                    ResponseType       = ParseFieldType(responseType, options, protobuf),
                    IsRequestStreamed  = streamReq,
                    IsResponseStreamed = streamRes
                });
        }

        return service;
    }

    private IType ParseFieldType(Type type, ParserOptions options, Protobuf referencingProtobuf)
    {
        switch (type.GenericTypeArguments.Length)
        {
            case 1:
                return new Repeated(
                    ParseFieldType(type.GenericTypeArguments[0], options, referencingProtobuf));
            case 2:
                return new Map(
                    ParseFieldType(type.GenericTypeArguments[0], options, referencingProtobuf),
                    ParseFieldType(type.GenericTypeArguments[1], options, referencingProtobuf));
        }

        if (TypeLookup(type, out IType? fieldType, out string? import))
        {
            if (import is not null)
            {
                referencingProtobuf.Imports.Add(import);
            }

            return fieldType;
        }

        if (type.IsEnum)
        {
            if ((options & ParserOptions.SkipEnums) > 0)
            {
                return Scalar.Int32;
            }

            fieldType = ParseEnum(type, options);
        }
        else
        {
            fieldType = ParseMessage(type, options);
        }

        Protobuf protobuf = ((INestableType)fieldType).Protobuf!;
        if (referencingProtobuf != protobuf)
        {
            referencingProtobuf.Imports.Add(protobuf.FileName);
        }

        return fieldType;
    }

    private Protobuf NewProtobuf(Type topLevelType, TopLevel topLevel)
    {
        Protobuf protobuf = new()
        {
            AssemblyName = topLevelType.Assembly.FullName,
            Namespace    = topLevelType.Namespace
        };

        topLevel.Protobuf = protobuf;
        protobuf.TopLevels.Add(topLevel);
        Protobufs.Add(protobuf);

        return protobuf;
    }

    private Protobuf GetProtobuf<T>(Type topLevelType, T topLevel, ParserOptions options)
        where T : TopLevel, INestableType
    {
        Protobuf protobuf;
        if (topLevelType.IsNested)
        {
            Type parent = topLevelType.DeclaringType!.DeclaringType!;
            if (!_parsed.TryGetValue(parent.FullName ?? parent.Name, out TopLevel? parentTopLevel))
            {
                parentTopLevel = ParseMessage(parent, options);
            }

            protobuf = parentTopLevel.Protobuf!;
            topLevel.Protobuf = protobuf;
            topLevel.Parent   = parentTopLevel;

            ((Message)parentTopLevel).Nested.Add(topLevelType.Name, topLevel);
        }
        else
        {
            protobuf = NewProtobuf(topLevelType, topLevel);
        }

        return protobuf;
    }

    private string TranslateMethodName(string methodName) =>
        NameLookup?.Invoke(methodName, out string? translatedName) == true
            ? translatedName
            : methodName;

    private string TranslateOneOfPropName(string oneOfPropName)
    {
        if (NameLookup?.Invoke(oneOfPropName, out string? translatedName) != true)
        {
            if (IsBeebyted(oneOfPropName))
            {
                return oneOfPropName;
            }

            translatedName = oneOfPropName;
        }

        return translatedName!.TrimEnd("Case").ToSnakeCaseLower();
    }

    private string TranslateMessageFieldName(string fieldName)
    {
        if (NameLookup?.Invoke(fieldName, out string? translatedName) != true)
        {
            if (IsBeebyted(fieldName))
            {
                return fieldName;
            }

            translatedName = fieldName;
        }

        return translatedName!.ToSnakeCaseLower();
    }

    private string TranslateEnumFieldName(FieldInfo field, string enumName)
    {
        if (field.GetCustomAttributesData()
                 .SingleOrDefault(static attr => attr.AttributeType.Name == "OriginalNameAttribute")
                ?.ConstructorArguments[0]
                 .Value
            is string originalName)
        {
            return originalName;
        }

        if (NameLookup?.Invoke(field.Name, out string? fieldName) != true)
        {
            fieldName = field.Name;
        }

        if (!IsBeebyted(fieldName!))
        {
            fieldName = fieldName!.ToSnakeCaseUpper();
        }

        if (!IsBeebyted(enumName))
        {
            enumName = enumName.ToSnakeCaseUpper();
        }

        return enumName + '_' + fieldName;
    }

    private string TranslateTypeName(Type type)
    {
        if (NameLookup is null)
            return type.Name;

        string? fullName = type.FullName;
        Guard.IsNotNull(fullName);

        int genericArgs = fullName.IndexOf('[');
        if (genericArgs != -1)
            fullName = fullName[..genericArgs];

        if (!NameLookup(fullName, out string? translatedName))
        {
            return type.Name;
        }

        int lastSlash = translatedName.LastIndexOf('/');
        if (lastSlash != -1)
            translatedName = translatedName[lastSlash..];

        int lastDot = translatedName.LastIndexOf('.');
        if (lastDot != -1)
            translatedName = translatedName[lastDot..];

        return translatedName;
    }

    public static bool LookupScalarAndWellKnownTypes(Type type, [NotNullWhen(true)] out IType? fieldType, out string? import)
    {
        switch (type.FullName)
        {
            case "System.String":
                import    = null;
                fieldType = Scalar.String;
                return true;
            case "System.Boolean":
                import    = null;
                fieldType = Scalar.Bool;
                return true;
            case "System.Double":
                import    = null;
                fieldType = Scalar.Double;
                return true;
            case "System.UInt32":
                import    = null;
                fieldType = Scalar.UInt32;
                return true;
            case "System.UInt64":
                import    = null;
                fieldType = Scalar.UInt64;
                return true;
            case "System.Int32":
                import    = null;
                fieldType = Scalar.Int32;
                return true;
            case "System.Int64":
                import    = null;
                fieldType = Scalar.Int64;
                return true;
            case "System.Single":
                import    = null;
                fieldType = Scalar.Float;
                return true;
            case "Google.Protobuf.ByteString":
                import    = null;
                fieldType = Scalar.Bytes;
                return true;
            case "Google.Protobuf.WellKnownTypes.Any":
                import    = "google/protobuf/any.proto";
                fieldType = WellKnown.Any;
                return true;
            case "Google.Protobuf.WellKnownTypes.Api":
                import    = "google/protobuf/api.proto";
                fieldType = WellKnown.Api;
                return true;
            case "Google.Protobuf.WellKnownTypes.BoolValue":
                import    = "google/protobuf/wrappers.proto";
                fieldType = WellKnown.BoolValue;
                return true;
            case "Google.Protobuf.WellKnownTypes.BytesValue":
                import    = "google/protobuf/wrappers.proto";
                fieldType = WellKnown.BytesValue;
                return true;
            case "Google.Protobuf.WellKnownTypes.DoubleValue":
                import    = "google/protobuf/wrappers.proto";
                fieldType = WellKnown.DoubleValue;
                return true;
            case "Google.Protobuf.WellKnownTypes.Duration":
                import    = "google/protobuf/duration.proto";
                fieldType = WellKnown.Duration;
                return true;
            case "Google.Protobuf.WellKnownTypes.Empty":
                import    = "google/protobuf/empty.proto";
                fieldType = WellKnown.Empty;
                return true;
            case "Google.Protobuf.WellKnownTypes.Enum":
                import    = "google/protobuf/type.proto";
                fieldType = WellKnown.Enum;
                return true;
            case "Google.Protobuf.WellKnownTypes.EnumValue":
                import    = "google/protobuf/type.proto";
                fieldType = WellKnown.EnumValue;
                return true;
            case "Google.Protobuf.WellKnownTypes.Field":
                import    = "google/protobuf/type.proto";
                fieldType = WellKnown.Field;
                return true;
            case "Google.Protobuf.WellKnownTypes.FieldMask":
                import    = "google/protobuf/field_mask.proto";
                fieldType = WellKnown.FieldMask;
                return true;
            case "Google.Protobuf.WellKnownTypes.FloatValue":
                import    = "google/protobuf/wrappers.proto";
                fieldType = WellKnown.FloatValue;
                return true;
            case "Google.Protobuf.WellKnownTypes.Int32Value":
                import    = "google/protobuf/wrappers.proto";
                fieldType = WellKnown.Int32Value;
                return true;
            case "Google.Protobuf.WellKnownTypes.Int64Value":
                import    = "google/protobuf/wrappers.proto";
                fieldType = WellKnown.Int64Value;
                return true;
            case "Google.Protobuf.WellKnownTypes.ListValue":
                import    = "google/protobuf/struct.proto";
                fieldType = WellKnown.ListValue;
                return true;
            case "Google.Protobuf.WellKnownTypes.Method":
                import    = "google/protobuf/api.proto";
                fieldType = WellKnown.Method;
                return true;
            case "Google.Protobuf.WellKnownTypes.Mixin":
                import    = "google/protobuf/api.proto";
                fieldType = WellKnown.Mixin;
                return true;
            case "Google.Protobuf.WellKnownTypes.NullValue":
                import    = "google/protobuf/struct.proto";
                fieldType = WellKnown.NullValue;
                return true;
            case "Google.Protobuf.WellKnownTypes.Option":
                import    = "google/protobuf/type.proto";
                fieldType = WellKnown.Option;
                return true;
            case "Google.Protobuf.WellKnownTypes.SourceContext":
                import    = "google/protobuf/source_context.proto";
                fieldType = WellKnown.SourceContext;
                return true;
            case "Google.Protobuf.WellKnownTypes.StringValue":
                import    = "google/protobuf/wrappers.proto";
                fieldType = WellKnown.StringValue;
                return true;
            case "Google.Protobuf.WellKnownTypes.Struct":
                import    = "google/protobuf/struct.proto";
                fieldType = WellKnown.Struct;
                return true;
            case "Google.Protobuf.WellKnownTypes.Syntax":
                import    = "google/protobuf/type.proto";
                fieldType = WellKnown.Syntax;
                return true;
            case "Google.Protobuf.WellKnownTypes.Timestamp":
                import    = "google/protobuf/timestamp.proto";
                fieldType = WellKnown.Timestamp;
                return true;
            case "Google.Protobuf.WellKnownTypes.Type":
                import    = "google/protobuf/type.proto";
                fieldType = WellKnown.Type;
                return true;
            case "Google.Protobuf.WellKnownTypes.UInt32Value":
                import    = "google/protobuf/wrappers.proto";
                fieldType = WellKnown.UInt32Value;
                return true;
            case "Google.Protobuf.WellKnownTypes.UInt64Value":
                import    = "google/protobuf/wrappers.proto";
                fieldType = WellKnown.UInt64Value;
                return true;
            case "Google.Protobuf.WellKnownTypes.Value":
                import    = "google/protobuf/struct.proto";
                fieldType = WellKnown.Value;
                return true;

            default:
                import    = null;
                fieldType = null;
                return false;
        }
    }

    // ReSharper disable once IdentifierTypo
    private static bool IsBeebyted(string name) =>
        name.Length == 11 && name.CountUpper() == 11;

    private static bool HasGeneratedCodeAttribute(IEnumerable<CustomAttributeData> attributes, string tool) =>
        attributes.Any(attr => attr.AttributeType.Name                      == nameof(GeneratedCodeAttribute)
                            && attr.ConstructorArguments[0].Value as string == tool);

    private static bool HasNonUserCodeAttribute(IEnumerable<CustomAttributeData> attributes) =>
        attributes.Any(static attr => attr.AttributeType.Name == nameof(DebuggerNonUserCodeAttribute));

    private static bool HasObsoleteAttribute(IEnumerable<CustomAttributeData> attributes) =>
        attributes.Any(static attr => attr.AttributeType.Name == nameof(ObsoleteAttribute));
}