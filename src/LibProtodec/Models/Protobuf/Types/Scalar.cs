// Copyright © 2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace LibProtodec.Models.Protobuf.Types;

// ReSharper disable StringLiteralTypo
public static class Scalar
{
    public static readonly IProtobufType Bool     = new External("bool");
    public static readonly IProtobufType Bytes    = new External("bytes");
    public static readonly IProtobufType Double   = new External("double");
    public static readonly IProtobufType Fixed32  = new External("fixed32");
    public static readonly IProtobufType Fixed64  = new External("fixed64");
    public static readonly IProtobufType Float    = new External("float");
    public static readonly IProtobufType Int32    = new External("int32");
    public static readonly IProtobufType Int64    = new External("int64");
    public static readonly IProtobufType SFixed32 = new External("sfixed32");
    public static readonly IProtobufType SFixed64 = new External("sfixed64");
    public static readonly IProtobufType SInt32   = new External("sint32");
    public static readonly IProtobufType SInt64   = new External("sint64");
    public static readonly IProtobufType String   = new External("string");
    public static readonly IProtobufType UInt32   = new External("uint32");
    public static readonly IProtobufType UInt64   = new External("uint64");
}