namespace ResXManager.View.Visuals
{
    using System;
    using System.Collections.Generic;
    using System.Composition;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Xml;
    using System.Xml.Serialization;
    using DataGridExtensions;
    using DocumentFormat.OpenXml;
    using Microsoft.Win32;
    using ResX.Scripting;
    using ResXManager.Infrastructure;
    using ResXManager.Model;
    using ResXManager.Model.XLif;
    using ResXManager.View.Tools;

    using TomsToolbox.Composition;
    using TomsToolbox.Essentials;
    using TomsToolbox.ObservableCollections;
    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition;
    using TomsToolbox.Wpf.Composition.AttributedModel;
    using TomsToolbox.Wpf.Converters;

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
            }
            catch (Exception ex)
            {
                exportProvider.TraceXamlLoaderError(ex);
            }
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
            OpenFileDialog openFileDialog = new()
            {
                Title = "Import Snapshot file",
                FileName = "Snapshot",
                Filter = " Snapshot File | *.snapshot"
            };
            if (!showDiff && openFileDialog.ShowDialog().GetValueOrDefault())
            {
                _resourceManager.LoadSnapshot(System.IO.File.ReadAllText(openFileDialog.FileName));
            }
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
            Mouse.OverrideCursor = Cursors.Wait;
            AutoTranslation.Start(_resourceViewModel.ResourceManager);
            Mouse.OverrideCursor = Cursors.Arrow;
        }

        private void XlifBtn_Click(object sender, RoutedEventArgs e)
        {
            xlifMenu.IsOpen = true;
            if (exportXlifMenu.Items.Count != 0)
                return;

            foreach (var v in _resourceManager.Cultures)
            {
                if (v.IsNeutral) continue;
                var child = new MenuItem() { Header = v.Culture.Name };
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

            file.File.Original = _resourceViewModel.ResourceManager.SolutionFolder;
            file.File.Datatype = "sln";
            file.File.Sourcelanguage = "en";
            file.File.Targetlanguage = culturekey.Culture.Name;
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
                            Source = neutralValue,
                            Target = new Target
                            {
                                State = "unknown",
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
                Filter = "XLIF files|*.xlf"
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
                Filter = "XLIF files|*.xlf"
            };

            if (sfd.ShowDialog().GetValueOrDefault())
            {
                ImportXlif(System.IO.File.ReadAllText(sfd.FileName));
            }
        }
        private void ImportXlif(string text)
        {
            var parts = text.Split('\n');
            parts[1] = "<xliff>";
            text = string.Join("\n", parts);
            XmlSerializer serializer = new(typeof(Xliff));
            using XmlReader reader = XmlReader.Create(new StringReader(text));
            var xliff = (Xliff)serializer.Deserialize(reader);
            var projName = xliff.File.Original.Replace(".csproj", string.Empty);
            var resources = _resourceManager.TableEntries
                .Where(y => y.Container.ProjectName == projName);

            if (resources == null)
                return;

            foreach (var gp in xliff.File.Body.Group)
            {
                foreach (var tunit in gp.Transunits)
                {
                    var r = resources.First(x => x.Key == tunit.Id);
                    r.Values.SetValue(xliff.File.Targetlanguage, tunit.Target.Text);
                    r.Comments.SetValue(xliff.File.Targetlanguage, tunit.Target.State);
                }
            }
            _resourceManager.Save();

        }
    }

}
