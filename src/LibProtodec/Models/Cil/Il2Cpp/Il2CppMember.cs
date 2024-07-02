// Copyright © 2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Text;
using SystemEx.Memory;
using CommunityToolkit.Diagnostics;
using LibCpp2IL;
using LibCpp2IL.BinaryStructures;
using LibCpp2IL.Metadata;

namespace LibProtodec.Models.Cil.Il2Cpp;

public abstract class Il2CppMember
{
    private ICilAttribute[]? _customAttributes;

    public IList<ICilAttribute> CustomAttributes
    {
        get
        {
            if (_customAttributes is null)
            {
                if (LibCpp2IlMain.MetadataVersion < 29f)
                {
                    int attrTypeRngIdx = LibCpp2IlMain.MetadataVersion <= 24f
                        ? CustomAttributeIndex
                        : BinarySearchToken(
                            LibCpp2IlMain.TheMetadata!.attributeTypeRanges,
                            Token,
                            DeclaringAssembly.customAttributeStart,
                            (int)DeclaringAssembly.customAttributeCount);

                    if (attrTypeRngIdx < 0)
                        return _customAttributes = Array.Empty<ICilAttribute>();

                    Il2CppCustomAttributeTypeRange attrTypeRng = LibCpp2IlMain.TheMetadata!.attributeTypeRanges[attrTypeRngIdx];

                    _customAttributes = new ICilAttribute[attrTypeRng.count];
                    for (int attrTypeIdx = 0; attrTypeIdx < attrTypeRng.count; attrTypeIdx++)
                    {
                        int typeIndex = LibCpp2IlMain.TheMetadata.attributeTypes[attrTypeRng.start + attrTypeIdx];
                        var type      = LibCpp2IlMain.Binary!.GetType(typeIndex);
                        var typeDef   = LibCpp2IlMain.TheMetadata.typeDefs[type.Data.ClassIndex];

                        _customAttributes[attrTypeIdx] = new Il2CppAttribute(typeDef, null);
                    }
                }
                else
                {
                    int attrDataRngIdx = BinarySearchToken(
                        LibCpp2IlMain.TheMetadata!.AttributeDataRanges,
                        Token,
                        DeclaringAssembly.customAttributeStart,
                        (int)DeclaringAssembly.customAttributeCount);

                    if (attrDataRngIdx < 0)
                        return _customAttributes = Array.Empty<ICilAttribute>();

                    Il2CppCustomAttributeDataRange attrDataRange   = LibCpp2IlMain.TheMetadata.AttributeDataRanges[attrDataRngIdx];
                    Il2CppCustomAttributeDataRange attrDataRngNext = LibCpp2IlMain.TheMetadata.AttributeDataRanges[attrDataRngIdx + 1];

                    long   attrDataStart = LibCpp2IlMain.TheMetadata.metadataHeader.attributeDataOffset + attrDataRange.startOffset;
                    long   attrDataEnd   = LibCpp2IlMain.TheMetadata.metadataHeader.attributeDataOffset + attrDataRngNext.startOffset;
                    byte[] attrData      = LibCpp2IlMain.TheMetadata.ReadByteArrayAtRawAddress(attrDataStart, (int)(attrDataEnd - attrDataStart));

                    MemoryReader reader = new(attrData);
                    int attributeCount = (int)ReadUnityCompressedUInt32(ref reader);

                    Span<uint> ctorIndices = stackalloc uint[attributeCount];
                    for (int i = 0; i < attributeCount; i++)
                        ctorIndices[i] = reader.ReadUInt32LittleEndian();

                    _customAttributes = new ICilAttribute[attributeCount];
                    for (int i = 0; i < attributeCount; i++)
                    {
                        uint ctorArgCount = ReadUnityCompressedUInt32(ref reader);
                        uint fieldCount   = ReadUnityCompressedUInt32(ref reader);
                        uint propCount    = ReadUnityCompressedUInt32(ref reader);

                        object?[] ctorArgValues = ctorArgCount > 0
                            ? new object[ctorArgCount]
                            : Array.Empty<object?>();

                        for (int j = 0; j < ctorArgCount; j++)
                        {
                            ctorArgValues[j] = ReadValue(ref reader);
                        }

                        for (uint j = 0; j < fieldCount; j++)
                        {
                            ReadValue(ref reader);
                            ResolveMember(ref reader);
                        }

                        for (uint j = 0; j < propCount; j++)
                        {
                            ReadValue(ref reader);
                            ResolveMember(ref reader);
                        }

                        Il2CppMethodDefinition attrCtor = LibCpp2IlMain.TheMetadata.methodDefs[ctorIndices[i]];
                        Il2CppTypeDefinition   attrType = LibCpp2IlMain.TheMetadata.typeDefs[attrCtor.declaringTypeIdx];

                        _customAttributes[i] = new Il2CppAttribute(attrType, ctorArgValues);
                    }
                }
            }

            return _customAttributes;
        }
    }

