// Copyright © 2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.CodeDom.Compiler;
using LibProtodec.Models.Protobuf.TopLevels;
using LibProtodec.Models.Protobuf.Types;

namespace LibProtodec.Models.Protobuf.Fields;

public sealed class ServiceMethod(Service declaringService)
{
    public required string        Name         { get; init; }
    public required IProtobufType RequestType  { get; init; }
    public required IProtobufType ResponseType { get; init; }

    public bool IsRequestStreamed  { get; init; }
    public bool IsResponseStreamed { get; init; }
    public bool IsObsolete         { get; init; }

    public void WriteTo(IndentedTextWriter writer)
    {
        writer.Write("rpc ");
        writer.Write(Name);
        writer.Write(" (");

        if (IsRequestStreamed)
        {
            writer.Write("stream ");
        }

        writer.Write(
            declaringService.QualifyTypeName(RequestType));
        writer.Write(") returns (");

        if (IsResponseStreamed)
        {
            writer.Write("stream ");
        }

        writer.Write(
            declaringService.QualifyTypeName(ResponseType));
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