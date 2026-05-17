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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Contracts.UserControls
{
    /// <summary>
    /// Interaction logic for LabelProgressBar.xaml
    /// </summary>
    public partial class LabelProgressBar : UserControl
    {
        public string ProgessString
        {
            get { return (string)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }

        private static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register("ProgessString", typeof(string), typeof(LabelProgressBar), new PropertyMetadata(string.Empty));

        public int ProgressValue
        {
            get { return (int)GetValue(ProgressValueProperty); }
            set { SetValue(ProgressValueProperty, value); }
        }

        private static readonly DependencyProperty ProgressValueProperty =
            DependencyProperty.Register("ProgressValue", typeof(int), typeof(LabelProgressBar), new PropertyMetadata(0));

        public LabelProgressBar()
        {
            InitializeComponent();
        }
    }
}
