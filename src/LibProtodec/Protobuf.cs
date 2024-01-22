// Copyright © 2023-2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.CodeDom.Compiler;
using System.IO;

namespace LibProtodec;

public abstract class Protobuf
{
    public required string Name { get; init; }

    public string? AssemblyName { get; init; }
    public string? Namespace    { get; init; }

    public abstract void WriteFileTo(IndentedTextWriter writer);

    public abstract void WriteTo(IndentedTextWriter writer);

    protected void WritePreambleTo(TextWriter writer) =>
        WritePreambleTo(writer, AssemblyName, Namespace);

    // ReSharper disable once MethodOverloadWithOptionalParameter
    public static void WritePreambleTo(TextWriter writer, string? assemblyName = null, string? @namespace = null)
    {
        writer.WriteLine("// Decompiled with protodec");

        if (assemblyName is not null)
        {
            writer.Write("// Assembly: ");
            writer.WriteLine(assemblyName);
        }

        writer.WriteLine();
        writer.WriteLine("""syntax = "proto3";""");
        writer.WriteLine();

        if (@namespace is not null)
        {
            writer.WriteLine($"""option csharp_namespace = "{@namespace}";""");
            writer.WriteLine();
        }
    }
}