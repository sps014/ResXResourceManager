namespace ResXManager.View.Visuals
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Composition;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using DataGridExtensions;
    using DocumentFormat.OpenXml;
    using Microsoft.Win32;

    using ResXManager.Infrastructure;
    using ResXManager.Model;
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
            OpenFileDialog openFileDialog = new();
            openFileDialog.Title = "Import Snapshot file";
            openFileDialog.FileName = "Snapshot";
            openFileDialog.Filter = " Snapshot File | *.snapshot";
            if (!showDiff && openFileDialog.ShowDialog().GetValueOrDefault())
            {
                _resourceManager.LoadSnapshot(File.ReadAllText(openFileDialog.FileName));
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
                diff_label.Text = "Show Diff Only";
            }
            else
            {
                _resourceViewModel.ResourceTableEntries = _resourceManager.TableEntries;
                diff_label.Text = "Show All";

            }

            showDiff = !showDiff;
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

                        if (snap == null && value == null)
                        {
                            continue;
                        }

                        if ((snap == null && value != null) ||
                            (snap != null && value == null) ||
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
    }

}
