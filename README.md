protodec
========
A tool to decompile protobuf parser/serializer classes compiled by [protoc](https://github.com/protocolbuffers/protobuf), from dotnet assemblies back into .proto definitions.

Usage
-----
```
Usage: protodec(.exe) <target_assembly_dir> <out_path> [target_assembly_name] [options]
Arguments:
  target_assembly_dir   A directory of assemblies to be loaded.
  out_path              An existing directory to output into individual files, otherwise output to a single file.
  target_assembly_name  The name of an assembly to parse. If omitted, all assemblies in the target_assembly_dir will be parsed.
Options:
  --skip_enums                                Skip parsing enums and replace references to them with int32.
  --skip_properties_without_protoc_attribute  Skip properties that aren't decorated with `GeneratedCode("protoc")` when parsing
```

Limitations
-----------
- Integers are assumed to be (u)int32/64 as C# doesn't differentiate between them and sint32/64 and (s)fixed32/64.
### Decompiling from [Il2CppDumper](https://github.com/Perfare/Il2CppDumper) DummyDLLs
- The `Name` parameter of `OriginalNameAttribute` is not dumped. In this case the C# names are used after conforming them to protobuf conventions
- Dumped assemblies depend on strong-named core libs, however the ones dumped are not strong-named.
  This interferes with loading and can be mitigated by copying the assemblies from your runtime into the target assembly directory.

I recommend using [Cpp2IL](https://github.com/SamboyCoding/Cpp2IL) instead of Il2CppDumper.

License
-------
This project is subject to the terms of the [Mozilla Public License, v. 2.0](./LICENSE).