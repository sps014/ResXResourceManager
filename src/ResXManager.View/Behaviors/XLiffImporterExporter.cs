using Microsoft.Win32;
using ResXManager.Infrastructure;
using ResXManager.Model.XLif;
using ResXManager.View.Visuals;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using System.Xml.Serialization;

namespace ResXManager.View
{
    internal static class XLiffImporterExporter
    {
        public static void ExportXliff(ResourceViewModel _resourceViewModel, CultureKey culturekey)
        {
            var file = new Xliff
            {
                File = new Model.XLif.File()
            };

            file.File.Original = _resourceViewModel.ResourceManager.SolutionFolder;
            file.File.Datatype = "sln";
            file.File.Sourcelanguage = "en";
            file.File.Targetlanguage = culturekey.Culture.Name;
            file.Version = "1.2";
            file.File.Body = new Body();

            XmlSerializer serializer = new(typeof(Xliff));
            using StringWriter ss = new();

            var projectResources = _resourceViewModel.ResourceManager.TableEntries.GroupBy(x => x.Container.ProjectName);
            foreach (var project in projectResources)
            {
                var projGroup = new Group
                { Datatype = "csproj", Id = project.First().Container.ProjectName };

                foreach (var rgs in project.GroupBy(y => y.Container.UniqueName))
                {
                    var resourceGroup = new Group
                    { Datatype = "resx", Id = rgs.First().Container.UniqueName };

                    foreach (var r in rgs)
                    {
                        var culturalValue = r.Values.GetValue(culturekey.Culture);
                        var key = r.Key;
                        var neutralValue = r.Values.GetValue(null);
                        var status = r.Comments.GetValue(culturekey.Culture);

                        var transUnit = new Transunit
                        {
                            Id = key,
                            Source = neutralValue,
                            Target = new Target
                            {
                                State = status,
                                Text = culturalValue,
                            },
                            Note = r.Comments.GetValue(culturekey.Culture)
                        };
                        resourceGroup.Transunits.Add(transUnit);
                    }
                    projGroup.Groups.Add(resourceGroup);
                }

                file.File.Body.Group.Add(projGroup);
            }

            serializer.Serialize(ss, file);
            var str = ss.ToString();

            SaveFileDialog sfd = new()
            {
                Filter = "XLF file|*.xlf|XLIFF file|*.xliff"
            };

            if (sfd.ShowDialog().GetValueOrDefault())
            {
                System.IO.File.WriteAllText(sfd.FileName, str);
            }


        }

        public static void ImportXliff(ResourceViewModel _resourceViewModel,string text)
        {
            XmlSerializer serializer = new(typeof(Xliff));
            using XmlReader reader = XmlReader.Create(new StringReader(text));
            var xliff = (Xliff)serializer.Deserialize(reader);

            //if (MessageBox.Show("Do you want to create a backup snapshot of current resources"
            //    , "Snapshot backup", MessageBoxButton.YesNo, MessageBoxImage.Hand) == MessageBoxResult.Yes)
            //{
            //    //TODO backup
            //}
            var cul = _resourceViewModel.ResourceManager.Cultures;
            var curCul = cul.First(x => !x.IsNeutral && x.Culture?.Name == xliff.File.Targetlanguage);

            foreach (var project in xliff.File.Body.Group)
            {
                var projectRes = _resourceViewModel.ResourceManager.TableEntries.Where(x => x.Container.ProjectName == project.Id);

                foreach (var resource in project.Groups)
                {
                    var Res = projectRes.Where(x => x.Container.UniqueName == resource.Id);

                    foreach (var trans in resource.Transunits)
                    {
                        var item = Res.FirstOrDefault(x => x.Key == trans.Id);
                        if (item == null)
                        {
                            MessageBox.Show($"not found Key: {trans.Id} in {resource.Id}.resx of {project.Id}", "Error");
                            return;
                        }
                        var r = item.Languages;
                        if (!r.Contains(curCul))
                            continue;
                        item.Values.SetValue(curCul.Culture, trans.Target.Text);
                        item.Comments.SetValue(curCul.Culture, trans.Target.State);
                    }

                }
            }
            _resourceViewModel.ResourceManager.Save();

            //foreach (var gp in xliff.File.Body.Group)
            //{
            //    foreach (var tunit in gp.Transunits)
            //    {
            //        var r = resources.First(x => x.Key == tunit.Id);
            //        r.Values.SetValue(xliff.File.Targetlanguage, tunit.Target.Text);
            //        r.Comments.SetValue(xliff.File.Targetlanguage, tunit.Target.State);
            //    }
            //}
            //_resourceManager.Save();

        }
    }
}
