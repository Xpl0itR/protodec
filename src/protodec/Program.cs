// Copyright © 2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using AssetRipper.Primitives;
using ConsoleAppFramework;
using LibCpp2IL;
using LibProtodec;
using LibProtodec.Loaders;
using LibProtodec.Models.Cil;
using LibProtodec.Models.Protobuf;
using Microsoft.Extensions.Logging;

ConsoleApp.ConsoleAppBuilder app = ConsoleApp.Create();
app.Add<Commands>();
app.Run(args);

internal sealed class Commands
{
    /// <summary>
    ///     Use reflection backend to load target CIL assembly and its dependants.
    /// </summary>
    /// <param name="targetPath">Either the path to the target assembly or a directory of assemblies, all of which be parsed.</param>
    /// <param name="outPath">An existing directory to output into individual files, otherwise output to a single file.</param>
    /// <param name="logLevel">Logging severity level.</param>
    /// <param name="parseServiceServers">Parses gRPC service definitions from server classes.</param>
    /// <param name="parseServiceClients">Parses gRPC service definitions from client classes.</param>
    /// <param name="skipEnums">Skip parsing enums and replace references to them with int32.</param>
    /// <param name="includePropertiesWithoutNonUserCodeAttribute">Includes properties that aren't decorated with `DebuggerNonUserCode` when parsing.</param>
    /// <param name="includeServiceMethodsWithoutGeneratedCodeAttribute">Includes methods that aren't decorated with `GeneratedCode("grpc_csharp_plugin")` when parsing gRPC services.</param>
    [Command("")]
    public void Root(
        [Argument] string targetPath,
        [Argument] string outPath,
        bool              skipEnums,
        bool              includePropertiesWithoutNonUserCodeAttribute,
        bool              includeServiceMethodsWithoutGeneratedCodeAttribute,
        bool              parseServiceServers,
        bool              parseServiceClients,
        LogLevel          logLevel = LogLevel.Information)
    {
        using ILoggerFactory loggerFactory = CreateLoggerFactory(logLevel);
        ILogger logger = CreateProtodecLogger(loggerFactory);

        logger.LogInformation("Loading target assemblies...");
        using ClrAssemblyLoader loader = new(targetPath, loggerFactory.CreateLogger<ClrAssemblyLoader>());

        Handle(
            loader,
            outPath,
            skipEnums,
            includePropertiesWithoutNonUserCodeAttribute,
            includeServiceMethodsWithoutGeneratedCodeAttribute,
            parseServiceServers,
            parseServiceClients,
            loggerFactory,
            logger);
    }

    /// <summary>
    ///     Use LibCpp2IL backend to directly load Il2Cpp compiled game assembly. EXPERIMENTAL.
    /// </summary>
    /// <param name="gameAssembly">The path to the game assembly DLL.</param>
    /// <param name="globalMetadata">The path to the global-metadata.dat file.</param>
    /// <param name="unityVersion">The version of Unity which was used to create the metadata file or alternatively, the path to the globalgamemanagers or the data.unity3d file.</param>
    /// <param name="outPath">An existing directory to output into individual files, otherwise output to a single file.</param>
    /// <param name="logLevel">Logging severity level.</param>
    /// <param name="parseServiceServers">Parses gRPC service definitions from server classes.</param>
    /// <param name="parseServiceClients">Parses gRPC service definitions from client classes.</param>
    /// <param name="skipEnums">Skip parsing enums and replace references to them with int32.</param>
    /// <param name="includePropertiesWithoutNonUserCodeAttribute">Includes properties that aren't decorated with `DebuggerNonUserCode` when parsing.</param>
    /// <param name="includeServiceMethodsWithoutGeneratedCodeAttribute">Includes methods that aren't decorated with `GeneratedCode("grpc_csharp_plugin")` when parsing gRPC services.</param>
    [Command("il2cpp")]
    public void Il2Cpp(
        [Argument] string gameAssembly,
        [Argument] string globalMetadata,
        [Argument] string unityVersion,
        [Argument] string outPath,
        bool              skipEnums,
        bool              includePropertiesWithoutNonUserCodeAttribute,
        bool              includeServiceMethodsWithoutGeneratedCodeAttribute,
        bool              parseServiceServers,
        bool              parseServiceClients,
        LogLevel          logLevel = LogLevel.Information)
    {
        if (!UnityVersion.TryParse(unityVersion, out UnityVersion unityVer, out _))
        {
            unityVer = unityVersion.EndsWith("globalgamemanagers")
                ? LibCpp2IlMain.GetVersionFromGlobalGameManagers(
                    File.ReadAllBytes(unityVersion))
                : LibCpp2IlMain.GetVersionFromDataUnity3D(
                    File.OpenRead(unityVersion));
        }

        using ILoggerFactory loggerFactory = CreateLoggerFactory(logLevel);
        ILogger logger = CreateProtodecLogger(loggerFactory);

        logger.LogInformation("Loading target assemblies...");
        Il2CppAssemblyLoader loader = new(gameAssembly, globalMetadata, unityVer, loggerFactory);

        Handle(
            loader,
            outPath,
            skipEnums,
            includePropertiesWithoutNonUserCodeAttribute,
            includeServiceMethodsWithoutGeneratedCodeAttribute,
            parseServiceServers,
            parseServiceClients,
            loggerFactory,
            logger);
    }

