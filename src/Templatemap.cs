using System;
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
    public class TemplateInfo
    {
        public string Content;
        public string TemplateFullName;
        public string LongItemName;
        public string ShortItemName;
        public bool HasReplacementTitle;
        public string WritePath;
        public bool IsLocal;
        public string Extension;
        public int CursorPosition;
    }

    static class TemplateMap
    {
        static readonly string _folder;
        static readonly string[] _templateFiles;
        const string _defaultExt = ".txt";
        const string _csExt = ".cs";
        const int _localTemplateSearchDepth = 3;

        static TemplateMap()
        {
            string assembly = Assembly.GetExecutingAssembly().Location;
            _folder = Path.Combine(Path.GetDirectoryName(assembly), "Templates");
            _templateFiles = Directory.GetFiles(_folder, "*" + _defaultExt, SearchOption.AllDirectories);
        }

        public static async Task<TemplateInfo> GetTemplateInfoAsync(Project project, string file)
        {
            TemplateInfo result = new TemplateInfo();
            string extension = Path.GetExtension(file).ToLowerInvariant();
            string name = Path.GetFileName(file);
            string safeName = name.StartsWith(".") ? name : Path.GetFileNameWithoutExtension(file);
            string relative = PackageUtilities.MakeRelative(project.GetRootFolder(), Path.GetDirectoryName(file));
            string folder = Path.GetDirectoryName(file);

            string localTemplate = SearchForLocalTemplate(Path.GetDirectoryName(file), relative, _localTemplateSearchDepth);

            result.IsLocal = !string.IsNullOrEmpty(localTemplate);

            string templateFullName = null;

            if (name == AddAnyFilePackage.Dummy)
            {
                
            }
            // Look for the local template
            else if (result.IsLocal)
            {
                templateFullName = localTemplate;
            }
            else
            {
                // Look for direct file name matches
                if (_templateFiles.Any(f => Path.GetFileName(f).Equals(name + _defaultExt, StringComparison.OrdinalIgnoreCase)))
                {
                    templateFullName = GetTemplate(name);
                }

                // Look for file extension matches
                else if (_templateFiles.Any(f => Path.GetFileName(f).Equals(extension + _defaultExt, StringComparison.OrdinalIgnoreCase)))
                {
                    string tmpl = AdjustForSpecific(safeName, extension);
                    templateFullName = GetTemplate(tmpl);
                }
            }

            if (name != AddAnyFilePackage.Dummy) result.Extension = string.IsNullOrEmpty(extension) ? _csExt : extension;

            HandleItemName(safeName, templateFullName, result);
            result.WritePath = result.IsLocal? Path.Combine(folder, result.LongItemName + result.Extension) : file; 
            string templateContent = await ReplaceTokensAsync(project, result, relative, templateFullName);
            result.TemplateFullName = templateFullName;
            result.Content = NormalizeLineEndings(templateContent);
            return result;
        }



        private static string GetTemplate(string name)
        {
            return Path.Combine(_folder, name + _defaultExt);
        }

        private static async Task<string> ReplaceTokensAsync(Project project, TemplateInfo template, string relative, string templateFullName)
        {
            if (string.IsNullOrEmpty(templateFullName))
                return templateFullName;

            string rootNs = project.GetRootNamespace();
            string ns = string.IsNullOrEmpty(rootNs) ? "MyNamespace" : rootNs;

            if (!string.IsNullOrEmpty(relative))
            {
                ns += "." + ProjectHelpers.CleanNameSpace(relative);
            }

            using (var reader = new StreamReader(templateFullName))
            {
                string content = await reader.ReadToEndAsync();

                var itemname = template.ShortItemName;

                return content.Replace("{namespace}", ns)
                              .Replace("{itemname}", itemname);
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

        private static void HandleItemName(string inputName, string templateFullName, TemplateInfo template)
        {
            const string replacement = "{itemname}";
                
            if (templateFullName == null)
            {
                template.LongItemName = inputName;
                template.ShortItemName = inputName;
            }
            else if (templateFullName.Contains(replacement))
            {
                template.HasReplacementTitle = true;

                var rawName = Path.GetFileNameWithoutExtension(templateFullName)
                    .Replace(".", "");

                var prefix = rawName.Replace(replacement, "");

                // if the user input filename already contains prefix
                if (inputName.Contains(prefix))
                {
                    template.ShortItemName = inputName.Replace(prefix, "");
                    template.LongItemName = inputName;
                }
                else
                {
                    template.ShortItemName = inputName;
                    template.LongItemName = rawName.Replace(replacement, inputName);
                }
            } else
            {
                template.LongItemName = inputName;
                template.ShortItemName = inputName;
            }
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
