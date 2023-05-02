protodec
========
A tool to decompile protobuf parser/serializer classes compiled by [protoc](https://github.com/protocolbuffers/protobuf), from dotnet assemblies back into .proto definitions.

Usage
-----
```
Usage: protodec(.exe) <target_assembly_path> <out_path> [options]
Arguments:
  target_assembly_path  Either a single assembly or a directory of assemblies to be parsed.
  out_path              An existing directory to output into individual files, otherwise output to a single file.
Options:
  --skip_enums                  Skip parsing enums and replace references to then with int32.
  --include_runtime_assemblies  Add the assemblies of the current runtime to the search path.
```

Limitations
-----------
- Integers are assumed to be (u)int32/64 as C# doesn't differentiate between them and sint32/64 and (s)fixed32/64.
  This could be solved by parsing the writer methods, however this wouldn't work on hollow assemblies such as DummyDlls produced by Il2CppDumper
### Il2CppDumper
- The Name parameter of OriginalNameAttribute is not dumped. In this case the C# names are used after conforming them to protobuf conventions
- Dumped assemblies depend on strong-named core libs, however the ones dumped are not strong-named.
  This interferes with loading and can be bypassed by loading the strong-named libs from your runtime by passing the `--include_runtime_assemblies` flag

License
-------
This project is subject to the terms of the [Mozilla Public License, v. 2.0](./LICENSE).