protodec
========
A tool to decompile [protoc](https://github.com/protocolbuffers/protobuf) compiled protobuf classes back into .proto definitions.

Usage
-----
```
Usage: [command] [arguments...] [options...] [-h|--help] [--version]                                                                                                                                                                                                                    
Use reflection backend to load target CIL assembly and its dependants.

Arguments:
  [0] <string>    Either the path to the target assembly or a directory of assemblies, all of which be parsed.
  [1] <string>    An existing directory to output into individual files, otherwise output to a single file.

Options:
  --skip-enums                                                  Skip parsing enums and replace references to them with int32. (Optional)
  --include-properties-without-non-user-code-attribute          Includes properties that aren't decorated with `DebuggerNonUserCode` when parsing. (Optional)
  --include-service-methods-without-generated-code-attribute    Includes methods that aren't decorated with `GeneratedCode("grpc_csharp_plugin")` when parsing gRPC services. (Optional)
  --parse-service-servers                                       Parses gRPC service definitions from server classes. (Optional)
  --parse-service-clients                                       Parses gRPC service definitions from client classes. (Optional)
  --log-level <LogLevel>                                        Logging severity level. (Default: Information)

Commands:
  il2cpp    Use LibCpp2IL backend to directly load Il2Cpp compiled game assembly. EXPERIMENTAL.
```
See per-command help message for more info.

Limitations
-----------
- Integers are assumed to be (u)int32/64 as CIL doesn't differentiate between them and sint32/64 and (s)fixed32/64.
- Package names are not preserved in protobuf compilation so naturally we cannot recover them during decompilation, which may result in naming conflicts.
- When decompiling from [Il2CppDumper](https://github.com/Perfare/Il2CppDumper) DummyDLLs or from an Il2Cpp assembly older than metadata version 29, due to the development branch of [LibCpp2Il](https://github.com/SamboyCoding/Cpp2IL/tree/development/LibCpp2IL) not yet recovering method bodies
    - The `Name` parameter of `OriginalNameAttribute` is not parsed. In this case, the CIL enum field names are used after conforming them to protobuf conventions.
    - The `Tool` parameter of `GeneratedCodeAttribute` is not compared against when parsing gRPC service methods, which may cause false positives in the event that another tool has generated methods in the service class.

Development
-----------
### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later

### Build
```
dotnet build protodec.sln
```

### Publish a standalone executable
The project uses Native AOT compilation. You must publish via the **solution** (not the `.csproj` directly) because `$(SolutionDir)` is used in project references.

| Target | Command |
|---|---|
| Windows x64 | `dotnet publish protodec.sln -c Release -r win-x64` |
| Linux x64 | `dotnet publish protodec.sln -c Release -r linux-x64` |

Output will be located in `bin/protodec/Release/net8.0/<rid>/publish/`.

License
-------
This project is subject to the terms of the [Mozilla Public License, v. 2.0](./LICENSE).