﻿using NuGet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;

namespace PecanWaffle {
    public class ProjectHelper {
        private const string PackagesSlash = @"packages\";
        private const string SlashPackagesSlash = @"\packages\";
        private const string Reference = "Reference";
        private const string HintPath = "HintPath";
        // taken from: https://github.com/ligershark/template-builder/blob/e801f5ef53a18739a3fb11b0c9b22d1e57bc00b5/src/TemplateBuilder/FixNuGetPackageHintPathsWizard.cs

        internal static void UpdatePackagesPathInProject(global::EnvDTE.Project project, string solutionFilePath) {
            if (project == null) { throw new ArgumentNullException(nameof(project)); }

            string projectFilePath = project.FileName;

            string projectDirectoryPath = Path.GetDirectoryName(projectFilePath);
            string solutionDirectoryPath = string.IsNullOrEmpty(solutionFilePath) ? projectDirectoryPath : Path.GetDirectoryName(solutionFilePath);
            string customPackagesDirectoryPath = ProjectHelper.GetCustomPackagesDirectoryPath(solutionDirectoryPath);

            string relativePackagesDirectoryPath = GetRelativePackagesDirectoryPath(
                projectDirectoryPath,
                solutionDirectoryPath,
                customPackagesDirectoryPath);

            bool hasChanged = false;
            Project buildProject = new Project(projectFilePath);
            foreach (ProjectMetadata metadata in buildProject.Items
                .Where(x => string.Equals(x.ItemType, Reference, StringComparison.OrdinalIgnoreCase))
                .SelectMany(x => x.Metadata)
                .Where(x => string.Equals(x.Name, HintPath, StringComparison.OrdinalIgnoreCase) &&
                    (x.UnevaluatedValue.StartsWith(PackagesSlash) || x.UnevaluatedValue.Contains(SlashPackagesSlash)))) {
                int startIndex;
                if (customPackagesDirectoryPath == null) {
                    startIndex = metadata.UnevaluatedValue.IndexOf(PackagesSlash);
                }
                else {
                    startIndex = metadata.UnevaluatedValue.IndexOf(PackagesSlash) + PackagesSlash.Length;
                }

                if (startIndex != -1) {
                    string newUnevaluatedValue = relativePackagesDirectoryPath + metadata.UnevaluatedValue.Substring(startIndex);
                    if (!string.Equals(metadata.UnevaluatedValue, newUnevaluatedValue, StringComparison.OrdinalIgnoreCase)) {
                        hasChanged = true;
                        metadata.UnevaluatedValue = newUnevaluatedValue;
                    }
                }
                else {
                    // If the project template author has used a nuget.config of their own, we are screwed. We don't 
                    // know which reference is a NuGet package reference as the folder could be named anything.
                    // So for safety we do nothing.
                }
            }

            if (hasChanged) {
                project.Save();
            }

            StringBuilder sb = new StringBuilder();
            foreach(var imp in buildProject.Imports) {
                sb.AppendLine(imp.ImportingElement.Project);
            }

            var foo = sb.ToString();
            var projElement = Microsoft.Build.Construction.ProjectRootElement.Open(projectFilePath);
            // x=>x.ImportingElement.Project.StartsWith(PackagesSlash) || x.ImportingElement.Project.Contains(SlashPackagesSlash))
            foreach (var import in projElement.Imports
                    .Where( x=>x.Project.StartsWith(PackagesSlash) || x.Project.Contains(SlashPackagesSlash) )) {
                int startIndex;
                if (customPackagesDirectoryPath == null) {
                    startIndex = import.Project.IndexOf(PackagesSlash);
                }
                else {
                    startIndex = import.Project.IndexOf(PackagesSlash) + PackagesSlash.Length;
                }

                if (startIndex != -1) {
                    string newUnevaluatedValue = relativePackagesDirectoryPath + import.Project.Substring(startIndex);
                    if (!string.Equals(import.Project, newUnevaluatedValue, StringComparison.OrdinalIgnoreCase)) {
                        hasChanged = true;
                        import.Project = newUnevaluatedValue;
                    }
                }
                else {
                    // If the project template author has used a nuget.config of their own, we are screwed. We don't 
                    // know which reference is a NuGet package reference as the folder could be named anything.
                    // So for safety we do nothing.
                }
            }

            if (hasChanged) {
                project.Save();
            }
        }
        internal static string GetRelativePackagesDirectoryPath(
            string projectDirectoryPath,
            string solutionDirectoryPath,
            string customPackagesDirectoryPath) {
            string relativePackagesDirectoryPath;
            if (customPackagesDirectoryPath == null) {
                relativePackagesDirectoryPath = GetRelativePath(
                    projectDirectoryPath,
                    solutionDirectoryPath);
            }
            else {
                // Absolute Custom Packages Path
                if (Path.IsPathRooted(customPackagesDirectoryPath)) {
                    return GetRelativePath(projectDirectoryPath, customPackagesDirectoryPath);
                }

                // Relative Custom Packages Path
                string path = Path.Combine(solutionDirectoryPath, customPackagesDirectoryPath);
                return GetRelativePath(projectDirectoryPath, path);
            }

            return relativePackagesDirectoryPath;
        }

        /// <summary>
        /// Creates a relative path from one file or folder to another.
        /// </summary>
        /// <param name="fromPath">Contains the directory that defines the start of the relative path.</param>
        /// <param name="toPath">Contains the path that defines the endpoint of the relative path.</param>
        /// <returns>The relative path from the start directory to the end path.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="fromPath"/> or <paramref name="toPath"/> is <c>null</c>.</exception>
        /// <exception cref="UriFormatException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        internal static string GetRelativePath(string fromPath, string toPath) {
            if (string.IsNullOrEmpty(fromPath)) {
                throw new ArgumentNullException("fromPath");
            }

            if (string.IsNullOrEmpty(toPath)) {
                throw new ArgumentNullException("toPath");
            }

            Uri fromUri = new Uri(AppendDirectorySeparatorChar(fromPath));
            Uri toUri = new Uri(AppendDirectorySeparatorChar(toPath));

            if (fromUri.Scheme != toUri.Scheme) {
                return toPath;
            }

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (string.Equals(toUri.Scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase)) {
                relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            return relativePath;
        }

        internal static string AppendDirectorySeparatorChar(string directoryPath) {
            if (!Path.HasExtension(directoryPath) &&
                !directoryPath.EndsWith(Path.DirectorySeparatorChar.ToString())) {
                return directoryPath + Path.DirectorySeparatorChar;
            }

            return directoryPath;
        }

        internal static string GetCustomPackagesDirectoryPath(string projectDirectoryPath) {
            // Read the nuget.config file and use the repository path there instead.
            // This is actually very complicated. See https://docs.nuget.org/consume/nuget-config-file
            // <?xml version="1.0" encoding="utf-8"?>
            // <configuration>
            //   <config>
            //     <add key="repositorypath" value="c:\blah" />
            //   </config>
            // </configuration>

            string rootPath = Path.GetPathRoot(projectDirectoryPath);
            IFileSystem fileSystem = new PhysicalFileSystem(rootPath);
            var settings = Settings.LoadMachineWideSettings(fileSystem, projectDirectoryPath);
            return settings.Select(x => x.GetRepositoryPath()).Where(x => x != null).FirstOrDefault();
        }


    }
}
