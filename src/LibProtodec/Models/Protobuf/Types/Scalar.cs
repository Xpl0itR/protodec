// Copyright © 2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace LibProtodec.Models.Protobuf.Types;

// ReSharper disable StringLiteralTypo
public sealed class Scalar(string typeName) : IProtobufType
{
    public string Name =>
        typeName;

    public static readonly IProtobufType Bool     = new Scalar("bool");
    public static readonly IProtobufType Bytes    = new Scalar("bytes");
    public static readonly IProtobufType Double   = new Scalar("double");
    public static readonly IProtobufType Fixed32  = new Scalar("fixed32");
    public static readonly IProtobufType Fixed64  = new Scalar("fixed64");
    public static readonly IProtobufType Float    = new Scalar("float");
    public static readonly IProtobufType Int32    = new Scalar("int32");
    public static readonly IProtobufType Int64    = new Scalar("int64");
    public static readonly IProtobufType SFixed32 = new Scalar("sfixed32");
    public static readonly IProtobufType SFixed64 = new Scalar("sfixed64");
    public static readonly IProtobufType SInt32   = new Scalar("sint32");
    public static readonly IProtobufType SInt64   = new Scalar("sint64");
    public static readonly IProtobufType String   = new Scalar("string");
    public static readonly IProtobufType UInt32   = new Scalar("uint32");
    public static readonly IProtobufType UInt64   = new Scalar("uint64");
}