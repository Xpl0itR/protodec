// Copyright © 2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;

namespace LibProtodec.Models.Cil;

public interface ICilType
{
    string  Name      { get; }
    string  FullName  { get; }
    string? Namespace { get; }

    string     DeclaringAssemblyName { get; }
    ICilModule DeclaringModule       { get; }
    ICilType?  DeclaringType         { get; }
    ICilType?  BaseType              { get; }

    bool IsAbstract { get; }
    bool IsClass    { get; }
    bool IsEnum     { get; }
    bool IsNested   { get; }
    bool IsSealed   { get; }

    IList<ICilType> GenericTypeArguments { get; }

    IList<ICilAttribute> CustomAttributes { get; }

    IEnumerable<ICilField> GetFields();

    IEnumerable<ICilMethod> GetMethods();

    IEnumerable<ICilType> GetNestedTypes();

    IEnumerable<ICilProperty> GetProperties();

    bool IsAssignableTo(ICilType type);
}