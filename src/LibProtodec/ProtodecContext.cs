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
using LibProtodec.Models.Cil;
using LibProtodec.Models.Protobuf;
using LibProtodec.Models.Protobuf.Fields;
using LibProtodec.Models.Protobuf.TopLevels;
using LibProtodec.Models.Protobuf.Types;
using Microsoft.Extensions.Logging;

namespace LibProtodec;

public delegate bool NameLookupFunc(string name, [MaybeNullWhen(false)] out string translatedName);

// ReSharper disable ClassWithVirtualMembersNeverInherited.Global, MemberCanBePrivate.Global, MemberCanBeProtected.Global, PropertyCanBeMadeInitOnly.Global
public class ProtodecContext
{
    private readonly Dictionary<string, TopLevel> _parsed = [];

    public readonly List<Protobuf> Protobufs = [];

    public ILogger<ProtodecContext>? Logger { get; set; }

    public NameLookupFunc? NameLookup { get; set; }

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

    public virtual Message ParseMessage(ICilType messageClass, ParserOptions options = ParserOptions.None)
    {
        Guard.IsTrue(messageClass is { IsClass: true, IsSealed: true });
        using IDisposable? _ = Logger?.BeginScopeParsingMessage(messageClass.FullName);

        if (_parsed.TryGetValue(messageClass.FullName, out TopLevel? parsedMessage))
        {
            Logger?.LogParsedMessage(parsedMessage.Name);
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
            ICilProperty property     = properties[pi];
            ICilType     propertyType = property.Type;

            using IDisposable? __ = Logger?.BeginScopeParsingProperty(property.Name, propertyType.FullName);

            if (((options & ParserOptions.IncludePropertiesWithoutNonUserCodeAttribute) == 0 && !HasNonUserCodeAttribute(property.CustomAttributes)))
            {
                Logger?.LogSkippingPropertyWithoutNonUserCodeAttribute();
                continue;
            }

            // only OneOf enums are defined nested directly in the message class
            if (propertyType.IsEnum && propertyType.DeclaringType?.Name == messageClass.Name)
            {
                string oneOfName = TranslateOneOfPropName(property.Name);
                Logger?.LogParsedOneOfField(oneOfName);

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

            if (idFields.Count <= fi)
            {
                Logger?.LogFailedToLocateIdField();
                continue;
            }

            MessageField field = new(message)
            {
                Type       = ParseFieldType(propertyType, options, protobuf),
                Name       = TranslateMessageFieldName(property.Name),
                Id         = (int)idFields[fi].ConstantValue!,
                IsObsolete = HasObsoleteAttribute(property.CustomAttributes),
                HasHasProp = msgFieldHasHasProp
            };

            Logger?.LogParsedField(field.Name, field.Id, field.Type.Name);
            message.Fields.Add(field.Id, field);
            fi++;
        }

        Logger?.LogParsedMessage(message.Name);
        return message;
    }

    public virtual Enum ParseEnum(ICilType enumEnum, ParserOptions options = ParserOptions.None)
    {
        Guard.IsTrue(enumEnum.IsEnum);
        using IDisposable? _ = Logger?.BeginScopeParsingEnum(enumEnum.FullName);

        if (_parsed.TryGetValue(enumEnum.FullName, out TopLevel? parsedEnum))
        {
            Logger?.LogParsedEnum(parsedEnum.Name);
            return (Enum)parsedEnum;
        }

        Enum @enum = new()
        {
            Name         = TranslateTypeName(enumEnum),
            IsObsolete   = HasObsoleteAttribute(enumEnum.CustomAttributes)
        };
        _parsed.Add(enumEnum.FullName, @enum);

        Protobuf protobuf = GetProtobuf(enumEnum, @enum, options);

        foreach (ICilField enumField in enumEnum.GetFields().Where(static field => field.IsLiteral))
        {
            using IDisposable? __ = Logger?.BeginScopeParsingField(enumField.Name);

            EnumField field = new()
            {
                Id         = (int)enumField.ConstantValue!,
                Name       = TranslateEnumFieldName(enumField.CustomAttributes, enumField.Name, @enum.Name),
                IsObsolete = HasObsoleteAttribute(enumField.CustomAttributes)
            };

            Logger?.LogParsedField(field.Name, field.Id);
            @enum.Fields.Add(field);
        }

        if (@enum.Fields.All(static field => field.Id != 0))
        {
            protobuf.Edition = "2023";
            @enum.IsClosed   = true;
        }

        Logger?.LogParsedEnum(@enum.Name);
        return @enum;
    }

    public virtual Service ParseService(ICilType serviceClass, ParserOptions options = ParserOptions.None)
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
        using IDisposable? _ = Logger?.BeginScopeParsingService(serviceClass.DeclaringType!.FullName);

        if (_parsed.TryGetValue(serviceClass.DeclaringType!.FullName, out TopLevel? parsedService))
        {
            Logger?.LogParsedService(parsedService.Name);
            return (Service)parsedService;
        }

        Service service = new()
        {
            Name         = TranslateTypeName(serviceClass.DeclaringType),
            IsObsolete   = HasObsoleteAttribute(serviceClass.CustomAttributes)
        };
        _parsed.Add(serviceClass.DeclaringType!.FullName, service);

        Protobuf protobuf = NewProtobuf(serviceClass, service);

        foreach (ICilMethod cilMethod in serviceClass.GetMethods().Where(static method => method is { IsInherited: false, IsPublic: true, IsStatic: false, IsConstructor: false }))
        {
            using IDisposable? __ = Logger?.BeginScopeParsingMethod(cilMethod.Name);

            if ((options & ParserOptions.IncludeServiceMethodsWithoutGeneratedCodeAttribute) == 0
             && !HasGeneratedCodeAttribute(cilMethod.CustomAttributes, "grpc_csharp_plugin"))
            {
                Logger?.LogSkippingMethodWithoutGeneratedCodeAttribute();
                continue;
            }

            ICilType requestType, responseType, returnType = cilMethod.ReturnType;
            bool streamReq, streamRes;

            if (isClientClass.Value)
            {
                string returnTypeName = TranslateTypeName(returnType);
                if (returnTypeName == "AsyncUnaryCall`1")
                {
                    Logger?.LogSkippingDuplicateMethod();
                    continue;
                }

                List<ICilType> parameters = cilMethod.GetParameterTypes().ToList();
                if (parameters.Count > 2)
                {
                    Logger?.LogSkippingDuplicateMethod();
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
                List<ICilType> parameters = cilMethod.GetParameterTypes().ToList();

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

            ServiceMethod method = new(service)
            {
                Name               = TranslateMethodName(cilMethod.Name),
                IsObsolete         = HasObsoleteAttribute(cilMethod.CustomAttributes),
                RequestType        = ParseFieldType(requestType,  options, protobuf),
                ResponseType       = ParseFieldType(responseType, options, protobuf),
                IsRequestStreamed  = streamReq,
                IsResponseStreamed = streamRes
            };

            Logger?.LogParsedMethod(method.Name, method.RequestType.Name, method.ResponseType.Name);
            service.Methods.Add(method);
        }

        Logger?.LogParsedService(service.Name);
        return service;
    }

    protected IProtobufType ParseFieldType(ICilType type, ParserOptions options, Protobuf referencingProtobuf)
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

        if (!LookupType(type, out IProtobufType? fieldType))
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

    protected virtual bool LookupType(ICilType cilType, [NotNullWhen(true)] out IProtobufType? protobufType)
    {
        switch (cilType.FullName)
        {
            case "System.String":
                protobufType = Scalar.String;
                break;
            case "System.Boolean":
                protobufType = Scalar.Bool;
                break;
            case "System.Double":
                protobufType = Scalar.Double;
                break;
            case "System.UInt32":
                protobufType = Scalar.UInt32;
                break;
            case "System.UInt64":
                protobufType = Scalar.UInt64;
                break;
            case "System.Int32":
                protobufType = Scalar.Int32;
                break;
            case "System.Int64":
                protobufType = Scalar.Int64;
                break;
            case "System.Single":
                protobufType = Scalar.Float;
                break;
            case "Google.Protobuf.ByteString":
                protobufType = Scalar.Bytes;
                break;

            case "Google.Protobuf.WellKnownTypes.Any":
                protobufType = WellKnown.Any;
                break;
            case "Google.Protobuf.WellKnownTypes.Api":
                protobufType = WellKnown.Api;
                break;
            case "Google.Protobuf.WellKnownTypes.BoolValue":
                protobufType = WellKnown.BoolValue;
                break;
            case "Google.Protobuf.WellKnownTypes.BytesValue":
                protobufType = WellKnown.BytesValue;
                break;
            case "Google.Protobuf.WellKnownTypes.DoubleValue":
                protobufType = WellKnown.DoubleValue;
                break;
            case "Google.Protobuf.WellKnownTypes.Duration":
                protobufType = WellKnown.Duration;
                break;
            case "Google.Protobuf.WellKnownTypes.Empty":
                protobufType = WellKnown.Empty;
                break;
            case "Google.Protobuf.WellKnownTypes.Enum":
                protobufType = WellKnown.Enum;
                break;
            case "Google.Protobuf.WellKnownTypes.EnumValue":
                protobufType = WellKnown.EnumValue;
                break;
            case "Google.Protobuf.WellKnownTypes.Field":
                protobufType = WellKnown.Field;
                break;
            case "Google.Protobuf.WellKnownTypes.FieldMask":
                protobufType = WellKnown.FieldMask;
                break;
            case "Google.Protobuf.WellKnownTypes.FloatValue":
                protobufType = WellKnown.FloatValue;
                break;
            case "Google.Protobuf.WellKnownTypes.Int32Value":
                protobufType = WellKnown.Int32Value;
                break;
            case "Google.Protobuf.WellKnownTypes.Int64Value":
                protobufType = WellKnown.Int64Value;
                break;
            case "Google.Protobuf.WellKnownTypes.ListValue":
                protobufType = WellKnown.ListValue;
                break;
            case "Google.Protobuf.WellKnownTypes.Method":
                protobufType = WellKnown.Method;
                break;
            case "Google.Protobuf.WellKnownTypes.Mixin":
                protobufType = WellKnown.Mixin;
                break;
            case "Google.Protobuf.WellKnownTypes.NullValue":
                protobufType = WellKnown.NullValue;
                break;
            case "Google.Protobuf.WellKnownTypes.Option":
                protobufType = WellKnown.Option;
                break;
            case "Google.Protobuf.WellKnownTypes.SourceContext":
                protobufType = WellKnown.SourceContext;
                break;
            case "Google.Protobuf.WellKnownTypes.StringValue":
                protobufType = WellKnown.StringValue;
                break;
            case "Google.Protobuf.WellKnownTypes.Struct":
                protobufType = WellKnown.Struct;
                break;
            case "Google.Protobuf.WellKnownTypes.Syntax":
                protobufType = WellKnown.Syntax;
                break;
            case "Google.Protobuf.WellKnownTypes.Timestamp":
                protobufType = WellKnown.Timestamp;
                break;
            case "Google.Protobuf.WellKnownTypes.Type":
                protobufType = WellKnown.Type;
                break;
            case "Google.Protobuf.WellKnownTypes.UInt32Value":
                protobufType = WellKnown.UInt32Value;
                break;
            case "Google.Protobuf.WellKnownTypes.UInt64Value":
                protobufType = WellKnown.UInt64Value;
                break;
            case "Google.Protobuf.WellKnownTypes.Value":
                protobufType = WellKnown.Value;
                break;

            default:
                protobufType = null;
                return false;
        }

        return true;
    }

    protected Protobuf NewProtobuf(ICilType topLevelType, TopLevel topLevel)
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

    protected Protobuf GetProtobuf<T>(ICilType topLevelType, T topLevel, ParserOptions options)
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

    protected string TranslateMethodName(string methodName) =>
        NameLookup?.Invoke(methodName, out string? translatedName) == true
            ? translatedName
            : methodName;

    protected string TranslateOneOfPropName(string oneOfPropName)
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

    protected string TranslateMessageFieldName(string fieldName)
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

    protected string TranslateEnumFieldName(IEnumerable<ICilAttribute> attributes, string fieldName, string enumName)
    {
        if (attributes.SingleOrDefault(static attr => attr.Type.Name == "OriginalNameAttribute")
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

    protected string TranslateTypeName(ICilType type)
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

    // ReSharper disable once IdentifierTypo
    protected static bool IsBeebyted(string name) =>
        name.Length == 11 && name.CountUpper() == 11;

    protected static bool HasGeneratedCodeAttribute(IEnumerable<ICilAttribute> attributes, string tool) =>
        attributes.Any(attr => attr.Type.Name                              == nameof(GeneratedCodeAttribute)
                            && attr.ConstructorArgumentValues[0] as string == tool);

    protected static bool HasNonUserCodeAttribute(IEnumerable<ICilAttribute> attributes) =>
        attributes.Any(static attr => attr.Type.Name == nameof(DebuggerNonUserCodeAttribute));

    protected static bool HasObsoleteAttribute(IEnumerable<ICilAttribute> attributes) =>
        attributes.Any(static attr => attr.Type.Name == nameof(ObsoleteAttribute));
}