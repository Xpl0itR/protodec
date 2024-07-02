protodec
========
A tool to decompile protobuf classes compiled by [protoc](https://github.com/protocolbuffers/protobuf), from il2cpp compiled CIL assemblies back into .proto definitions.

This branch was created as a proof-of-concept using [the development branch of LibCpp2Il](https://github.com/SamboyCoding/Cpp2IL/tree/development/LibCpp2IL) to parse the game assembly and metadata directly, without the intermediate step of generating dummy DLLs.

I offer no guarantees that this branch functions 1:1 with master, it may explode.

Usage
-----
```
Usage: protodec(.exe) <game_assembly_path> <global_metadata_path> <unity_version> <out_path> [options]
Arguments:
  game_assembly_path    The path to the game assembly DLL.
  global_metadata_path  The path to the global-metadata.dat file.
  unity_version         The version of Unity which was used to create the metadata file or alternatively, the path to the globalgamemanagers or the data.unity3d file.
  out_path              An existing directory to output into individual files, otherwise output to a single file.
Options:
  --parse_service_servers                                     Parses gRPC service definitions from server classes.
  --parse_service_clients                                     Parses gRPC service definitions from client classes.
  --skip_enums                                                Skip parsing enums and replace references to them with int32.
  --include_properties_without_non_user_code_attribute        Includes properties that aren't decorated with `DebuggerNonUserCode` when parsing.
  --include_service_methods_without_generated_code_attribute  Includes methods that aren't decorated with `GeneratedCode("grpc_csharp_plugin")` when parsing gRPC services.
```

Limitations
-----------
- Integers are assumed to be (u)int32/64 as CIL doesn't differentiate between them and sint32/64 and (s)fixed32/64.
- Package names are not preserved in protobuf compilation so naturally we cannot recover them during decompilation, which may result in naming conflicts.
- Due to the development branch of Cpp2Il not yet recovering method bodies, when parsing an Il2Cpp assembly older than metadata version 29
    - The `Name` parameter of `OriginalNameAttribute` is not parsed. In this case, the CIL enum field names are used after conforming them to protobuf conventions.
    - The `Tool` parameter of `GeneratedCodeAttribute` is not compared against when parsing gRPC service methods, which may cause false positives in the event that another tool has generated methods in the service class.

License
-------
This project is subject to the terms of the [Mozilla Public License, v. 2.0](./LICENSE).