using ResX.Scripting;
using ResXManager.Model;
using ResXManager.View.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public CustomEditCommitArgs CustomEditEventArgs { get;internal set; }
        private CheckBox[] CheckBoxes;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!Cache.Any())
                return;

            CheckBoxes = new CheckBox[Cache.Count];

            if (!Title.Contains(CustomEditEventArgs.PreviousValue))
                Title = $"Edit From `{CustomEditEventArgs.PreviousValue}` To `{CustomEditEventArgs.CurrentValue}`";

            ItemsList.Items.Clear();
            int i = 0;
            foreach (var item in Cache)
            {
                var stackPanel = new StackPanel()
                {
                    Orientation = Orientation.Horizontal
                };
                var checkBox = new CheckBox() { Margin = new Thickness(10, 0, 0, 0) };

                CheckBoxes[i] = checkBox;

                var label = new TextBlock
                {
                    Text = $"\"{CustomEditEventArgs.PreviousValue}\" --> \"{item.UniqueName}\" resource  --> \"{item.ProjectName}\" project"
                };

                stackPanel.Children.Add(checkBox);
                stackPanel.Children.Add(label);
                ItemsList.Items.Add(stackPanel);
                i++;
            }
        }

        private void ConfirmBtn_Click(object sender, RoutedEventArgs e)
        {
            var cachedItems = Cache.ToList();

            for (int i = 0; i < CheckBoxes.Length; i++)
            {
                if (!CheckBoxes[i].IsChecked.Value)
                    continue;
                cachedItems[i].Entry.Values.SetValue(CustomEditEventArgs.Culture, CustomEditEventArgs.CurrentValue);
            }
            
            ResourceManager.Save();

            Close();
        }

        private void SelectAllBtn_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in CheckBoxes)
            {
                item.IsChecked = true;
            }
        }
    }
}
