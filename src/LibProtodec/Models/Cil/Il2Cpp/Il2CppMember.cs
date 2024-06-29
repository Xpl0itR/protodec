// Copyright © 2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using LibCpp2IL;
using LibCpp2IL.Metadata;

namespace LibProtodec.Models.Cil.Il2Cpp;

public abstract class Il2CppMember
{
    public IEnumerable<ICilAttribute> GetCustomAttributes()
    {
        if (LibCpp2IlMain.MetadataVersion >= 29)
        {
            throw new NotImplementedException();
        }

        Il2CppCustomAttributeTypeRange? attributeTypeRange = LibCpp2IlMain.TheMetadata!.GetCustomAttributeData(
            DeclaringAssembly, CustomAttributeIndex, Token, out _);

        if (attributeTypeRange is null || attributeTypeRange.count == 0)
            yield break;

        for (int attrIndex = attributeTypeRange.start,
                 end = attributeTypeRange.start + attributeTypeRange.count;
             attrIndex < end;
             attrIndex++)
        {
            int typeIndex = LibCpp2IlMain.TheMetadata.attributeTypes[attrIndex];

            yield return new Il2CppAttribute(
                LibCpp2ILUtils.GetTypeReflectionData(
                    LibCpp2IlMain.Binary!.GetType(typeIndex)));
        }
    }

    protected abstract Il2CppImageDefinition DeclaringAssembly { get; }

    protected abstract int CustomAttributeIndex { get; }

    protected abstract uint Token { get; }
}