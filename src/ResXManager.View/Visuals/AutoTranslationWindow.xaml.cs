using ResX.Scripting;
using ResXManager.Model;
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
    /// Interaction logic for AutoTranslationWindow.xaml
    /// </summary>
    public partial class AutoTranslationWindow : Window
    {
        public AutoTranslationWindow()
        {
            InitializeComponent();
            AutoTranslation.OnProgress += AutoTranslation_OnProgress;
            AutoTranslation.OnTranslationAction += AutoTranslation_OnTranslationAction;
            selectPanel.Visibility = Visibility.Collapsed;

        }

        private DecisionResult decisionDone;

        private async Task<RequireActionResult> AutoTranslation_OnTranslationAction(RequireActionEventArg e)
        {
            selectPanel.Visibility = Visibility.Visible;
            decisionDone = DecisionResult.Pending;
            choiceGrid.Visibility = Visibility.Visible;
            choiceGrid.Items.Clear();
            pNameLabel.Content = e.ProjectName;
            rNameLabel.Content = e.UniqueName;
            kNameLabel.Content = e.Key;
            cNameLabel.Content = e.Culture;
            nameLabel.Content = e.NeutralText;
            foreach (var c in e.Values)
            {
                var stackPnl = new StackPanel()
                {
                    Orientation = Orientation.Horizontal
                };
                stackPnl.Children.Add(new Label() { Content = c.CultureValues[e.Culture] });
                stackPnl.Children.Add(new Label() { Content = c.ProjectName,Margin=new Thickness(30,0,0,0) });
                stackPnl.Children.Add(new Label() { Content = c.UniqueName });

                choiceGrid.Items.Add(stackPnl);
            }

            while (decisionDone == DecisionResult.Pending)
            {
                await Task.Delay(100);
            }

            selectPanel.Visibility = Visibility.Collapsed;
            return new RequireActionResult()
            {
                Merge = decisionDone == DecisionResult.Accepted,
                Index = choiceGrid.SelectedIndex
            };

        }


        ~AutoTranslationWindow()
        {
            AutoTranslation.OnProgress -= AutoTranslation_OnProgress;
        }

        private void AutoTranslation_OnProgress(ProgressEventArg e)
        {
            if (e.EventType == EventType.CacheBuildUp)
            {
                //progressBar.Value = e.CachedItemCount;
            }
        }

        public ResourceManager ResXManager { get; internal set; }

        private void startBtn_Click(object sender, RoutedEventArgs e)
        {
            AutoTranslation.Start(ResXManager);
            //progressBar.Visibility = Visibility.Visible;
            //progressBar.Maximum = ResXManager.TableEntries.Count;
            statusText.Foreground = new SolidColorBrush(Colors.Black);
            statusText.Content = $"Building up cache of size {ResXManager.TableEntries.Count}";
        }

        private void SelectBtn_Click(object sender, RoutedEventArgs e)
        {
            if (choiceGrid.SelectedIndex < 0)
            {
                MessageBox.Show("Please select a valid item from pre source translations");
                return;
            }
            decisionDone = DecisionResult.Accepted;
        }

        private void DiscardBtn_Click(object sender, RoutedEventArgs e)
        {
            decisionDone = DecisionResult.Discarded;
        }
        public enum DecisionResult
        {
            Pending,
            Accepted,
            Discarded
        }
    }
}
