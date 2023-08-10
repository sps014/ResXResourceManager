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
    using static ResXManager.View.CustomActions.ResXRootProjectHelper;
    using TomsToolbox.Composition;
    using TomsToolbox.Essentials;
    using TomsToolbox.ObservableCollections;
    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition;
    using TomsToolbox.Wpf.Composition.AttributedModel;
    using TomsToolbox.Wpf.Converters;
    using File = System.IO.File;
    using Group = Model.XLif.Group;
    using ResXManager.View.CustomActions;
    using ResXManager.Scripting;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using System.Diagnostics;

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
            var dataGridName = (sender as DataGrid).Name;
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
                e.Entry.Container.NeutralProjectFile.FilePath, e.Entry.Key);

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
            window.ResourceManager = ScriptHost.ResourceManager;
            window.Cache = cache;
            window.CustomEditEventArgs = e;
            window.ShowDialog();
        }

        private HashSet<TranslateContainerModel> BuildCache(string text, string culture, string filePath, string k)
        {
            var result = new HashSet<TranslateContainerModel>();

            foreach (var e in ScriptHost.ResourceManager.TableEntries)
            {
                var value = e.Values.GetValue(culture);
                if (text != value)
                    continue;

                if (filePath == e.Container.NeutralProjectFile.FilePath && e.Key == k)
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

        public static Host ScriptHost { get; private set; }

        private async void ResourceManager_Loaded(object? sender, EventArgs e)
        {
            DataGrid?.SetupColumns(_resourceManager, _resourceViewModel, _configuration);

            //Now Load shadow Resource Manager if it is required


            if (!ResourceViewModel.IsLoadedFromCLI)
            {
                _resourceViewModel.ShadowResourceManager = _resourceManager;
                ScriptHost = new Host();
                ScriptHost.ResourceManager = _resourceManager;
                return;
            }


            if (ScriptHost is not null)
                ScriptHost.Dispose();

            DataGrid.IsReadOnly = true;
            AutoTranslateBtn.Visibility = Visibility.Collapsed;
            ScriptHost = new Host();
            var dirPath = Environment.GetCommandLineArgs()[1];
            if (!HasResXManagerRoot(Path.GetDirectoryName(dirPath)))
            {
                if (!CreateResxManagerRootFile())
                {
                    _resourceViewModel.ShadowResourceManager = _resourceViewModel.ResourceManager;
                    return;
                }
            }

            ScriptHost.ResourceManager.Loaded += (_, e) =>
            {
                //enable editing of values now
                //_resourceViewModel.ResourceManager.TableEntries = ScriptHost.ResourceManager.TableEntries;
                //_resourceViewModel.ResourceManager.ResourceEntities = ScriptHost.ResourceManager.ResourceEntities;

                Dispatcher.Invoke(() =>
                {
                    DataGrid.IsReadOnly = false;
                    Cursor = Cursors.Arrow;
                    openAppBtn.Visibility = Visibility.Visible;
                    AutoTranslateBtn.Visibility = Visibility.Visible;
                });

            };

            Cursor = Cursors.Wait;
            await Task.Run(() =>
            {
                ScriptHost.Load(ResXManagerRootDir(dirPath));
            });


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
            DiffViewer viewer = new DiffViewer();
            viewer.ResourceManager = ScriptHost.ResourceManager;
            viewer.Initialize();
            viewer.ShowDialog();

        }

        private void AutoTranslateBtn_Click(object sender, RoutedEventArgs e)
        {
            var win = new AutoTranslationWindow();
            win.ResXManager = ScriptHost.ResourceManager;
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
                var child = new MenuItem() { Header = v.Culture.Name };
                child.Click += (_, _) => XLiffImporterExporter.ExportXliff(_resourceViewModel,v);
                exportXlifMenu.Items.Add(child);
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
                XLiffImporterExporter.ImportXliff(_resourceViewModel,System.IO.File.ReadAllText(sfd.FileName));
            }
        }     

        private void openAppBtn_Click(object sender, RoutedEventArgs e)
        {
            Process
                .Start(Application.ResourceAssembly.Location
                ,$"\"{Path.GetDirectoryName(Environment.GetCommandLineArgs()[1])}\"")
                ;
            Application.Current.Shutdown();
        }

        private void includeInProject_Click(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;
            IncludeAllResxFiles.Build(_resourceManager.SolutionFolder!);
            Cursor = Cursors.Arrow;
        }
    }

}
