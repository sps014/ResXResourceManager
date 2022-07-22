namespace ResXManager.View.Visuals
{
    using System;
    using System.Collections.Generic;
    using System.Composition;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Controls;
    using System.Xml;
    using System.Xml.Serialization;
    using DataGridExtensions;
    using DocumentFormat.OpenXml;
    using Microsoft.Win32;
    using ResX.Scripting;
    using ResXManager.Infrastructure;
    using ResXManager.Model;
    using ResXManager.Model.XLif;
    using ResXManager.View.Behaviors;
    using ResXManager.View.Tools;

    using TomsToolbox.Composition;
    using TomsToolbox.Essentials;
    using TomsToolbox.ObservableCollections;
    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition;
    using TomsToolbox.Wpf.Composition.AttributedModel;
    using TomsToolbox.Wpf.Converters;
    using File = System.IO.File;
    using Group = Model.XLif.Group;

    /// <summary>
    /// Interaction logic for ResourceView.xaml
    /// </summary>
    [DataTemplate(typeof(ResourceViewModel))]
    [Shared]
    public partial class ResourceView
    {
        private readonly ResourceManager _resourceManager;
        private readonly IConfiguration _configuration;
        private readonly ResourceViewModel _resourceViewModel;
        private readonly ITracer _tracer;

        [ImportingConstructor]
        public ResourceView(IExportProvider exportProvider)
        {
            _resourceManager = exportProvider.GetExportedValue<ResourceManager>();
            _resourceViewModel = exportProvider.GetExportedValue<ResourceViewModel>();
            _configuration = exportProvider.GetExportedValue<IConfiguration>();
            _tracer = exportProvider.GetExportedValue<ITracer>();
            _resourceViewModel.ClearFiltersRequest += ResourceViewModel_ClearFiltersRequest;

            try
            {
                this.SetExportProvider(exportProvider);

                _resourceManager.Loaded += ResourceManager_Loaded;
                _resourceManager.LanguageAdded += ResourceManager_LanguageAdded;

                InitializeComponent();

                DataGrid?.SetupColumns(_resourceManager, _resourceViewModel, _configuration);
                DataGridTryBeginEditBehavior.OnEditEnded += DataGridTryBeginEditBehavior_OnEditEnded;

            }
            catch (Exception ex)
            {
                exportProvider.TraceXamlLoaderError(ex);
            }
        }

        private void DataGridTryBeginEditBehavior_OnEditEnded(object? sender, CustomEditCommitArgs e)
        {
            var dataGridName = (sender as DataGrid)!.Name;
            if (dataGridName.Contains("ItemsList"))
            {
                return;
            }
            if (e.PreviousValue == e.CurrentValue)
                return;

            var match = Regex.Match(e.ColumnName, @"\[\.(.*)\]");

            if (!match.Success)
                return;

            var culture = match.Groups[1].Value;

            e.Culture = culture;

            //built a cache for given culture text

            var cache = BuildCache(e.PreviousValue, culture,
                e.Entry.Container.ProjectName,
                e.Entry.Container.UniqueName);

            if (!cache.Any())
                return;

            if (MessageBox
                .Show($"Previous Value \"{e.PreviousValue}\" occurs {cache.Count} times at other places, Do you want to modify them also?",
                "Edit Similar Items", MessageBoxButton.YesNo, MessageBoxImage.Information)
                != MessageBoxResult.Yes)
            {
                return;
            }

            var window = new EditSimilarWindow();
            window.ResourceManager = _resourceManager;
            window.Cache = cache;
            window.CustomEditEventArgs = e;
            window.ShowDialog();
        }

        private HashSet<TranslateContainerModel> BuildCache(string text, string culture,string projectName,string resourceName)
        {
            var result = new HashSet<TranslateContainerModel>();

            foreach (var e in _resourceManager.TableEntries)
            {
                var value = e.Values.GetValue(culture);
                if (text != value || (resourceName == e.Container.UniqueName
                    && projectName == e.Container.ProjectName))
                    continue;

                result.Add(new TranslateContainerModel()
                {
                    Key = e.Key,
                    ProjectName = e.Container.ProjectName,
                    UniqueName = e.Container.UniqueName,
                    Entry = e
                });
            }

            return result;
        }

        private void ResourceViewModel_ClearFiltersRequest(object? sender, ResourceTableEntryEventArgs e)
        {
            var filter = DataGrid.Items.Filter;

            if (filter == null)
                return;

            if (filter(e.Entry))
                return;

            DataGrid.GetFilter().Clear();
        }

        private void ResourceManager_Loaded(object? sender, EventArgs e)
        {
            DataGrid?.SetupColumns(_resourceManager, _resourceViewModel, _configuration);
        }

        private void ResourceManager_LanguageAdded(object? sender, LanguageEventArgs e)
        {
            DataGrid.CreateNewLanguageColumn(_configuration, e.Language.Culture);
        }

        private void AddLanguage_Click(object? sender, RoutedEventArgs e)
        {
            var existingCultures = _resourceManager.Cultures
                .Select(c => c.Culture)
                .ExceptNullItems();

            var languageSelection = new LanguageSelectionBoxViewModel(existingCultures);

            var window = Window.GetWindow(this);

            if (!ConfirmationDialog.Show(this.GetExportProvider(), languageSelection, Properties.Resources.Title, window).GetValueOrDefault())
                return;

            WaitCursor.Start(this);

            var culture = languageSelection.SelectedLanguage;

            DataGrid.CreateNewLanguageColumn(_configuration, culture);

            if (!_configuration.AutoCreateNewLanguageFiles)
                return;

            if (!_resourceManager.ResourceEntities.All(resourceEntity => _resourceManager.CanEdit(resourceEntity, culture)))
            {
                // nothing left to do, message already shown.
            }
        }

        private void CreateSnapshotCommandConverter_Executing(object? sender, ConfirmedCommandEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                AddExtension = true,
                CheckPathExists = true,
                DefaultExt = ".snapshot",
                Filter = "Snapshots|*.snapshot|All Files|*.*",
                FilterIndex = 0,
                FileName = DateTime.Today.ToString("yyyy_MM_dd", CultureInfo.InvariantCulture)
            };

            if (!dlg.ShowDialog().GetValueOrDefault())
                e.Cancel = true;
            else
                e.Parameter = dlg.FileName;

            WaitCursor.Start(this);
        }

        private void LoadSnapshotCommandConverter_Executing(object? sender, ConfirmedCommandEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                AddExtension = true,
                CheckPathExists = true,
                CheckFileExists = true,
                DefaultExt = ".snapshot",
                Filter = "Snapshots|*.snapshot|All Files|*.*",
                FilterIndex = 0,
                Multiselect = false
            };

            if (!dlg.ShowDialog().GetValueOrDefault())
                e.Cancel = true;
            else
                e.Parameter = dlg.FileName;

            WaitCursor.Start(this);
        }

        private void ExportExcelCommandConverter_Executing(object? sender, ConfirmedCommandEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                AddExtension = true,
                CheckPathExists = true,
                DefaultExt = ".xlsx",
                Filter = "Excel Worksheets|*.xlsx|All Files|*.*",
                FilterIndex = 0,
                FileName = DateTime.Now.ToString("yyyy_MM_dd_HH_mm", CultureInfo.InvariantCulture)
            };

            if (_configuration.ExcelExportMode == ExcelExportMode.Text)
            {
                dlg.DefaultExt = ".txt";
                dlg.Filter = "Text files|*.txt|CSV files|*.csv|All Files|*.*";
            }

            if (!dlg.ShowDialog().GetValueOrDefault())
                e.Cancel = true;
            else
                e.Parameter = new ExportParameters(dlg.FileName, e.Parameter as IResourceScope);

            WaitCursor.Start(this);
        }

        private void ImportExcelCommandConverter_Executing(object? sender, ConfirmedCommandEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                AddExtension = true,
                CheckPathExists = true,
                CheckFileExists = true,
                DefaultExt = ".xlsx",
                Filter = "Exported files|*.xlsx;*.txt;*.csv|All Files|*.*",
                FilterIndex = 0,
                Multiselect = false
            };

            if (!dlg.ShowDialog().GetValueOrDefault())
                e.Cancel = true;
            else
                e.Parameter = dlg.FileName;

            WaitCursor.Start(this);
        }

        private void DeleteCommandConverter_Executing(object? sender, ConfirmedCommandEventArgs e)
        {
            if (MessageBox.Show(Properties.Resources.ConfirmDeleteItems, Properties.Resources.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                e.Cancel = true;
            }
        }

        private void CutCommandConverter_Executing(object? sender, ConfirmedCommandEventArgs e)
        {
            if (MessageBox.Show(Properties.Resources.ConfirmCutItems, Properties.Resources.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                e.Cancel = true;
            }
        }

        private void CommandConverter_Error(object? sender, ErrorEventArgs e)
        {
            var ex = e.GetException();

            if (ex == null)
                return;

            MessageBox.Show(ex.Message, Properties.Resources.Title);

            if (ex is ImportException)
                return;

            _tracer.TraceError(ex.ToString());
        }

        private class ExportParameters : IExportParameters
        {
            public ExportParameters(string? fileName, IResourceScope? scope)
            {
                FileName = fileName;
                Scope = scope;
            }

            public IResourceScope? Scope
            {
                get;
            }

            public string? FileName
            {
                get;
            }
        }

        private void Diff_Click(object sender, RoutedEventArgs e)
        {
            var folder = _resourceManager.SolutionFolder;
            int maxDepth = 4;
            while (maxDepth>0 && folder!.Length>4 &&
                !File.Exists($"{folder}\\Localization\\Localization_Full.snapshot"))
            {
                folder = new DirectoryInfo(folder).Parent.FullName;
                maxDepth--;
            }
            var file = $"{folder}\\Localization\\Localization_Full.snapshot";
            while (!File.Exists(file))
            {
                file = $"{new DirectoryInfo(_resourceManager.SolutionFolder)}";
            }
            OpenFileDialog openFileDialog = new()
            {
                Title = "Import Snapshot file",
                FileName = "Snapshot",
                Filter = " Snapshot File | *.snapshot"
            };
            if (!showDiff && openFileDialog.ShowDialog().GetValueOrDefault())
            {
                file = openFileDialog.FileName;
            }

            _resourceManager.LoadSnapshot(File.ReadAllText(file));

            Perform();

        }
        private bool showDiff;
        public void Perform()
        {

            if (!showDiff)
            {
                var changes = CalcDiff();
                _resourceViewModel.ResourceTableEntries =
                    _resourceViewModel.ResourceTableEntries
                    .ObservableWhere(y => changes.Contains(y))
                    .ObservableSelectMany(x => x.Container.Entries);
            }
            else
            {
                _resourceViewModel.ResourceTableEntries = _resourceManager.TableEntries;

            }
            showDiff = !showDiff;
            diff_check.IsChecked = showDiff;
        }
        private HashSet<ResourceTableEntry> CalcDiff()
        {
            var allChanges = new HashSet<ResourceTableEntry>();
            var entries = _resourceManager.TableEntries.GroupBy(x => x.Container);
            foreach (var gp in entries)
            {
                foreach (var resource in gp)
                {
                    foreach (var lang in resource.Languages)
                    {
                        var snap = resource.SnapshotValues.GetValue(lang.Culture);
                        var value = resource.Values.GetValue(lang.Culture);

                        if (string.IsNullOrWhiteSpace(snap) && string.IsNullOrWhiteSpace(value))
                        {
                            continue;
                        }

                        if ((string.IsNullOrWhiteSpace(snap) && !string.IsNullOrWhiteSpace(value)) ||
                            (!string.IsNullOrWhiteSpace(snap) && string.IsNullOrWhiteSpace(value)) ||
                            !snap.Equals(value, StringComparison.Ordinal))
                        {
                            allChanges.Add(resource);
                            break;
                        }
                    }
                }
            }
            return allChanges;
        }

        private void AutoTranslateBtn_Click(object sender, RoutedEventArgs e)
        {
            var win = new AutoTranslationWindow();
            win.ResXManager = _resourceManager;
            win.ShowDialog();
           
        }

        private void XlifBtn_Click(object sender, RoutedEventArgs e)
        {
            xlifMenu.IsOpen = true;
            if (exportXlifMenu.Items.Count != 0)
                return;

            foreach (var v in _resourceManager.Cultures)
            {
                if (v.IsNeutral) continue;
                var child = new MenuItem() { Header = v.Culture!.Name };
                child.Click += (_, _) => ExportXliff(v);
                exportXlifMenu.Items.Add(child);
            }
        }

        private void ExportXliff(CultureKey culturekey)
        {
            var file = new Xliff
            {
                File = new Model.XLif.File()
            };

            file.File.Original = _resourceViewModel.ResourceManager.SolutionFolder!;
            file.File.Datatype = "sln";
            file.File.Sourcelanguage = "en";
            file.File.Targetlanguage = culturekey.Culture!.Name;
            file.Version = "1.2";
            file.File.Body = new Body();

            XmlSerializer serializer = new(typeof(Xliff));
            using StringWriter ss = new();

            var projectResources = _resourceManager.TableEntries.GroupBy(x => x.Container.ProjectName);
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
                            Source = neutralValue!,
                            Target = new Target
                            {
                                State = status!,
                                Text = culturalValue!,
                            },
                            Note = r.Comments.GetValue(culturekey.Culture)!
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

        private void XlifImportBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog sfd = new()
            {
                Filter = "XLF file|*.xlf|XLIFF file|*.xliff"
            };

            if (sfd.ShowDialog().GetValueOrDefault())
            {
                ImportXlif(System.IO.File.ReadAllText(sfd.FileName));
            }
        }
        private void ImportXlif(string text)
        {
            XmlSerializer serializer = new(typeof(Xliff));
            using XmlReader reader = XmlReader.Create(new StringReader(text));
            var xliff = (Xliff)serializer.Deserialize(reader);

            //if (MessageBox.Show("Do you want to create a backup snapshot of current resources"
            //    , "Snapshot backup", MessageBoxButton.YesNo, MessageBoxImage.Hand) == MessageBoxResult.Yes)
            //{
            //    //TODO backup
            //}
            var cul = _resourceManager.Cultures;
            var curCul = cul.First(x => !x.IsNeutral && x.Culture?.Name == xliff.File.Targetlanguage);

            foreach (var project in xliff.File.Body.Group)
            {
                var projectRes = _resourceManager.TableEntries.Where(x => x.Container.ProjectName == project.Id);

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
            _resourceManager.Save();

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
