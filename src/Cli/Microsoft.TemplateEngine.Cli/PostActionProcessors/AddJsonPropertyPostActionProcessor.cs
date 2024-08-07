// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

using Microsoft.DotNet.Cli.Utils;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.PhysicalFileSystem;

namespace Microsoft.TemplateEngine.Cli.PostActionProcessors
{
    internal class AddJsonPropertyPostActionProcessor : PostActionProcessorBase
    {
        private const string AllowFileCreationArgument = "allowFileCreation";
        private const string AllowPathCreationArgument = "allowPathCreation";
        private const string JsonFileNameArgument = "jsonFileName";
        private const string ParentPropertyPathArgument = "parentPropertyPath";
        private const string NewJsonPropertyNameArgument = "newJsonPropertyName";
        private const string NewJsonPropertyValueArgument = "newJsonPropertyValue";

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };

        private static readonly JsonDocumentOptions DeserializerOptions = new()
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        };

        public override Guid Id => ActionProcessorId;

        internal static Guid ActionProcessorId { get; } = new Guid("695A3659-EB40-4FF5-A6A6-C9C4E629FCB0");

        protected override bool ProcessInternal(
            IEngineEnvironmentSettings environment,
            IPostAction action,
            ICreationEffects creationEffects,
            ICreationResult templateCreationResult,
            string outputBasePath)
        {
            if (!action.Args.TryGetValue(JsonFileNameArgument, out string? jsonFileName))
            {
                Reporter.Error.WriteLine(string.Format(LocalizableStrings.PostAction_ModifyJson_Error_ArgumentNotConfigured, JsonFileNameArgument));
                return false;
            }

            IReadOnlyList<string> jsonFiles = FindFilesInCurrentFolderOrParentFolder(environment.Host.FileSystem, outputBasePath, jsonFileName);

            if (jsonFiles.Count == 0)
            {
                if (!bool.TryParse(action.Args.GetValueOrDefault(AllowFileCreationArgument, "false"), out bool createFile))
                {
                    Reporter.Error.WriteLine(string.Format(LocalizableStrings.PostAction_ModifyJson_Error_ArgumentNotBoolean, AllowFileCreationArgument));
                    return false;
                }

                if (!createFile)
                {
                    Reporter.Error.WriteLine(LocalizableStrings.PostAction_ModifyJson_Error_NoJsonFile);
                    return false;
                }

                string newJsonFilePath = Path.Combine(outputBasePath, jsonFileName);
                environment.Host.FileSystem.WriteAllText(newJsonFilePath, "{}");
                jsonFiles = new List<string> { newJsonFilePath };
            }

            if (jsonFiles.Count > 1)
            {
                Reporter.Error.WriteLine(string.Format(LocalizableStrings.PostAction_ModifyJson_Error_MultipleJsonFiles, jsonFileName));
                return false;
            }

            var newJsonElementProperties = JsonContentParameters.CreateFromPostAction(action);

            if (newJsonElementProperties == null)
            {
                return false;
            }

            JsonNode? newJsonContent = AddElementToJson(
                environment.Host.FileSystem,
                jsonFiles[0],
                newJsonElementProperties!.ParentProperty,
                ":",
                newJsonElementProperties.NewJsonPropertyName,
                newJsonElementProperties.NewJsonPropertyValue,
                action);

            if (newJsonContent == null)
            {
                return false;
            }

            environment.Host.FileSystem.WriteAllText(jsonFiles[0], newJsonContent.ToJsonString(SerializerOptions));

            Reporter.Output.WriteLine(string.Format(LocalizableStrings.PostAction_ModifyJson_Succeeded, jsonFileName));

            return true;
        }

        private static JsonNode? AddElementToJson(IPhysicalFileSystem fileSystem, string targetJsonFile, string? propertyPath, string propertyPathSeparator, string newJsonPropertyName, string newJsonPropertyValue, IPostAction action)
        {
            JsonNode? jsonContent = JsonNode.Parse(fileSystem.ReadAllText(targetJsonFile), nodeOptions: null, documentOptions: DeserializerOptions);

            if (jsonContent == null)
            {
                return null;
            }

            if (!bool.TryParse(action.Args.GetValueOrDefault(AllowPathCreationArgument, "false"), out bool createPath))
            {
                Reporter.Error.WriteLine(string.Format(LocalizableStrings.PostAction_ModifyJson_Error_ArgumentNotBoolean, AllowPathCreationArgument));
                return false;
            }

            JsonNode? parentProperty = FindJsonNode(jsonContent, propertyPath, propertyPathSeparator, createPath);

            if (parentProperty == null)
            {
                Reporter.Error.WriteLine(string.Format(LocalizableStrings.PostAction_ModifyJson_Error_ParentPropertyPathInvalid, propertyPath));
                return null;
            }

            try
            {
                parentProperty[newJsonPropertyName] = JsonNode.Parse(newJsonPropertyValue);
            }
            catch (JsonException)
            {
                parentProperty[newJsonPropertyName] = newJsonPropertyValue;
            }

            return jsonContent;
        }

        private static JsonNode? FindJsonNode(JsonNode content, string? nodePath, string pathSeparator, bool createPath)
        {
            if (nodePath == null)
            {
                return content;
            }

            string[] properties = nodePath.Split(pathSeparator);

            JsonNode? node = content;

            foreach (string property in properties)
            {
                if (node == null)
                {
                    return null;
                }

                JsonNode? childNode = node[property];
                if (childNode is null && createPath)
                {
                    node[property] = childNode = new JsonObject();
                }

                node = childNode;
            }

            return node;
        }

        private static string[] FindFilesInCurrentFolderOrParentFolder(
            IPhysicalFileSystem fileSystem,
            string startPath,
            string matchPattern)
        {
            string? directory = fileSystem.DirectoryExists(startPath) ? startPath : Path.GetDirectoryName(startPath);

            if (directory == null)
            {
                throw new InvalidOperationException();
            }

            int numberOfUpLevels = 0;

            do
            {
                Reporter.Verbose.WriteLine(string.Format(LocalizableStrings.PostAction_ModifyJson_Verbose_AttemptingToFindJsonFile, matchPattern, directory));
                string[] filesInDir = fileSystem.EnumerateFileSystemEntries(directory, matchPattern, SearchOption.AllDirectories).ToArray();

                if (filesInDir.Length > 0)
                {
                    return filesInDir;
                }

                directory = Path.GetPathRoot(directory) != directory ? Directory.GetParent(directory)?.FullName : null;
                numberOfUpLevels++;
            }
            while (directory != null && numberOfUpLevels <= 1);

            return Array.Empty<string>();
        }

        private class JsonContentParameters
        {
            private JsonContentParameters(string? parentProperty, string newJsonPropertyName, string newJsonPropertyValue)
            {
                ParentProperty = parentProperty;
                NewJsonPropertyName = newJsonPropertyName;
                NewJsonPropertyValue = newJsonPropertyValue;
            }

            public string? ParentProperty { get; }

            public string NewJsonPropertyName { get; }

            public string NewJsonPropertyValue { get; }

            /// <summary>
            /// Creates an instance of <see cref="JsonContentParameters"/> based on the configured arguments in the Post Action.
            /// </summary>
            /// <param name="action"></param>
            /// <returns>A <see cref="JsonContentParameters"/> instance, or null if no instance could be created.</returns>
            public static JsonContentParameters? CreateFromPostAction(IPostAction action)
            {
                // If no ParentPropertyPath is specified, the new JSON property must be added to the root of the
                // document.
                action.Args.TryGetValue(ParentPropertyPathArgument, out string? parentProperty);
                action.Args.TryGetValue(NewJsonPropertyNameArgument, out string? newJsonPropertyName);
                action.Args.TryGetValue(NewJsonPropertyValueArgument, out string? newJsonPropertyValue);

                if (newJsonPropertyName == null)
                {
                    Reporter.Error.WriteLine(string.Format(LocalizableStrings.PostAction_ModifyJson_Error_ArgumentNotConfigured, NewJsonPropertyNameArgument));
                    return null;
                }

                if (newJsonPropertyValue == null)
                {
                    Reporter.Error.WriteLine(string.Format(LocalizableStrings.PostAction_ModifyJson_Error_ArgumentNotConfigured, NewJsonPropertyValueArgument));
                    return null;
                }

                return new JsonContentParameters(parentProperty, newJsonPropertyName, newJsonPropertyValue);
            }
        }
    }
}