    private static void Handle(
        CilAssemblyLoader loader,
        string            outPath,
        bool              skipEnums,
        bool              includePropertiesWithoutNonUserCodeAttribute,
        bool              includeServiceMethodsWithoutGeneratedCodeAttribute,
        bool              parseServiceServers,
        bool              parseServiceClients,
        ILoggerFactory    loggerFactory,
        ILogger           logger)
    {
        ParserOptions options = ParserOptions.None;
        if (skipEnums)
            options |= ParserOptions.SkipEnums;
        if (includePropertiesWithoutNonUserCodeAttribute)
            options |= ParserOptions.IncludePropertiesWithoutNonUserCodeAttribute;
        if (includeServiceMethodsWithoutGeneratedCodeAttribute)
            options |= ParserOptions.IncludeServiceMethodsWithoutGeneratedCodeAttribute;

        ProtodecContext ctx = new()
        {
            Logger = loggerFactory.CreateLogger<ProtodecContext>()
        };

        logger.LogInformation("Parsing Protobuf message types...");
        foreach (ICilType message in loader.GetProtobufMessageTypes())
        {
            ctx.ParseMessage(message, options);
        }

        if (parseServiceServers)
        {
            logger.LogInformation("Parsing Protobuf service server types...");
            foreach (ICilType service in loader.GetProtobufServiceServerTypes())
            {
                ctx.ParseService(service, options);
            }
        }

        if (parseServiceClients)
        {
            logger.LogInformation("Parsing Protobuf service client types...");
            foreach (ICilType service in loader.GetProtobufServiceClientTypes())
            {
                ctx.ParseService(service, options);
            }
        }

        const string indent = "  ";
        if (Directory.Exists(outPath))
        {
            logger.LogInformation("Writing {count} Protobuf files to \"{path}\"...", ctx.Protobufs.Count, outPath);

            HashSet<string> writtenFiles = [];
            foreach (Protobuf protobuf in ctx.Protobufs)
            {
                // This workaround stops files from being overwritten in the case of a naming conflict,
                // however the actual conflict will still have to be resolved manually
                string fileName = protobuf.FileName;
                while (!writtenFiles.Add(fileName))
                {
                    fileName = '_' + fileName;
                }

                string protobufPath = Path.Join(outPath, fileName);

                using StreamWriter       streamWriter = new(protobufPath);
                using IndentedTextWriter indentWriter = new(streamWriter, indent);

                protobuf.WriteTo(indentWriter);
            }
        }
        else
        {
            logger.LogInformation("Writing Protobufs as a single file to \"{path}\"...", outPath);

            using StreamWriter       streamWriter = new(outPath);
            using IndentedTextWriter indentWriter = new(streamWriter, indent);

            ctx.WriteAllTo(indentWriter);
        }
    }

    private static ILoggerFactory CreateLoggerFactory(LogLevel logLevel) =>
        LoggerFactory.Create(
            builder => builder.AddSimpleConsole(static console => console.IncludeScopes = true)
                              .SetMinimumLevel(logLevel));

    private static ILogger CreateProtodecLogger(ILoggerFactory loggerFactory)
    {
        ILogger logger = loggerFactory.CreateLogger("protodec");
        ConsoleApp.LogError = msg => logger.LogError(msg);

        return logger;
    }
}