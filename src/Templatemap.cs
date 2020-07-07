﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.AddAnyFile
{
    static class TemplateMap
    {
        static readonly string _folder;
        static readonly string[] _templateFiles;
        const string _defaultExt = ".txt";
        const int _lacalTemplateSearchDepth = 3;

        static TemplateMap()
        {
            string assembly = Assembly.GetExecutingAssembly().Location;
            _folder = Path.Combine(Path.GetDirectoryName(assembly), "Templates");
            _templateFiles = Directory.GetFiles(_folder, "*" + _defaultExt, SearchOption.AllDirectories);
        }

        public static async Task<string> GetTemplateFilePathAsync(Project project, string file)
        {
            string extension = Path.GetExtension(file).ToLowerInvariant();
            string name = Path.GetFileName(file);
            string safeName = name.StartsWith(".") ? name : Path.GetFileNameWithoutExtension(file);
            string relative = PackageUtilities.MakeRelative(project.GetRootFolder(), Path.GetDirectoryName(file));

            string localTemplate = SearchForLocalTemplate(Path.GetDirectoryName(file), relative, _lacalTemplateSearchDepth);

            string templateFile = null;

            // Look for the local template
            if (!string.IsNullOrEmpty(localTemplate))
            {
                templateFile = localTemplate;
            }
            else
            {
                // Look for direct file name matches
                if (_templateFiles.Any(f => Path.GetFileName(f).Equals(name + _defaultExt, StringComparison.OrdinalIgnoreCase)))
                {
                    templateFile = GetTemplate(name);
                }

                // Look for file extension matches
                else if (_templateFiles.Any(f => Path.GetFileName(f).Equals(extension + _defaultExt, StringComparison.OrdinalIgnoreCase)))
                {
                    string tmpl = AdjustForSpecific(safeName, extension);
                    templateFile = GetTemplate(tmpl);
                }
            }

            string template = await ReplaceTokensAsync(project, safeName, relative, templateFile);
            return NormalizeLineEndings(template);
        }

        private static string GetTemplate(string name)
        {
            return Path.Combine(_folder, name + _defaultExt);
        }

        private static async Task<string> ReplaceTokensAsync(Project project, string name, string relative, string templateFile)
        {
            if (string.IsNullOrEmpty(templateFile))
                return templateFile;

            string rootNs = project.GetRootNamespace();
            string ns = string.IsNullOrEmpty(rootNs) ? "MyNamespace" : rootNs;

            if (!string.IsNullOrEmpty(relative))
            {
                ns += "." + ProjectHelpers.CleanNameSpace(relative);
            }

            using (var reader = new StreamReader(templateFile))
            {
                string content = await reader.ReadToEndAsync();

                return content.Replace("{namespace}", ns)
                              .Replace("{itemname}", name);
            }
        }

        private static string NormalizeLineEndings(string content)
        {
            if (string.IsNullOrEmpty(content))
                return content;

            return Regex.Replace(content, @"\r\n|\n\r|\n|\r", "\r\n");
        }

        private static string AdjustForSpecific(string safeName, string extension)
        {
            if (Regex.IsMatch(safeName, "^I[A-Z].*"))
                return extension += "-interface";

            return extension;
        }

        private static IEnumerable<DirectoryInfo> GetAllParentDirectories(DirectoryInfo directoryToScan)
        {
            Stack<DirectoryInfo> ret = new Stack<DirectoryInfo>();
            GetAllParentDirectories(directoryToScan, ref ret);
            return ret;
        }

        private static void GetAllParentDirectories(DirectoryInfo directoryToScan, ref Stack<DirectoryInfo> directories)
        {
            if (directoryToScan == null || directoryToScan.Name == directoryToScan.Root.Name)
                return;

            directories.Push(directoryToScan);
            GetAllParentDirectories(directoryToScan.Parent, ref directories);
        }
        
        private static string SearchForLocalTemplate(string path, string relative, int depth)
        {
            var rootDirectory = new DirectoryInfo(relative).Root;
            var currentDirectory = new DirectoryInfo(path);

            for (int i = 0; i < depth; i++)
            {
                var template = Directory.GetFiles(currentDirectory.FullName, "*.template", SearchOption.TopDirectoryOnly).FirstOrDefault();
                if (string.IsNullOrEmpty(template))
                {
                    if (currentDirectory.Name == rootDirectory.Name) return null;
                    currentDirectory = currentDirectory.Parent;
                }
                else
                {
                    return template;
                }
            }
            return null;
        }
    }
}
