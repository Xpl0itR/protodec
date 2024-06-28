// Copyright © 2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.CodeDom.Compiler;
using LibProtodec.Models.Protobuf.TopLevels;
using LibProtodec.Models.Protobuf.Types;

namespace LibProtodec.Models.Protobuf.Fields;

public sealed class ServiceMethod
{
    public required string        Name         { get; init; }
    public required IProtobufType RequestType  { get; init; }
    public required IProtobufType ResponseType { get; init; }

    public bool IsRequestStreamed  { get; init; }
    public bool IsResponseStreamed { get; init; }
    public bool IsObsolete         { get; init; }

    public void WriteTo(IndentedTextWriter writer, TopLevel topLevel)
    {
        writer.Write("rpc ");
        writer.Write(Name);
        writer.Write(" (");

        if (IsRequestStreamed)
        {
            writer.Write("stream ");
        }

        Protobuf.WriteTypeNameTo(writer, RequestType, topLevel);
        writer.Write(") returns (");

        if (IsResponseStreamed)
        {
            writer.Write("stream ");
        }

        Protobuf.WriteTypeNameTo(writer, ResponseType, topLevel);
        writer.Write(')');

        if (IsObsolete)
        {
            writer.WriteLine(" {");
            writer.Indent++;

            Protobuf.WriteOptionTo(writer, "deprecated", "true");

            writer.Indent--;
            writer.WriteLine('}');
        }
        else
        {
            writer.WriteLine(';');
        }
    }
}