using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace ResXManager.View.Visuals;

internal static class IncludeAllResxFiles
{
    public static void Build(string solutionFolder)
    {
        var projd = Directory.GetFiles(solutionFolder, "*.csproj", SearchOption.AllDirectories);

        foreach (var projPath in projd)
        {
            var isSdkStyleProject = Regex.IsMatch(File.ReadAllText(projPath), "^\\s*\\<\\s*Project\\s+Sdk\\s*=\\s*\"");

            if (isSdkStyleProject)
                continue;

            var doc = new XmlDocument();
            doc.Load(projPath);
            var nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("msbuild", "http://schemas.microsoft.com/developer/msbuild/2003");
            var embeddedResources = doc.SelectNodes("//msbuild:EmbeddedResource", nsmgr)
                .Cast<XmlNode>()
                .Select(x => x.Attributes["Include"].Value)
                .ToHashSet();
            var projDir = Path.GetDirectoryName(projPath);
            var localizedResx = GetLocalizedResX(projDir);

            foreach (var resource in localizedResx)
            {
                var relativePath = resource.Replace(projDir + "\\", string.Empty);
                if (!embeddedResources.Contains(relativePath))
                {
                    //AnsiConsole.MarkupLine($"[green] adding {relativePath} in {projPath} [/]");

                    var itemGroup = doc.CreateElement("ItemGroup", nsmgr.LookupNamespace("msbuild"));
                    var embeddedResource = doc.CreateElement("EmbeddedResource", nsmgr.LookupNamespace("msbuild"));
                    embeddedResource.SetAttribute("Include", relativePath);
                    var dependentUpon = doc.CreateElement("DependentUpon", nsmgr.LookupNamespace("msbuild"));
                    var csFile = relativePath.Split('\\').Last();
                    csFile = csFile.Replace(".fr", string.Empty);
                    csFile = csFile.Replace(".de", string.Empty);
                    csFile = csFile.Replace(".zh-CN", string.Empty);
                    csFile = csFile.Replace(".resx", string.Empty);

                    if (IsWinformDesignerFile(resource))
                        csFile += ".cs";

                    dependentUpon.InnerText = csFile;
                    embeddedResource.AppendChild(dependentUpon);
                    itemGroup.AppendChild(embeddedResource);
                    doc.DocumentElement.AppendChild(itemGroup);
                }
                else
                {
                    //AnsiConsole.MarkupLine($"[yellow] already exist resource {relativePath} in {projPath} [/]");
                    var file = doc.SelectSingleNode($"//msbuild:EmbeddedResource[@Include='{relativePath}']", nsmgr);
                    if (file != null)
                    {
                        var dependentUponNode = file.SelectSingleNode("msbuild:DependentUpon", nsmgr);
                        if (dependentUponNode != null)
                            file.RemoveChild(dependentUponNode);

                        var dependentUpon = doc.CreateElement("DependentUpon", nsmgr.LookupNamespace("msbuild"));
                        var csFile = relativePath.Split('\\').Last();
                        csFile = csFile.Replace(".fr", string.Empty);
                        csFile = csFile.Replace(".de", string.Empty);
                        csFile = csFile.Replace(".zh-CN", string.Empty);
                        csFile = csFile.Replace(".resx", string.Empty);

                        if (IsWinformDesignerFile(resource))
                            csFile += ".cs";

                        dependentUpon.InnerText = csFile;
                        file.AppendChild(dependentUpon);
                    }
                }
            }

            doc.Save(projPath);
        }
        //AnsiConsole.MarkupLine("[green] Finished Adding ResX in csproj[/]");
        //AnsiConsole.WriteLine("Done");


    }
    private static string[] GetLocalizedResX(string projectPath)
    {
        //AnsiConsole.MarkupLine("[yellow] finding all resources in directory [/]");

        var resx = Directory.GetFiles(projectPath, "*.resx", SearchOption.AllDirectories);
        return resx.Where(y =>
        {
            var x = Path.GetFileNameWithoutExtension(y);
            return x.EndsWith("de") || x.EndsWith("fr") || x.EndsWith("zh-CN");
        }).ToArray();
    }

    private static bool IsWinformDesignerFile(string resxFile)
    {
        resxFile = resxFile.Replace(".fr", string.Empty);
        resxFile = resxFile.Replace(".de", string.Empty);
        resxFile = resxFile.Replace(".zh-CN", string.Empty);

        var designerFile = resxFile.Replace(".resx", ".cs");
        return File.Exists(designerFile);
    }
}


