// Copyright © 2024 Xpl0itR
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using Microsoft.Extensions.Logging;

namespace LibProtodec;

// ReSharper disable InconsistentNaming, StringLiteralTypo
internal static partial class LoggerMessages
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to locate corresponding id field; likely stripped or otherwise obfuscated.")]
    internal static partial void LogFailedToLocateIdField(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Loaded {typeCount} types from {assemblyCount} assemblies for parsing.")]
    internal static partial void LogLoadedTypeAndAssemblyCount(this ILogger logger, int typeCount, int assemblyCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Parsed as enum \"{name}\".")]
    internal static partial void LogParsedEnum(this ILogger logger, string name);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Parsed as field \"{name}\" with id \"{id}\".")]
    internal static partial void LogParsedField(this ILogger logger, string name, int id);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Parsed as field \"{name}\" with id \"{id}\" of type \"{typeName}\".")]
    internal static partial void LogParsedField(this ILogger logger, string name, int id, string typeName);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Parsed as message \"{name}\".")]
    internal static partial void LogParsedMessage(this ILogger logger, string name);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Parsed as method \"{name}\" with request type \"{reqType}\" and response type \"{resType}\".")]
    internal static partial void LogParsedMethod(this ILogger logger, string name, string reqType, string resType);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Parsed as oneof field \"{name}\".")]
    internal static partial void LogParsedOneOfField(this ILogger logger, string name);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Parsed as service \"{name}\".")]
    internal static partial void LogParsedService(this ILogger logger, string name);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Skipping duplicate method.")]
    internal static partial void LogSkippingDuplicateMethod(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Skipping property without required NonUserCodeAttribute.")]
    internal static partial void LogSkippingPropertyWithoutNonUserCodeAttribute(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Skipping method without required GeneratedCodeAttribute.")]
    internal static partial void LogSkippingMethodWithoutGeneratedCodeAttribute(this ILogger logger);

    internal static IDisposable? BeginScopeParsingEnum(this ILogger logger, string typeName) =>
        __BeginScopeParsingEnumCallback(logger, typeName);

    internal static IDisposable? BeginScopeParsingField(this ILogger logger, string name) =>
        __BeginScopeParsingFieldCallback(logger, name);

    internal static IDisposable? BeginScopeParsingMessage(this ILogger logger, string name) =>
        __BeginScopeParsingMessageCallback(logger, name);

    internal static IDisposable? BeginScopeParsingMethod(this ILogger logger, string name) =>
        __BeginScopeParsingMethodCallback(logger, name);

    internal static IDisposable? BeginScopeParsingProperty(this ILogger logger, string name, string typeName) =>
        __BeginScopeParsingPropertyCallback(logger, name, typeName);

    internal static IDisposable? BeginScopeParsingService(this ILogger logger, string typeName) =>
        __BeginScopeParsingServiceCallback(logger, typeName);

    private static readonly Func<ILogger, string, IDisposable?> __BeginScopeParsingEnumCallback =
        LoggerMessage.DefineScope<string>("Parsing enum from type \"{typeName}\"");

    private static readonly Func<ILogger, string, IDisposable?> __BeginScopeParsingFieldCallback =
        LoggerMessage.DefineScope<string>("Parsing field \"{name}\"");

    private static readonly Func<ILogger, string, IDisposable?> __BeginScopeParsingMessageCallback =
        LoggerMessage.DefineScope<string>("Parsing message from type \"{name}\"");

    private static readonly Func<ILogger, string, IDisposable?> __BeginScopeParsingMethodCallback =
        LoggerMessage.DefineScope<string>("Parsing method \"{name}\"");

    private static readonly Func<ILogger, string, string, IDisposable?> __BeginScopeParsingPropertyCallback =
        LoggerMessage.DefineScope<string, string>("Parsing property \"{name}\" of type \"{typeName}\"");

    private static readonly Func<ILogger, string, IDisposable?> __BeginScopeParsingServiceCallback =
        LoggerMessage.DefineScope<string>("Parsing service from type \"{typeName}\"");
}