    protected abstract Il2CppImageDefinition DeclaringAssembly { get; }

    protected abstract int CustomAttributeIndex { get; }

    protected abstract uint Token { get; }

    private static int BinarySearchToken(IReadOnlyList<IIl2CppTokenProvider> source, uint token, int start, int count)
    {
        int lo = start;
        int hi = start + count - 1;
        while (lo <= hi)
        {
            int i = lo + ((hi - lo) >> 1);

            switch (source[i].Token.CompareTo(token))
            {
                case 0:
                    return i;
                case < 0:
                    lo = i + 1;
                    break;
                default:
                    hi = i - 1;
                    break;
            }
        }

        return ~lo;
    }

    private static uint ReadUnityCompressedUInt32(ref MemoryReader reader)
    {
        byte byt = reader.ReadByte();

        switch (byt)
        {
            case < 128:
                return byt;
            case 240:
                return reader.ReadUInt32LittleEndian();
            case 254:
                return uint.MaxValue - 1;
            case byte.MaxValue:
                return uint.MaxValue;
        }

        if ((byt & 192) == 192)
        {
            return (byt & ~192U) << 24
                 | ((uint)reader.ReadByte() << 16)
                 | ((uint)reader.ReadByte() << 8)
                 | reader.ReadByte();
        }

        if ((byt & 128) == 128)
        {
            return (byt & ~128U) << 8
                 | reader.ReadByte();
        }

        return ThrowHelper.ThrowInvalidDataException<uint>();
    }

    private static int ReadUnityCompressedInt32(ref MemoryReader reader)
    {
        uint unsigned = ReadUnityCompressedUInt32(ref reader);
        if (unsigned == uint.MaxValue)
            return int.MinValue;

        bool isNegative = (unsigned & 1) == 1;
        unsigned >>= 1;

        return isNegative
            ? -(int)(unsigned + 1)
            : (int)unsigned;
    }

    private static object? ReadValue(ref MemoryReader reader)
    {
        Il2CppTypeEnum type = (Il2CppTypeEnum)reader.ReadByte();
        return ReadValue(ref reader, type);
    }

