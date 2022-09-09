using ResX.Scripting;
using ResXManager.Model;
using ResXManager.View.Behaviors;
using System;
using System.Collections.Generic;
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
using ResourceManager = ResXManager.Model.ResourceManager;

namespace ResXManager.View.Visuals
{
    /// <summary>
    /// Interaction logic for EditSimilarWindow.xaml
    /// </summary>
    public partial class EditSimilarWindow : Window
    {
        public EditSimilarWindow()
        {
            InitializeComponent();
        }

        public ResourceManager ResourceManager { get; internal set; }
        public HashSet<TranslateContainerModel> Cache { get; internal set; }
        public CustomEditCommitArgs CustomEditEventArgs { get; internal set; }
        private TargetItem[] targetItems;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!Cache.Any())
                return;

            targetItems = new TargetItem[Cache.Count];

            if (!Title.Contains(CustomEditEventArgs.PreviousValue))
                Title = $"Edit From `{CustomEditEventArgs.PreviousValue}` To `{CustomEditEventArgs.CurrentValue}`";

            ItemsList.Items.Clear();
            int i = 0;
            foreach (var item in Cache)
            {
                var ec = CustomEditEventArgs;

                var itemz = new TargetItem
                {
                    Key = item.Key,
                    Modify = false,
                    ProjectName = item.ProjectName,
                    ResourceName = item.UniqueName,
                    PreviousValue = ec.PreviousValue
                };

                targetItems[i] = itemz;

                ItemsList.Items.Add(itemz);
                i++;
            }
        }

        
        private void ConfirmBtn_Click(object sender, RoutedEventArgs e)
        {
            var cachedItems = Cache.ToList();

            for (int i = 0; i < targetItems.Length; i++)
            {
                if (!targetItems[i].Modify)
                    continue;

                cachedItems[i].Entry.Values
                    .SetValue(CustomEditEventArgs.Culture, CustomEditEventArgs.CurrentValue);
            }
            ResourceManager.Save();

            Close();
        }

        private void SelectAllBtn_Click(object sender, RoutedEventArgs e)
        {
            ItemsList.Items.Clear();
            foreach (var item in targetItems)
            {
                item.Modify = true;
                ItemsList.Items.Add(item);
            }
        }
        private class TargetItem
        {
            public bool Modify { get; set; }
            public string Key { get; set; }
            public string ResourceName { get; set; }
            public string ProjectName { get; set; }
            public string PreviousValue { get; internal set; }
        }

        private void ItemsList_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ItemsList.SelectedIndex < 0)
                return;

            ItemsList.Items.Clear();
            TargetItem item = targetItems[ItemsList.SelectedIndex];
            item.Modify = !item.Modify;

            foreach (var v in targetItems)
                ItemsList.Items.Add(v);
        }
    }
}
