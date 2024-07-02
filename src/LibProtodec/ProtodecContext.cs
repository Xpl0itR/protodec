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
using SystemEx;
using CommunityToolkit.Diagnostics;
using LibCpp2IL;
using LibProtodec.Models.Cil;
using LibProtodec.Models.Protobuf;
using LibProtodec.Models.Protobuf.Fields;
using LibProtodec.Models.Protobuf.TopLevels;
using LibProtodec.Models.Protobuf.Types;

namespace LibProtodec;

public delegate bool TypeLookupFunc(ICilType cilType, [NotNullWhen(true)] out IProtobufType? protobufType);
public delegate bool NameLookupFunc(string name, [MaybeNullWhen(false)] out string translatedName);

public sealed class ProtodecContext
{
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

    public Message ParseMessage(ICilType messageClass, ParserOptions options = ParserOptions.None)
    {
        Guard.IsTrue(messageClass is { IsClass: true, IsSealed: true });

        if (_parsed.TryGetValue(messageClass.FullName, out TopLevel? parsedMessage))
        {
            return (Message)parsedMessage;
        }

        Message message = new()
        {
            Name       = TranslateTypeName(messageClass),
            IsObsolete = HasObsoleteAttribute(messageClass.CustomAttributes)
        };
        _parsed.Add(messageClass.FullName, message);

        Protobuf protobuf = GetProtobuf(messageClass, message, options);

        List<ICilField> idFields = messageClass.GetFields()
                                               .Where(static field => field is { IsPublic: true, IsStatic: true, IsLiteral: true })
                                               .ToList();

        List<ICilProperty> properties = messageClass.GetProperties()
                                                    .Where(static property => property is { IsInherited: false, CanRead: true, Getter: { IsPublic: true, IsStatic: false, IsVirtual: false } })
                                                    .ToList();

        for (int pi = 0, fi = 0; pi < properties.Count; pi++)
        {
            ICilProperty property = properties[pi];
            if (((options & ParserOptions.IncludePropertiesWithoutNonUserCodeAttribute) == 0 && !HasNonUserCodeAttribute(property.CustomAttributes)))
            {
                continue;
            }

            ICilType propertyType = property.Type;

            // only OneOf enums are defined nested directly in the message class
            if (propertyType.IsEnum && propertyType.DeclaringType?.Name == messageClass.Name)
            {
                string oneOfName = TranslateOneOfPropName(property.Name);
                List<int> oneOfProtoFieldIds = propertyType.GetFields()
                                                           .Where(static field => field.IsLiteral)
                                                           .Select(static field => (int)field.ConstantValue!)
                                                           .Where(static id => id > 0)
                                                           .ToList();

                message.OneOfs.Add(oneOfName, oneOfProtoFieldIds);
                continue;
            }

            bool msgFieldHasHasProp = false; // some field properties are immediately followed by an additional "Has" get-only boolean property
            if (properties.Count > pi + 1 && properties[pi + 1].Type.Name == nameof(Boolean) && !properties[pi + 1].CanWrite)
            {
                msgFieldHasHasProp = true;
                pi++;
            }

            MessageField field = new()
            {
                Type       = ParseFieldType(propertyType, options, protobuf),
                Name       = TranslateMessageFieldName(property.Name),
                Id         = (int)idFields[fi].ConstantValue!,
                IsObsolete = HasObsoleteAttribute(property.CustomAttributes),
                HasHasProp = msgFieldHasHasProp
            };

            message.Fields.Add(field.Id, field);
            fi++;
        }

        return message;
    }

    public Enum ParseEnum(ICilType enumEnum, ParserOptions options = ParserOptions.None)
    {
        Guard.IsTrue(enumEnum.IsEnum);

        if (_parsed.TryGetValue(enumEnum.FullName, out TopLevel? parsedEnum))
        {
            return (Enum)parsedEnum;
        }

        Enum @enum = new()
        {
            Name         = TranslateTypeName(enumEnum),
            IsObsolete   = HasObsoleteAttribute(enumEnum.CustomAttributes)
        };
        _parsed.Add(enumEnum.FullName, @enum);

        Protobuf protobuf = GetProtobuf(enumEnum, @enum, options);

        foreach (ICilField field in enumEnum.GetFields().Where(static field => field.IsLiteral))
        {
            @enum.Fields.Add(
                new EnumField
                {
                    Id         = (int)field.ConstantValue!,
                    Name       = TranslateEnumFieldName(field.CustomAttributes, field.Name, @enum.Name),
                    IsObsolete = HasObsoleteAttribute(field.CustomAttributes)
                });
        }

        if (@enum.Fields.All(static field => field.Id != 0))
        {
            protobuf.Edition = "2023";
            @enum.IsClosed   = true;
        }

        return @enum;
    }

