using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace notepad
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string startTitle = "Блокнот";
        private bool changed;
        program programInfo;

        public MainWindow()
        {
            InitializeComponent();
        }
        public static readonly RoutedCommand Open = new RoutedCommand();
        private void openBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            openFile.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        }

        public static readonly RoutedCommand Save = new RoutedCommand();
        private void saveBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            saveFile.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        }

        private void program_Click(object sender, RoutedEventArgs e)
        {
            programInfo = new program();
            programInfo.Show();
        }

        private void openFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.DefaultExt = ".txt";
            openFileDialog.Filter = "TXT Files (*.txt)|*.txt";

            if (openFileDialog.ShowDialog() == true)
                richTextBox.AppendText(File.ReadAllText(openFileDialog.FileName));
        }

        private void saveFile_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();

            saveFileDialog.DefaultExt = ".txt";
            saveFileDialog.Filter = "TXT Files (*.txt)|*.txt";

            if (saveFileDialog.ShowDialog() == true)
            {
                string richText = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd).Text;
                File.WriteAllText(saveFileDialog.FileName, richText);
            }
        }

        private void richTextBox_Initialized(object sender, EventArgs e)
        {
            this.startTitle = this.Title;
            this.Title = this.Title;
        }

        private void richTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            this.Title = this.startTitle + " *";
            this.changed = true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            closed onenClosed = new closed();

            if (onenClosed.ShowDialog() == true)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();

                saveFileDialog.DefaultExt = ".txt";
                saveFileDialog.Filter = "TXT Files (*.txt)|*.txt";

                if (saveFileDialog.ShowDialog() == true)
                {
                    string richText = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd).Text;
                    File.WriteAllText(saveFileDialog.FileName, richText);
                }
            }
        }
    }
}
