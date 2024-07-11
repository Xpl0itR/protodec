// Copyright © 2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.CodeDom.Compiler;
using System.Collections.Generic;
using LibProtodec.Models.Protobuf.Fields;

namespace LibProtodec.Models.Protobuf.TopLevels;

public sealed class Service : TopLevel
{
    public readonly List<ServiceMethod> Methods = [];

    public override void WriteTo(IndentedTextWriter writer)
    {
        writer.Write("service ");
        writer.Write(this.Name);
        writer.WriteLine(" {");
        writer.Indent++;

        if (this.IsObsolete)
        {
            Protobuf.WriteOptionTo(writer, "deprecated", "true");
        }

        foreach (ServiceMethod method in Methods)
        {
            method.WriteTo(writer);
        }

        writer.Indent--;
        writer.Write('}');
    }
}