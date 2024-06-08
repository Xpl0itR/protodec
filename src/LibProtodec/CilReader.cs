// Copyright © 2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using SystemEx.Memory;

namespace LibProtodec;

public static class CilReader
{
    // Temporary AOT incompatible method to fill the dictionary, TODO: replace with a source generator
    private static readonly Dictionary<int, OpCode> OpCodeLookup =
        typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
                       .Select(field => (OpCode)field.GetValue(null)!)
                       .ToDictionary(opCode => (int)(ushort)opCode.Value);

    public static OpCode ReadCilOpCode(this ref MemoryReader reader, out int operandLength)
    {
        byte opCodeByte = reader.ReadByte();
        int opCodeInt = opCodeByte == OpCodes.Prefix1.Value
            ? (opCodeByte << 8) | reader.ReadByte()
            : opCodeByte;

        OpCode opCode = OpCodeLookup[opCodeInt];
        operandLength = SizeOf(opCode.OperandType);

        return opCode;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int SizeOf(OperandType operandType)
    {
        switch (operandType)
        {
            case OperandType.ShortInlineBrTarget:
            case OperandType.ShortInlineI:
            case OperandType.ShortInlineVar:
                return 1;
            case OperandType.InlineVar:
                return 2;
            case OperandType.InlineBrTarget:
            case OperandType.InlineField:
            case OperandType.InlineI:
            case OperandType.InlineMethod:
            case OperandType.InlineSig:
            case OperandType.InlineString:
            case OperandType.InlineSwitch:
            case OperandType.InlineTok:
            case OperandType.InlineType:
            case OperandType.ShortInlineR:
                return 4;
            case OperandType.InlineI8:
            case OperandType.InlineR:
                return 8;
            case OperandType.InlineNone:
            default:
                return 0;
        }
    }
}