    public Service ParseService(ICilType serviceClass, ParserOptions options = ParserOptions.None)
    {
        Guard.IsTrue(serviceClass.IsClass);

        bool? isClientClass = null;
        if (serviceClass.IsAbstract)
        {
            if (serviceClass is { IsSealed: true, IsNested: false })
            {
                List<ICilType> nested = serviceClass.GetNestedTypes().ToList();
                serviceClass = nested.SingleOrDefault(static nested => nested is { IsAbstract: true, IsSealed: false }) 
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

        if (_parsed.TryGetValue(serviceClass.DeclaringType!.FullName, out TopLevel? parsedService))
        {
            return (Service)parsedService;
        }

        Service service = new()
        {
            Name         = TranslateTypeName(serviceClass.DeclaringType),
            IsObsolete   = HasObsoleteAttribute(serviceClass.CustomAttributes)
        };
        _parsed.Add(serviceClass.DeclaringType!.FullName, service);

        Protobuf protobuf = NewProtobuf(serviceClass, service);

        foreach (ICilMethod method in serviceClass.GetMethods().Where(static method => method is { IsInherited: false, IsPublic: true, IsStatic: false }))
        {
            if ((options & ParserOptions.IncludeServiceMethodsWithoutGeneratedCodeAttribute) == 0
             && !HasGeneratedCodeAttribute(method.CustomAttributes, "grpc_csharp_plugin"))
            {
                continue;
            }

            ICilType requestType, responseType, returnType = method.ReturnType;
            bool streamReq, streamRes;

            if (isClientClass.Value)
            {
                string returnTypeName = TranslateTypeName(returnType);
                if (returnTypeName == "AsyncUnaryCall`1")
                {
                    continue;
                }

                List<ICilType> parameters = method.GetParameterTypes().ToList();
                if (parameters.Count > 2)
                {
                    continue;
                }

                switch (returnType.GenericTypeArguments.Count)
                {
                    case 2:
                        requestType  = returnType.GenericTypeArguments[0];
                        responseType = returnType.GenericTypeArguments[1];
                        streamReq    = true;
                        streamRes    = returnTypeName == "AsyncDuplexStreamingCall`2";
                        break;
                    case 1:
                        requestType  = parameters[0];
                        responseType = returnType.GenericTypeArguments[0];
                        streamReq    = false;
                        streamRes    = true;
                        break;
                    default:
                        requestType  = parameters[0];
                        responseType = returnType;
                        streamReq    = false;
                        streamRes    = false;
                        break;
                }
            }
            else
            {
                List<ICilType> parameters = method.GetParameterTypes().ToList();

                if (parameters[0].GenericTypeArguments.Count == 1)
                {
                    streamReq   = true;
                    requestType = parameters[0].GenericTypeArguments[0];
                }
                else
                {
                    streamReq   = false;
                    requestType = parameters[0];
                }

                if (returnType.GenericTypeArguments.Count == 1)
                {
                    streamRes    = false;
                    responseType = returnType.GenericTypeArguments[0];
                }
                else
                {
                    streamRes    = true;
                    responseType = parameters[1].GenericTypeArguments[0];
                }
            }

            service.Methods.Add(
                new ServiceMethod
                {
                    Name               = TranslateMethodName(method.Name),
                    IsObsolete         = HasObsoleteAttribute(method.CustomAttributes),
                    RequestType        = ParseFieldType(requestType,  options, protobuf),
                    ResponseType       = ParseFieldType(responseType, options, protobuf),
                    IsRequestStreamed  = streamReq,
                    IsResponseStreamed = streamRes
                });
        }

        return service;
    }

    private IProtobufType ParseFieldType(ICilType type, ParserOptions options, Protobuf referencingProtobuf)
    {
        switch (type.GenericTypeArguments.Count)
        {
            case 1:
                return new Repeated(
                    ParseFieldType(type.GenericTypeArguments[0], options, referencingProtobuf));
            case 2:
                return new Map(
                    ParseFieldType(type.GenericTypeArguments[0], options, referencingProtobuf),
                    ParseFieldType(type.GenericTypeArguments[1], options, referencingProtobuf));
        }

        if (!TypeLookup(type, out IProtobufType? fieldType))
        {
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
        }

        switch (fieldType)
        {
            case WellKnown wellKnown:
                referencingProtobuf.Imports.Add(
                    wellKnown.FileName);
                break;
            case INestableType nestableType:
                Protobuf protobuf = nestableType.Protobuf!;
                if (referencingProtobuf != protobuf)
                    referencingProtobuf.Imports.Add(
                        protobuf.FileName);
                break;
        }

        return fieldType;
    }

    private Protobuf NewProtobuf(ICilType topLevelType, TopLevel topLevel)
    {
        Protobuf protobuf = new()
        {
            AssemblyName = topLevelType.DeclaringAssemblyName,
            Namespace    = topLevelType.Namespace
        };

        topLevel.Protobuf = protobuf;
        protobuf.TopLevels.Add(topLevel);
        Protobufs.Add(protobuf);

        return protobuf;
    }

    private Protobuf GetProtobuf<T>(ICilType topLevelType, T topLevel, ParserOptions options)
        where T : TopLevel, INestableType
    {
        Protobuf protobuf;
        if (topLevelType.IsNested)
        {
            ICilType parent = topLevelType.DeclaringType!.DeclaringType!;
            if (!_parsed.TryGetValue(parent.FullName, out TopLevel? parentTopLevel))
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

    private string TranslateEnumFieldName(IEnumerable<ICilAttribute> attributes, string fieldName, string enumName)
    {
        if (LibCpp2IlMain.MetadataVersion >= 29f //TODO: do not merge into master until il2cpp-specific global is removed
         && attributes.SingleOrDefault(static attr => attr.Type.Name == "OriginalNameAttribute")
                     ?.ConstructorArgumentValues[0] is string originalName)
        {
            return originalName;
        }

        if (NameLookup?.Invoke(fieldName, out string? translatedName) == true)
        {
            fieldName = translatedName;
        }

        if (!IsBeebyted(fieldName))
        {
            fieldName = fieldName.ToSnakeCaseUpper();
        }

        if (!IsBeebyted(enumName))
        {
            enumName = enumName.ToSnakeCaseUpper();
        }

        return enumName + '_' + fieldName;
    }

    private string TranslateTypeName(ICilType type)
    {
        if (NameLookup is null)
            return type.Name;

        string fullName = type.FullName;
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

    public static bool LookupScalarAndWellKnownTypes(ICilType cilType, [NotNullWhen(true)] out IProtobufType? protobufType)
    {
        switch (cilType.FullName)
        {
            case "System.String":
                protobufType = Scalar.String;
                return true;
            case "System.Boolean":
                protobufType = Scalar.Bool;
                return true;
            case "System.Double":
                protobufType = Scalar.Double;
                return true;
            case "System.UInt32":
                protobufType = Scalar.UInt32;
                return true;
            case "System.UInt64":
                protobufType = Scalar.UInt64;
                return true;
            case "System.Int32":
                protobufType = Scalar.Int32;
                return true;
            case "System.Int64":
                protobufType = Scalar.Int64;
                return true;
            case "System.Single":
                protobufType = Scalar.Float;
                return true;
            case "Google.Protobuf.ByteString":
                protobufType = Scalar.Bytes;
                return true;
            case "Google.Protobuf.WellKnownTypes.Any":
                protobufType = WellKnown.Any;
                return true;
            case "Google.Protobuf.WellKnownTypes.Api":
                protobufType = WellKnown.Api;
                return true;
            case "Google.Protobuf.WellKnownTypes.BoolValue":
                protobufType = WellKnown.BoolValue;
                return true;
            case "Google.Protobuf.WellKnownTypes.BytesValue":
                protobufType = WellKnown.BytesValue;
                return true;
            case "Google.Protobuf.WellKnownTypes.DoubleValue":
                protobufType = WellKnown.DoubleValue;
                return true;
            case "Google.Protobuf.WellKnownTypes.Duration":
                protobufType = WellKnown.Duration;
                return true;
            case "Google.Protobuf.WellKnownTypes.Empty":
                protobufType = WellKnown.Empty;
                return true;
            case "Google.Protobuf.WellKnownTypes.Enum":
                protobufType = WellKnown.Enum;
                return true;
            case "Google.Protobuf.WellKnownTypes.EnumValue":
                protobufType = WellKnown.EnumValue;
                return true;
            case "Google.Protobuf.WellKnownTypes.Field":
                protobufType = WellKnown.Field;
                return true;
            case "Google.Protobuf.WellKnownTypes.FieldMask":
                protobufType = WellKnown.FieldMask;
                return true;
            case "Google.Protobuf.WellKnownTypes.FloatValue":
                protobufType = WellKnown.FloatValue;
                return true;
            case "Google.Protobuf.WellKnownTypes.Int32Value":
                protobufType = WellKnown.Int32Value;
                return true;
            case "Google.Protobuf.WellKnownTypes.Int64Value":
                protobufType = WellKnown.Int64Value;
                return true;
            case "Google.Protobuf.WellKnownTypes.ListValue":
                protobufType = WellKnown.ListValue;
                return true;
            case "Google.Protobuf.WellKnownTypes.Method":
                protobufType = WellKnown.Method;
                return true;
            case "Google.Protobuf.WellKnownTypes.Mixin":
                protobufType = WellKnown.Mixin;
                return true;
            case "Google.Protobuf.WellKnownTypes.NullValue":
                protobufType = WellKnown.NullValue;
                return true;
            case "Google.Protobuf.WellKnownTypes.Option":
                protobufType = WellKnown.Option;
                return true;
            case "Google.Protobuf.WellKnownTypes.SourceContext":
                protobufType = WellKnown.SourceContext;
                return true;
            case "Google.Protobuf.WellKnownTypes.StringValue":
                protobufType = WellKnown.StringValue;
                return true;
            case "Google.Protobuf.WellKnownTypes.Struct":
                protobufType = WellKnown.Struct;
                return true;
            case "Google.Protobuf.WellKnownTypes.Syntax":
                protobufType = WellKnown.Syntax;
                return true;
            case "Google.Protobuf.WellKnownTypes.Timestamp":
                protobufType = WellKnown.Timestamp;
                return true;
            case "Google.Protobuf.WellKnownTypes.Type":
                protobufType = WellKnown.Type;
                return true;
            case "Google.Protobuf.WellKnownTypes.UInt32Value":
                protobufType = WellKnown.UInt32Value;
                return true;
            case "Google.Protobuf.WellKnownTypes.UInt64Value":
                protobufType = WellKnown.UInt64Value;
                return true;
            case "Google.Protobuf.WellKnownTypes.Value":
                protobufType = WellKnown.Value;
                return true;
            default:
                protobufType = null;
                return false;
        }
    }

    // ReSharper disable once IdentifierTypo
    private static bool IsBeebyted(string name) =>
        name.Length == 11 && name.CountUpper() == 11;

    private static bool HasGeneratedCodeAttribute(IEnumerable<ICilAttribute> attributes, string tool) =>
        attributes.Any(attr => attr.Type.Name == nameof(GeneratedCodeAttribute)
                            && (LibCpp2IlMain.MetadataVersion < 29f //TODO: do not merge into master until il2cpp-specific global is removed
                             || attr.ConstructorArgumentValues[0] as string == tool));

    private static bool HasNonUserCodeAttribute(IEnumerable<ICilAttribute> attributes) =>
        attributes.Any(static attr => attr.Type.Name == nameof(DebuggerNonUserCodeAttribute));

    private static bool HasObsoleteAttribute(IEnumerable<ICilAttribute> attributes) =>
        attributes.Any(static attr => attr.Type.Name == nameof(ObsoleteAttribute));
}