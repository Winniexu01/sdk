﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;
using System.Text.Json;
using Microsoft.NET.Sdk.Localization;
using static Microsoft.NET.Sdk.WorkloadManifestReader.WorkloadManifestReader;

namespace Microsoft.NET.Sdk.WorkloadManifestReader
{
    public partial class SdkDirectoryWorkloadManifestProvider
    {
        static class JsonReader
        {

            /// <summary>
            /// this expects the reader to be before the value token, and leaves it on the last token of the value
            /// </summary>
            internal static bool ConsumeValue(ref Utf8JsonStreamReader reader)
            {
                if (!reader.Read())
                {
                    return false;
                }

                var tokenType = reader.TokenType;
                if (tokenType != JsonTokenType.StartArray && tokenType != JsonTokenType.StartObject)
                {
                    return true;
                }

                var depth = reader.CurrentDepth;
                do
                {
                    if (!reader.Read())
                    {
                        return false;
                    }
                } while (reader.CurrentDepth > depth);

                return true;
            }

            internal static void ConsumeToken(ref Utf8JsonStreamReader reader, JsonTokenType expected)
            {
                if (reader.Read() && expected == reader.TokenType)
                {
                    return;
                }
                ThrowUnexpectedTokenException(ref reader, expected);
            }

            private static void ThrowUnexpectedTokenException(ref Utf8JsonStreamReader reader, JsonTokenType expected)
            {
                string key;
                if (expected.IsBool())
                {
                    key = Strings.ExpectedBoolAtOffset;
                }
                else if (expected.IsInt())
                {
                    key = Strings.ExpectedIntegerAtOffset;
                }
                else if (expected == JsonTokenType.String)
                {
                    key = Strings.ExpectedStringAtOffset;
                }
                else
                {
                    throw new JsonFormatException(Strings.ExpectedTokenAtOffset, expected, reader.TokenStartIndex);
                }

                throw new JsonFormatException(key, reader.TokenStartIndex);
            }

            internal static string? ReadString(ref Utf8JsonStreamReader reader)
            {
                ConsumeToken(ref reader, JsonTokenType.String);
                return reader.GetString();
            }
        }

        [Serializable]
        public class JsonFormatException : Exception
        {
            public JsonFormatException() { }
            public JsonFormatException(string messageFormat, params object?[] args) : base(string.Format(messageFormat, args)) { }
            public JsonFormatException(string message) : base(message) { }
            public JsonFormatException(string message, Exception inner) : base(message, inner) { }
#if NET8_0_OR_GREATER
            [Obsolete(DiagnosticId = "SYSLIB0051")] // add this attribute to the serialization ctor
#endif
            protected JsonFormatException(SerializationInfo info, StreamingContext context) : base(info, context) { }
        }
    }
}

