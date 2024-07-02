// Copyright © 2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using CommunityToolkit.Diagnostics;
using LibCpp2IL;
using LibCpp2IL.Metadata;
using LibCpp2IL.Reflection;

namespace LibProtodec.Models.Cil.Il2Cpp;

public sealed class Il2CppType : Il2CppMember, ICilType
{
    private readonly Il2CppTypeDefinition _il2CppType;
    private readonly Il2CppTypeReflectionData[] _genericArgs;
    private ICilType[]? _genericTypeArguments;

    private Il2CppType(Il2CppTypeDefinition il2CppType, Il2CppTypeReflectionData[] genericArgs) =>
        (_il2CppType, _genericArgs) = (il2CppType, genericArgs);

    public string Name =>
        _il2CppType.Name!;

    public string FullName =>
        _il2CppType.FullName!;

    public string? Namespace =>
        _il2CppType.Namespace;

    public string DeclaringAssemblyName =>
        LibCpp2IlMain.TheMetadata!.GetStringFromIndex(
            DeclaringAssembly.nameIndex);

    public ICilType? DeclaringType =>
        IsNested
            ? GetOrCreate(
                LibCpp2ILUtils.GetTypeReflectionData(
                    LibCpp2IlMain.Binary!.GetType(
                        _il2CppType.DeclaringTypeIndex)))
            : null;

    public ICilType? BaseType =>
        _il2CppType.ParentIndex == -1
            ? null
            : GetOrCreate(
                LibCpp2ILUtils.GetTypeReflectionData(
                    LibCpp2IlMain.Binary!.GetType(
                        _il2CppType.ParentIndex)));

    public bool IsAbstract =>
        _il2CppType.IsAbstract;

    public bool IsClass =>
        (_il2CppType.Attributes & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Class
     && !_il2CppType.IsValueType;

    public bool IsEnum =>
        _il2CppType.IsEnumType;

    public bool IsNested =>
        _il2CppType.DeclaringTypeIndex >= 0;

    public bool IsSealed =>
        (_il2CppType.Attributes & TypeAttributes.Sealed) != 0;

    public IList<ICilType> GenericTypeArguments
    {
        get
        {
            if (_genericTypeArguments is null)
            {
                _genericTypeArguments = _genericArgs.Length < 1
                    ? Array.Empty<ICilType>()
                    : new ICilType[_genericArgs.Length];

                for (int i = 0; i < _genericArgs.Length; i++)
                {
                    _genericTypeArguments[i] = GetOrCreate(_genericArgs[i]);
                }
            }

            return _genericTypeArguments;
        }
    }

    public IEnumerable<ICilField> GetFields()
    {
        for (int i = 0; i < _il2CppType.FieldCount; i++)
        {
            yield return new Il2CppField(
                LibCpp2IlMain.TheMetadata!.fieldDefs[
                    _il2CppType.FirstFieldIdx + i]);
        }
    }

    public IEnumerable<ICilMethod> GetMethods()
    {
        for (int i = 0; i < _il2CppType.MethodCount; i++)
        {
            yield return new Il2CppMethod(
                LibCpp2IlMain.TheMetadata!.methodDefs[
                    _il2CppType.FirstMethodIdx + i]);
        }
    }

    public IEnumerable<ICilType> GetNestedTypes()
    {
        for (int i = 0; i < _il2CppType.NestedTypeCount; i++)
        {
            yield return GetOrCreate(
                LibCpp2IlMain.TheMetadata!.typeDefs[
                    LibCpp2IlMain.TheMetadata.nestedTypeIndices[
                        _il2CppType.NestedTypesStart + i]]);
        }
    }

    public IEnumerable<ICilProperty> GetProperties()
    {
        for (int i = 0; i < _il2CppType.PropertyCount; i++)
        {
            yield return new Il2CppProperty(
                LibCpp2IlMain.TheMetadata!.propertyDefs[
                    _il2CppType.FirstPropertyId + i],
                _il2CppType);
        }
    }

    public bool IsAssignableTo(ICilType type)
    {
        if (type is Il2CppType il2CppType)
        {
            return IsAssignableTo(_il2CppType, il2CppType._il2CppType);
        }

        return ThrowHelper.ThrowNotSupportedException<bool>();
    }

    protected override Il2CppImageDefinition DeclaringAssembly =>
        _il2CppType.DeclaringAssembly!;

    protected override int CustomAttributeIndex =>
        _il2CppType.CustomAttributeIndex;

    protected override uint Token =>
        _il2CppType.Token;


    private static readonly ConcurrentDictionary<string, Il2CppType> TypeLookup = [];

    public static ICilType GetOrCreate(Il2CppTypeDefinition il2CppType) =>
        TypeLookup.GetOrAdd(
            il2CppType.FullName!,
            static (_, il2CppType) =>
                new Il2CppType(il2CppType, Array.Empty<Il2CppTypeReflectionData>()),
            il2CppType);

    public static ICilType GetOrCreate(Il2CppTypeReflectionData il2CppTypeData)
    {
        Guard.IsTrue(il2CppTypeData.isType);

        return TypeLookup.GetOrAdd(
            il2CppTypeData.ToString(),
            static (_, il2CppTypeData) =>
                new Il2CppType(il2CppTypeData.baseType!, il2CppTypeData.genericParams),
            il2CppTypeData);
    }

    private static bool IsAssignableTo(Il2CppTypeDefinition thisType, Il2CppTypeDefinition baseType)
    {
        if (baseType.IsInterface)
        {
            foreach (Il2CppTypeReflectionData @interface in thisType.Interfaces!)
            {
                if (@interface.baseType == baseType)
                {
                    return true;
                }
            }
        }
        
        if (thisType == baseType)
        {
            return true;
        }

        Il2CppTypeDefinition? thisTypeBaseType = thisType.BaseType?.baseType;

        return thisTypeBaseType is not null
            && IsAssignableTo(thisTypeBaseType, baseType);
    }
}