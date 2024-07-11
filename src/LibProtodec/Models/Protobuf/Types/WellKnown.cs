// Copyright © 2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace LibProtodec.Models.Protobuf.Types;

public sealed class WellKnown(string typeName, string fileName) : IProtobufType
{
    public string Name =>
        typeName;

    public string FileName =>
        fileName;

    public static readonly IProtobufType Any           = new WellKnown("google.protobuf.Any",           "google/protobuf/any.proto");
    public static readonly IProtobufType Api           = new WellKnown("google.protobuf.Api",           "google/protobuf/api.proto");
    public static readonly IProtobufType BoolValue     = new WellKnown("google.protobuf.BoolValue",     "google/protobuf/wrappers.proto");
    public static readonly IProtobufType BytesValue    = new WellKnown("google.protobuf.BytesValue",    "google/protobuf/wrappers.proto");
    public static readonly IProtobufType DoubleValue   = new WellKnown("google.protobuf.DoubleValue",   "google/protobuf/wrappers.proto");
    public static readonly IProtobufType Duration      = new WellKnown("google.protobuf.Duration",      "google/protobuf/duration.proto");
    public static readonly IProtobufType Empty         = new WellKnown("google.protobuf.Empty",         "google/protobuf/empty.proto");
    public static readonly IProtobufType Enum          = new WellKnown("google.protobuf.Enum",          "google/protobuf/type.proto");
    public static readonly IProtobufType EnumValue     = new WellKnown("google.protobuf.EnumValue",     "google/protobuf/type.proto");
    public static readonly IProtobufType Field         = new WellKnown("google.protobuf.Field",         "google/protobuf/type.proto");
    public static readonly IProtobufType FieldMask     = new WellKnown("google.protobuf.FieldMask",     "google/protobuf/field_mask.proto");
    public static readonly IProtobufType FloatValue    = new WellKnown("google.protobuf.FloatValue",    "google/protobuf/wrappers.proto");
    public static readonly IProtobufType Int32Value    = new WellKnown("google.protobuf.Int32Value",    "google/protobuf/wrappers.proto");
    public static readonly IProtobufType Int64Value    = new WellKnown("google.protobuf.Int64Value",    "google/protobuf/wrappers.proto");
    public static readonly IProtobufType ListValue     = new WellKnown("google.protobuf.ListValue",     "google/protobuf/struct.proto");
    public static readonly IProtobufType Method        = new WellKnown("google.protobuf.Method",        "google/protobuf/api.proto");
    public static readonly IProtobufType Mixin         = new WellKnown("google.protobuf.Mixin",         "google/protobuf/api.proto");
    public static readonly IProtobufType NullValue     = new WellKnown("google.protobuf.NullValue",     "google/protobuf/struct.proto");
    public static readonly IProtobufType Option        = new WellKnown("google.protobuf.Option",        "google/protobuf/type.proto");
    public static readonly IProtobufType SourceContext = new WellKnown("google.protobuf.SourceContext", "google/protobuf/source_context.proto");
    public static readonly IProtobufType StringValue   = new WellKnown("google.protobuf.StringValue",   "google/protobuf/wrappers.proto");
    public static readonly IProtobufType Struct        = new WellKnown("google.protobuf.Struct",        "google/protobuf/struct.proto");
    public static readonly IProtobufType Syntax        = new WellKnown("google.protobuf.Syntax",        "google/protobuf/type.proto");
    public static readonly IProtobufType Timestamp     = new WellKnown("google.protobuf.Timestamp",     "google/protobuf/timestamp.proto");
    public static readonly IProtobufType Type          = new WellKnown("google.protobuf.Type",          "google/protobuf/type.proto");
    public static readonly IProtobufType UInt32Value   = new WellKnown("google.protobuf.UInt32Value",   "google/protobuf/wrappers.proto");
    public static readonly IProtobufType UInt64Value   = new WellKnown("google.protobuf.UInt64Value",   "google/protobuf/wrappers.proto");
    public static readonly IProtobufType Value         = new WellKnown("google.protobuf.Value",         "google/protobuf/struct.proto");
}