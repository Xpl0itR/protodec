// Copyright © 2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace LibProtodec.Models.Types;

// ReSharper disable StringLiteralTypo
public static class Scalar
{
    public static readonly IType Bool     = new External("bool");
    public static readonly IType Bytes    = new External("bytes");
    public static readonly IType Double   = new External("double");
    public static readonly IType Fixed32  = new External("fixed32");
    public static readonly IType Fixed64  = new External("fixed64");
    public static readonly IType Float    = new External("float");
    public static readonly IType Int32    = new External("int32");
    public static readonly IType Int64    = new External("int64");
    public static readonly IType SFixed32 = new External("sfixed32");
    public static readonly IType SFixed64 = new External("sfixed64");
    public static readonly IType SInt32   = new External("sint32");
    public static readonly IType SInt64   = new External("sint64");
    public static readonly IType String   = new External("string");
    public static readonly IType UInt32   = new External("uint32");
    public static readonly IType UInt64   = new External("uint64");
}