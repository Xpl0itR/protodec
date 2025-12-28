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

namespace LibProtodec.Models.Cil.Clr;

public sealed class ClrType : ClrMember, ICilType
{
    private const BindingFlags Everything = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;

    private readonly Type _clrType;
    private ICilType[]? _genericTypeArguments;

    private ClrType(Type clrType) : base(clrType) =>
        _clrType = clrType;

    public string FullName =>
        _clrType.FullName ?? _clrType.Name;

    public string? Namespace =>
        _clrType.Namespace;

    public string DeclaringAssemblyName =>
        _clrType.Assembly.FullName!;

    public ICilModule DeclaringModule =>
        ClrModule.GetOrCreate(
            _clrType.Module);

    public ICilType? BaseType =>
        _clrType.BaseType is null
            ? null
            : GetOrCreate(
                _clrType.BaseType);

    public bool IsAbstract =>
        _clrType.IsAbstract;

    public bool IsClass =>
        _clrType.IsClass;

    public bool IsEnum =>
        _clrType.IsEnum;

    public bool IsNested =>
        _clrType.IsNested;

    public bool IsSealed =>
        _clrType.IsSealed;

    public IList<ICilType> GenericTypeArguments
    {
        get
        {
            if (_genericTypeArguments is null)
            {
                Type[] args = _clrType.GenericTypeArguments;

                _genericTypeArguments = args.Length < 1
                    ? Array.Empty<ICilType>()
                    : new ICilType[args.Length];

                for (int i = 0; i < args.Length; i++)
                {
                    _genericTypeArguments[i] = GetOrCreate(args[i]);
                }
            }

            return _genericTypeArguments;
        }
    }

    public IEnumerable<ICilField> GetFields()
    {
        foreach (FieldInfo field in _clrType.GetFields(Everything))
        {
            yield return new ClrField(field);
        }
    }

    public IEnumerable<ICilMethod> GetMethods()
    {
        foreach (MethodInfo method in _clrType.GetMethods(Everything))
        {
            yield return new ClrMethod(method);
        }
    }

    public IEnumerable<ICilType> GetNestedTypes()
    {
        foreach (Type type in _clrType.GetNestedTypes(Everything))
        {
            yield return GetOrCreate(type);
        }
    }

    public IEnumerable<ICilProperty> GetProperties()
    {
        foreach (PropertyInfo property in _clrType.GetProperties(Everything))
        {
            yield return new ClrProperty(property);
        }
    }

    public bool IsAssignableTo(ICilType type)
    {
        if (type is ClrType clrType)
        {
            return _clrType.IsAssignableTo(clrType._clrType);
        }

        return ThrowHelper.ThrowNotSupportedException<bool>();
    }


    private static readonly ConcurrentDictionary<string, ClrType> TypeLookup = [];

    public static ICilType GetOrCreate(Type clrType) =>
        TypeLookup.GetOrAdd(
            clrType.FullName ?? clrType.Name,
            static (_, clrType) => new ClrType(clrType),
            clrType);
}