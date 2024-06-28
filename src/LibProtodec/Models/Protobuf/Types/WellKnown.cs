// Copyright © 2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace LibProtodec.Models.Protobuf.Types;

public static class WellKnown
{
    public static readonly IProtobufType Any           = new External("google.protobuf.Any");
    public static readonly IProtobufType Api           = new External("google.protobuf.Api");
    public static readonly IProtobufType BoolValue     = new External("google.protobuf.BoolValue");
    public static readonly IProtobufType BytesValue    = new External("google.protobuf.BytesValue");
    public static readonly IProtobufType DoubleValue   = new External("google.protobuf.DoubleValue");
    public static readonly IProtobufType Duration      = new External("google.protobuf.Duration");
    public static readonly IProtobufType Empty         = new External("google.protobuf.Empty");
    public static readonly IProtobufType Enum          = new External("google.protobuf.Enum");
    public static readonly IProtobufType EnumValue     = new External("google.protobuf.EnumValue");
    public static readonly IProtobufType Field         = new External("google.protobuf.Field");
    public static readonly IProtobufType FieldMask     = new External("google.protobuf.FieldMask");
    public static readonly IProtobufType FloatValue    = new External("google.protobuf.FloatValue");
    public static readonly IProtobufType Int32Value    = new External("google.protobuf.Int32Value");
    public static readonly IProtobufType Int64Value    = new External("google.protobuf.Int64Value");
    public static readonly IProtobufType ListValue     = new External("google.protobuf.ListValue");
    public static readonly IProtobufType Method        = new External("google.protobuf.Method");
    public static readonly IProtobufType Mixin         = new External("google.protobuf.Mixin");
    public static readonly IProtobufType NullValue     = new External("google.protobuf.NullValue");
    public static readonly IProtobufType Option        = new External("google.protobuf.Option");
    public static readonly IProtobufType SourceContext = new External("google.protobuf.SourceContext");
    public static readonly IProtobufType StringValue   = new External("google.protobuf.StringValue");
    public static readonly IProtobufType Struct        = new External("google.protobuf.Struct");
    public static readonly IProtobufType Syntax        = new External("google.protobuf.Syntax");
    public static readonly IProtobufType Timestamp     = new External("google.protobuf.Timestamp");
    public static readonly IProtobufType Type          = new External("google.protobuf.Type");
    public static readonly IProtobufType UInt32Value   = new External("google.protobuf.UInt32Value");
    public static readonly IProtobufType UInt64Value   = new External("google.protobuf.UInt64Value");
    public static readonly IProtobufType Value         = new External("google.protobuf.Value");
}