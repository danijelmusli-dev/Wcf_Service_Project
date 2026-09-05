using System.Windows;
using System.Windows.Controls;

namespace Contracts.UserControls
{
    public partial class LabelProgressBar : UserControl
    {
        public static readonly DependencyProperty ProgressStringProperty =
            DependencyProperty.Register("ProgressString", typeof(string), typeof(LabelProgressBar), new PropertyMetadata(string.Empty));

        public string ProgressString
        {
            get => (string)GetValue(ProgressStringProperty);
            set => SetValue(ProgressStringProperty, value);
        }

        public static readonly DependencyProperty ProgressValueProperty =
            DependencyProperty.Register("ProgressValue", typeof(int), typeof(LabelProgressBar), new PropertyMetadata(0));

        public int ProgressValue
        {
            get => (int)GetValue(ProgressValueProperty);
            set => SetValue(ProgressValueProperty, value);
        }

        public LabelProgressBar()
        {
            InitializeComponent();
        }
    }
}
