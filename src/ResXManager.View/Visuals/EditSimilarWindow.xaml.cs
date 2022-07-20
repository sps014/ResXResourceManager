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
    }
}