    private static object? ReadValue(ref MemoryReader reader, Il2CppTypeEnum type)
    {
        switch (type)
        {
            case Il2CppTypeEnum.IL2CPP_TYPE_ENUM:
                Il2CppTypeEnum underlyingType = ReadEnumUnderlyingType(ref reader);
                return ReadValue(ref reader, underlyingType);
            case Il2CppTypeEnum.IL2CPP_TYPE_SZARRAY:
                return ReadSzArray(ref reader);
            case Il2CppTypeEnum.IL2CPP_TYPE_IL2CPP_TYPE_INDEX:
                int typeIndex = ReadUnityCompressedInt32(ref reader);
                return LibCpp2IlMain.Binary!.GetType(typeIndex);
            case Il2CppTypeEnum.IL2CPP_TYPE_BOOLEAN:
                return reader.ReadBoolean();
            case Il2CppTypeEnum.IL2CPP_TYPE_CHAR:
                return (char)reader.ReadInt16LittleEndian();
            case Il2CppTypeEnum.IL2CPP_TYPE_I1:
                return reader.ReadSByte();
            case Il2CppTypeEnum.IL2CPP_TYPE_U1:
                return reader.ReadByte();
            case Il2CppTypeEnum.IL2CPP_TYPE_I2:
                return reader.ReadInt16LittleEndian();
            case Il2CppTypeEnum.IL2CPP_TYPE_U2:
                return reader.ReadUInt16LittleEndian();
            case Il2CppTypeEnum.IL2CPP_TYPE_I4:
                return ReadUnityCompressedInt32(ref reader);
            case Il2CppTypeEnum.IL2CPP_TYPE_U4:
                return ReadUnityCompressedUInt32(ref reader);
            case Il2CppTypeEnum.IL2CPP_TYPE_I8:
                return reader.ReadInt64LittleEndian();
            case Il2CppTypeEnum.IL2CPP_TYPE_U8:
                return reader.ReadUInt64LittleEndian();
            case Il2CppTypeEnum.IL2CPP_TYPE_R4:
                return reader.ReadSingleLittleEndian();
            case Il2CppTypeEnum.IL2CPP_TYPE_R8:
                return reader.ReadDoubleLittleEndian();
            case Il2CppTypeEnum.IL2CPP_TYPE_STRING:
                return ReadString(ref reader);
            case Il2CppTypeEnum.IL2CPP_TYPE_CLASS:
            case Il2CppTypeEnum.IL2CPP_TYPE_OBJECT:
            case Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST:
            default:
                return ThrowHelper.ThrowNotSupportedException<object>();
        }
    }

    private static Il2CppTypeEnum ReadEnumUnderlyingType(ref MemoryReader reader)
    {
        int typeIdx        = ReadUnityCompressedInt32(ref reader);
        var enumType       = LibCpp2IlMain.Binary!.GetType(typeIdx);
        var underlyingType = LibCpp2IlMain.Binary.GetType(
            enumType.AsClass().ElementTypeIndex);

        return underlyingType.Type;
    }

    private static object?[]? ReadSzArray(ref MemoryReader reader)
    {
        int arrayLength = ReadUnityCompressedInt32(ref reader);
        if (arrayLength == -1)
            return null;

        Il2CppTypeEnum arrayType = (Il2CppTypeEnum)reader.ReadByte();
        if (arrayType == Il2CppTypeEnum.IL2CPP_TYPE_ENUM)
            arrayType = ReadEnumUnderlyingType(ref reader);

        bool typePrefixed = reader.ReadBoolean();
        if (typePrefixed && arrayType != Il2CppTypeEnum.IL2CPP_TYPE_OBJECT)
            ThrowHelper.ThrowInvalidDataException("Array elements are type-prefixed, but the array type is not object");

        object?[] array = new object?[arrayLength];
        for (int i = 0; i < arrayLength; i++)
        {
            Il2CppTypeEnum elementType = typePrefixed
                ? (Il2CppTypeEnum)reader.ReadByte()
                : arrayType;

            array[i] = ReadValue(ref reader, elementType);
        }

        return array;
    }

    private static string? ReadString(ref MemoryReader reader)
    {
        int length = ReadUnityCompressedInt32(ref reader);
        if (length == -1)
            return null;

        ReadOnlySpan<byte> bytes = reader.ReadBytes(length);
        return Encoding.UTF8.GetString(bytes);
    }

    private static void ResolveMember(ref MemoryReader reader)
    {
        // We don't care about attribute properties or fields,
        // so we just read enough to exhaust the stream

        int memberIndex = ReadUnityCompressedInt32(ref reader);
        if (memberIndex < 0)
        {
            uint typeIndex = ReadUnityCompressedUInt32(ref reader);
            memberIndex = -(memberIndex + 1);
        }
    }
}