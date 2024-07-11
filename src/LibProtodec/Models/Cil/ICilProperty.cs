// Copyright © 2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;

namespace LibProtodec.Models.Cil;

public interface ICilProperty
{
    string   Name { get; }
    ICilType Type { get; }

    bool IsInherited { get; }
    bool CanRead     { get; }
    bool CanWrite    { get; }

    ICilMethod? Getter { get; }
    ICilMethod? Setter { get; }

    IList<ICilAttribute> CustomAttributes { get; }
}