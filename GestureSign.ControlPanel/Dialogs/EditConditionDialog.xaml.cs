using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using MahApps.Metro.Controls;

namespace GestureSign.ControlPanel.Dialogs
{
    /// <summary>
    /// Interaction logic for EditConditionDialog.xaml
    /// </summary>
    public partial class EditConditionDialog : MetroWindow
    {
        private string _keyConditionVariable;

        public EditConditionDialog(string condition)
        {
            DataContext = condition;

            InitializeComponent();
        }

        private void InsertButton_Click(object sender, RoutedEventArgs e)
        {
            InsertConditionText($"finger_{FingerIDComboBox.Text}_{VariableComboBox.Text}");
        }

        private void WindowVariableButton_Click(object sender, RoutedEventArgs e)
        {
            VariableButton_Click(sender, e);
        }

        private void VariableButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            InsertConditionText(button?.Tag as string);
        }

        private void KeyConditionTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            if (key == Key.None)
                return;

            int virtualKey = KeyInterop.VirtualKeyFromKey(key);
            _keyConditionVariable = $"key_{((System.Windows.Forms.Keys)virtualKey).ToString().ToLowerInvariant()}_down";
            KeyConditionTextBox.Text = _keyConditionVariable;
            e.Handled = true;
        }

        private void InsertKeyButton_Click(object sender, RoutedEventArgs e)
        {
            InsertConditionText(_keyConditionVariable);
        }

        private void InsertConditionText(string variable)
        {
            if (string.IsNullOrWhiteSpace(variable))
                return;

            int caretIndex = ConditionTextBox.CaretIndex;
            ConditionTextBox.Text = ConditionTextBox.Text.Insert(caretIndex, variable);
            ConditionTextBox.CaretIndex = caretIndex + variable.Length;

            ConditionTextBox.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void ConditionTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            ConditionTextBox.Text = DataContext as string;
            ConditionTextBox.Focus();
        }

        private void VariableComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            VariableComboBox.Items.Add("start_X");
            VariableComboBox.Items.Add("start_X%");
            VariableComboBox.Items.Add("start_Y");
            VariableComboBox.Items.Add("start_Y%");
            VariableComboBox.Items.Add("end_X");
            VariableComboBox.Items.Add("end_X%");
            VariableComboBox.Items.Add("end_Y");
            VariableComboBox.Items.Add("end_Y%");
            VariableComboBox.Items.Add("ID");

            VariableComboBox.SelectedIndex = 0;
        }

        private void FingerIDComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            for (int i = 1; i <= 10; i++)
            {
                FingerIDComboBox.Items.Add(i);
            }
            FingerIDComboBox.SelectedIndex = 0;
        }
    }
}
