using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using ResXManager.Model;
using ResXManager.Scripting;
using ResXManager.View.Behaviors;
using TomsToolbox.Essentials;
using TomsToolbox.ObservableCollections;
using static ResXManager.View.CustomActions.ResXRootProjectHelper;

namespace ResXManager.View.Visuals
{
    /// <summary>
    /// Interaction logic for DiffViewer.xaml
    /// </summary>
    public partial class DiffViewer : Window
    {
        public DiffViewer()
        {
            InitializeComponent();
        }

        public Model.ResourceManager? ResourceManager { get; set; }
        public HashSet<ResourceTableEntry> Changes { get; set; }

        public void Initialize()
        {
            var hasRoot = HasResXManagerRoot(ResourceManager!.SolutionFolder!);

            string? file = null;
            if (hasRoot)
            {
                var folder = ResXManagerRootDir(ResourceManager!.SolutionFolder!);
                if (File.Exists($"{folder}\\Localization\\Localization_Full.snapshot"))
                {
                    file = $"{folder}\\Localization\\Localization_Full.snapshot";
                }

            }
            else
            {
                CreateResxManagerRootFile();
                Initialize();
                return;
            }

            OpenFileDialog openFileDialog = new()
            {
                Title = "Import Snapshot file",
                FileName = "Localization_Full",
                Filter = " Snapshot File | *.snapshot"
            };
            if (file == null)
            {
                if (openFileDialog.ShowDialog().GetValueOrDefault())
                    file = openFileDialog.FileName;
                else
                    return;
            }
            if (file is not null)
                ResourceManager.LoadSnapshot(File.ReadAllText(file));

            Changes = DiffHelper.CalcDiff(ResourceManager);

        }

        private void PopulateChangeGrid()
        {
            int id = 1;
            var changeData = Changes.Select(x => new ChangeRowData
            {
                ProjectName = x.Container.ProjectName,
                Resource = x.Container.BaseName,
                Key = x.Key,
                Neutral = x.Values.GetValue(null)!,
                OtherTranslation = ResourceManager!.Cultures.Where(y => y.Culture != null && !y.IsNeutral)
                .Select(y => x.Values.GetValue(y.Culture))
                .ToArray(),
                Index = id++
            }).ToList();
            //add columns to datagrid  Project Name/Resource   Neutral text

            changeGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Number",
                Binding = new Binding("Index")
            });
            changeGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Project Name",
                Binding = new Binding("ProjectName")
            });

            changeGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Resource",
                Binding = new Binding("Resource")
            });

            changeGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Key",
                Binding = new Binding("Key")
            });

            changeGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Neutral Text",
                Binding = new Binding("Neutral")
            });

            //add columns for each language

            int idx = 0;
            foreach(var lang in ResourceManager!.Cultures)
            {
                if (lang.Culture == null || lang.IsNeutral)
                    continue;

                changeGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = lang.Culture.EnglishName,
                    Binding = new Binding("OtherTranslation[" + (idx++) + "]")
                });
            }

            //add rows to datagrid
            foreach (var change in changeData)
            {
                changeGrid.Items.Add(change);
            }

            //disabe editing
            changeGrid.IsReadOnly = true;

            //make neutral and other translation columns wider and give margin to all columns
            for(int i = 0; i < changeGrid.Columns.Count; i++)
            {
                if (i == 4 || i > 5)
                    changeGrid.Columns[i].Width = new DataGridLength(1, DataGridLengthUnitType.Star);
                else
                    changeGrid.Columns[i].Width = new DataGridLength(1, DataGridLengthUnitType.Auto);

                //set margin of 10 px to all columns

                changeGrid.Columns[i].CellStyle = new Style(typeof(DataGridCell))
                {
                    Setters =
                    {
                        new Setter(Control.MarginProperty, new Thickness(10,0,10,0))
                    }
                };
                
            }

            //allow cell selection
            changeGrid.SelectionUnit = DataGridSelectionUnit.Cell;
            changeGrid.SelectedCellsChanged += ChangeGrid_CurrentCellChanged;

        }

        private void ChangeGrid_CurrentCellChanged(object? sender, EventArgs e)
        {
            //get the current cell return if null
            var cell = changeGrid.CurrentCell;
            if (cell == null)
                return;

            //get the text of the cell as a string
            dynamic cellContent = cell.Column.GetCellContent(cell.Item);
            if (cellContent == null)
                return;
            var text = cellContent.Text;

            if(text == null)
                return;

            cellTextBox.Text = text;
            cellTextBox.ToolTip = text;        
        }

        public class  ChangeRowData
        {
            public string ProjectName { get; set; }
            public string Resource { get; set; }
            public string Key { get; set; }
            public string Neutral { get; set; }
            public string[] OtherTranslation { get; set; }
            public int Index { get; set; }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            PopulateChangeGrid();
        }

        private void exportDiff(object sender, RoutedEventArgs e)
        {
            //open save file dialog
            SaveFileDialog saveFileDialog = new()
            {
                Title = "Export Diff",
                FileName = "Diff",
                Filter = " Excel File | *.xlsx"
            };

            if (saveFileDialog.ShowDialog().GetValueOrDefault())
            {
                //create excel file
                using SwiftExcel.ExcelWriter writer = new(saveFileDialog.FileName);

                //write header Project File Key Neutral OtherTranslations as columns
                writer.Write("Project", 1,1);
                writer.Write("File", 2,1);
                writer.Write("Key", 3,1);
                writer.Write("", 4,1);
                writer.Write("Comment", 5,1);
                int column = 6;
                foreach(var lang in ResourceManager!.Cultures)
                {
                    if (lang.Culture == null || lang.IsNeutral)
                        continue;

                    writer.Write(lang.Culture.Name, column++,1);
                    writer.Write($"Comment.{lang.Culture.Name}", column++,1);
                }

                //write data
                int row = 2;
                foreach(var change in Changes)
                {
                    writer.Write(change.Container.ProjectName,1,row);
                    writer.Write(change.Container.BaseName,2, row);
                    writer.Write(change.Key,3, row);
                    writer.Write(change.Values.GetValue(null),4, row);
                    writer.Write(change.Comments.GetValue(null),5, row);

                    column = 6;
                    foreach (var lang in ResourceManager!.Cultures)
                    {
                        if (lang.Culture == null || lang.IsNeutral)
                            continue;

                        writer.Write(change.Values.GetValue(lang.Culture), column++,row);
                        writer.Write(change.Comments.GetValue(lang.Culture),column++,row);
                    }

                    row++;
                }
            }
        }
    }
}
