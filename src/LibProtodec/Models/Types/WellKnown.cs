// Copyright © 2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace LibProtodec.Models.Types;

public static class WellKnown
{
    public static readonly IType Any           = new External("google.protobuf.Any");
    public static readonly IType Api           = new External("google.protobuf.Api");
    public static readonly IType BoolValue     = new External("google.protobuf.BoolValue");
    public static readonly IType BytesValue    = new External("google.protobuf.BytesValue");
    public static readonly IType DoubleValue   = new External("google.protobuf.DoubleValue");
    public static readonly IType Duration      = new External("google.protobuf.Duration");
    public static readonly IType Empty         = new External("google.protobuf.Empty");
    public static readonly IType Enum          = new External("google.protobuf.Enum");
    public static readonly IType EnumValue     = new External("google.protobuf.EnumValue");
    public static readonly IType Field         = new External("google.protobuf.Field");
    public static readonly IType FieldMask     = new External("google.protobuf.FieldMask");
    public static readonly IType FloatValue    = new External("google.protobuf.FloatValue");
    public static readonly IType Int32Value    = new External("google.protobuf.Int32Value");
    public static readonly IType Int64Value    = new External("google.protobuf.Int64Value");
    public static readonly IType ListValue     = new External("google.protobuf.ListValue");
    public static readonly IType Method        = new External("google.protobuf.Method");
    public static readonly IType Mixin         = new External("google.protobuf.Mixin");
    public static readonly IType NullValue     = new External("google.protobuf.NullValue");
    public static readonly IType Option        = new External("google.protobuf.Option");
    public static readonly IType SourceContext = new External("google.protobuf.SourceContext");
    public static readonly IType StringValue   = new External("google.protobuf.StringValue");
    public static readonly IType Struct        = new External("google.protobuf.Struct");
    public static readonly IType Syntax        = new External("google.protobuf.Syntax");
    public static readonly IType Timestamp     = new External("google.protobuf.Timestamp");
    public static readonly IType Type          = new External("google.protobuf.Type");
    public static readonly IType UInt32Value   = new External("google.protobuf.UInt32Value");
    public static readonly IType UInt64Value   = new External("google.protobuf.UInt64Value");
    public static readonly IType Value         = new External("google.protobuf.Value");
}