﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.Extensions.Logging;

namespace Hagar.CodeGenerator.MSBuild
{
    public class CodeGeneratorCommand
    {
        private const string HagarAssemblyShortName = "Hagar";

        private static readonly int[] SuppressCompilerWarnings =
        {
            162, // CS0162 - Unreachable code detected.
            219, // CS0219 - The variable 'V' is assigned but its value is never used.
            414, // CS0414 - The private field 'F' is assigned but its value is never used.
            618, // CS0616 - Member is obsolete.
            649, // CS0649 - Field 'F' is never assigned to, and will always have its default value.
            693, // CS0693 - Type parameter 'type parameter' has the same name as the type parameter from outer type 'T'
            1591, // CS1591 - Missing XML comment for publicly visible type or member 'Type_or_Member'
            1998 // CS1998 - This async method lacks 'await' operators and will run synchronously
        };

        public ILogger Log { get; set; }
        
        /// <summary>
        /// The MSBuild project path.
        /// </summary>
        public string ProjectPath { get; set; }

        /// <summary>
        /// The optional ProjectGuid.
        /// </summary>
        public string ProjectGuid { get; set; }

        /// <summary>
        /// The output type, such as Exe, or Library.
        /// </summary>
        public string OutputType { get; set; }

        /// <summary>
        /// The target path of the compilation.
        /// </summary>
        public string TargetPath { get; set; }

        /// <summary>
        /// The source files.
        /// </summary>
        public List<string> Compile { get; } = new List<string>();

        /// <summary>
        /// The libraries referenced by the project.
        /// </summary>
        public List<string> Reference { get; } = new List<string>();

        /// <summary>
        /// The file which holds the generated code.
        /// </summary>
        public string CodeGenOutputFile { get; set; }

        public async Task<bool> Execute(CancellationToken cancellationToken)
        {
            try
            {
                var projectName = Path.GetFileNameWithoutExtension(this.ProjectPath);
                var projectId = !string.IsNullOrEmpty(this.ProjectGuid) && Guid.TryParse(this.ProjectGuid, out var projectIdGuid)
                    ? ProjectId.CreateFromSerialized(projectIdGuid)
                    : ProjectId.CreateNewId();


                this.Log.LogDebug($"ProjectGuid: {this.ProjectGuid}");
                this.Log.LogDebug($"ProjectID: {projectId}");

                var languageName = GetLanguageName(this.ProjectPath);
                var documents = GetDocuments(this.Compile, projectId).ToList();
                var metadataReferences = GetMetadataReferences(this.Reference).ToList();
                
                foreach (var doc in documents)
                    this.Log.LogDebug($"Document: {doc.FilePath}");
                foreach (var reference in metadataReferences)
                    this.Log.LogDebug($"Reference: {reference.Display}");

                var projectInfo = ProjectInfo.Create(
                    projectId,
                    VersionStamp.Create(),
                    projectName,
                    projectName,
                    languageName,
                    this.ProjectPath,
                    this.TargetPath,
                    CreateCompilationOptions(this.OutputType, languageName),
                    documents: documents,
                    metadataReferences: metadataReferences
                );
                this.Log.LogDebug($"Project: {projectInfo}");

                var workspace = new AdhocWorkspace();
                workspace.AddProject(projectInfo);

                var project = workspace.CurrentSolution.Projects.Single();

                var stopwatch = Stopwatch.StartNew();
                var compilation = await project.GetCompilationAsync(cancellationToken);
                this.Log.LogInformation($"GetCompilation completed in {stopwatch.ElapsedMilliseconds}ms.");

                if (compilation.ReferencedAssemblyNames.All(name => name.Name != HagarAssemblyShortName)) return false;

                var generator = new CodeGenerator(compilation);
                stopwatch.Restart();
                var syntax = await generator.GenerateCode(cancellationToken);
                this.Log.LogInformation($"GenerateCode completed in {stopwatch.ElapsedMilliseconds}ms.");
                stopwatch.Restart();
                
                var normalized = syntax.NormalizeWhitespace();
                this.Log.LogInformation($"NormalizeWhitespace completed in {stopwatch.ElapsedMilliseconds}ms.");
                stopwatch.Restart();

                var source = normalized.ToFullString();
                this.Log.LogInformation($"Generate source from syntax completed in {stopwatch.ElapsedMilliseconds}ms.");
                stopwatch.Restart();
                using (var sourceWriter = new StreamWriter(this.CodeGenOutputFile))
                {
                    sourceWriter.WriteLine("#if !EXCLUDE_GENERATED_CODE");
                    foreach (var warningNum in SuppressCompilerWarnings) await sourceWriter.WriteLineAsync($"#pragma warning disable {warningNum}");
                    if (!string.IsNullOrWhiteSpace(source)) await sourceWriter.WriteLineAsync(source);
                    foreach (var warningNum in SuppressCompilerWarnings) await sourceWriter.WriteLineAsync($"#pragma warning restore {warningNum}");
                    sourceWriter.WriteLine("#endif");
                }
                this.Log.LogInformation($"Write source to disk completed in {stopwatch.ElapsedMilliseconds}ms.");

                return true;
            }
            catch (ReflectionTypeLoadException rtle)
            {
                foreach (var ex in rtle.LoaderExceptions)
                    this.Log.LogDebug($"Exception: {ex}");
                throw;
            }
        }

        private static IEnumerable<DocumentInfo> GetDocuments(List<string> sources, ProjectId projectId) =>
            sources
                ?.Where(File.Exists)
                .Select(x => DocumentInfo.Create(
                    DocumentId.CreateNewId(projectId),
                    Path.GetFileName(x),
                    loader: TextLoader.From(
                        TextAndVersion.Create(
                            SourceText.From(File.ReadAllText(x)), VersionStamp.Create())),
                    filePath: x))
            ?? Array.Empty<DocumentInfo>();

        private static IEnumerable<MetadataReference> GetMetadataReferences(List<string> references) =>
            references
                ?.Where(File.Exists)
                .Select(x => MetadataReference.CreateFromFile(x))
            ?? (IEnumerable<MetadataReference>)Array.Empty<MetadataReference>();


        private static string GetLanguageName(string projectPath)
        {
            switch (Path.GetExtension(projectPath))
            {
                case ".csproj":
                    return LanguageNames.CSharp;
                case string ext when !string.IsNullOrWhiteSpace(ext):
                    throw new NotSupportedException($"Projects of type {ext} are not supported.");
                default:
                    throw new InvalidOperationException("Could not determine supported language from project path");

            }
        }

        private static CompilationOptions CreateCompilationOptions(string outputType, string languageName)
        {
            OutputKind? kind = null;
            switch (outputType)
            {
                case "Library":
                    kind = OutputKind.DynamicallyLinkedLibrary;
                    break;
                case "Exe":
                    kind = OutputKind.ConsoleApplication;
                    break;
                case "Module":
                    kind = OutputKind.NetModule;
                    break;
                case "Winexe":
                    kind = OutputKind.WindowsApplication;
                    break;
            }

            if (kind.HasValue)
            {
                if (languageName == LanguageNames.CSharp)
                {
                    return new CSharpCompilationOptions(kind.Value);
                }
                if (languageName == LanguageNames.VisualBasic)
                {
                    return new VisualBasicCompilationOptions(kind.Value);
                }
            }

            return null;
        }
    }
}
