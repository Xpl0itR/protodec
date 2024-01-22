protodec
========
A tool to decompile protobuf classes compiled by [protoc](https://github.com/protocolbuffers/protobuf), from CIL assemblies back into .proto definitions.

Usage
-----
```
Usage: protodec(.exe) <target_assembly_path> <out_path> [options]
Arguments:
  target_assembly_path  Either the path to the target assembly or a directory of assemblies, all of which be parsed.
  out_path              An existing directory to output into individual files, otherwise output to a single file.
Options:
  --skip_enums                                Skip parsing enums and replace references to them with int32.
  --skip_properties_without_protoc_attribute  Skip properties that aren't decorated with `GeneratedCode("protoc")` when parsing
```

Limitations
-----------
- Integers are assumed to be (u)int32/64 as CIL doesn't differentiate between them and sint32/64 and (s)fixed32/64.
- When decompiling from [Il2CppDumper](https://github.com/Perfare/Il2CppDumper) DummyDLLs
    - The `Name` parameter of `OriginalNameAttribute` is not dumped. In this case, the CIL enum field names are used after conforming them to protobuf conventions

License
-------
This project is subject to the terms of the [Mozilla Public License, v. 2.0](./LICENSE).