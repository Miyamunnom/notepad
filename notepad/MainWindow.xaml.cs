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
using System.Net.Http;
using Google.Cloud.Translate.V3;
using Google.Api.Gax.ResourceNames;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using System.Net;
using Newtonsoft.Json;
using System.Web;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Collections;

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

        private static readonly HttpClient client = new HttpClient();

        public MainWindow()
        {
            InitializeComponent();

            using (StreamReader r = new StreamReader("languages.json"))
            {
                var jsonData = new JavaScriptSerializer().Deserialize<List<dynamic>>(r.ReadToEnd());

                foreach (object item in jsonData)
                {
                    MenuItem menuItem = new MenuItem()
                    {
                        Header = item.ToString(),
                        Name = item.ToString(),
                    };

                    menuItem.Click += Menu_Click;

                    translator.Items.Add(menuItem);
                }
            }
        }

        public string TranslateText(string input, string fromLanguage, string toLanguage)
        {
            string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={fromLanguage}&tl={toLanguage}&dt=t&q={Uri.EscapeUriString(input)}";
            HttpClient httpClient = new HttpClient();
            string result = httpClient.GetStringAsync(url).Result;

            var jsonData = new JavaScriptSerializer().Deserialize<List<dynamic>>(result);

            var translationItems = jsonData[0];

            string translation = "";

            foreach (object item in translationItems)
            {
                IEnumerable translationLineObject = item as IEnumerable;

                IEnumerator translationLineString = translationLineObject.GetEnumerator();

                translationLineString.MoveNext();

                translation += $"{Convert.ToString(translationLineString.Current)}";
            }

            if (translation.Length > 1) { translation = translation.Substring(1); };

            return translation;
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
            {
                richTextBox.SelectAll();
                richTextBox.Selection.Text = "";
                String fullText = File.ReadAllText(openFileDialog.FileName);
                richTextBox.AppendText(fullText);

                translatedBox.Text += TranslateText(fullText, "ru", "en");
            }
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

        private bool showTranslator = false;
        private string toTranslate = "";

        private void translator_Click(object sender, RoutedEventArgs e)
        {
        }
        void Menu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem clickedLanguage = (MenuItem)sender;
            string lang = clickedLanguage.Header.ToString();

            if (showTranslator)
            {
                if (toTranslate != lang)
                {
                    toTranslate = lang;

                    translatedBox.Text = "";

                    translatedBox.Text += TranslateText(new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd).Text, "ru", lang);
                }
                else
                {
                    columnDef_1.Width = new GridLength(342, GridUnitType.Star);
                    columnDef_2.Width = new GridLength(0, GridUnitType.Star);
                    showTranslator = false;
                    translatedBox.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                columnDef_1.Width = new GridLength(171, GridUnitType.Star);
                columnDef_2.Width = new GridLength(171, GridUnitType.Star);
                showTranslator = true;
                translatedBox.Visibility = Visibility.Visible;

                if (toTranslate != lang)
                {
                    toTranslate = lang;

                    translatedBox.Text = "";

                    translatedBox.Text += TranslateText(new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd).Text, "ru", lang);
                }
            }
        }

        private bool showStatus = true;

        private void status_Click(object sender, RoutedEventArgs e)
        {
            if (showStatus)
            {
                showStatus = false;
                status_block.Visibility = Visibility.Collapsed;
            }
            else
            {
                showStatus = true;
                status_block.Visibility = Visibility.Visible;
            }
        }

        private void translator_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
        }

        private void richTextBox_MouseMove(object sender, MouseEventArgs e)
        {
            TextRange textRange = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
            string textBox = textRange.Text;

            int someBigNumber = int.MaxValue;
            int lineMoved, currentLineNumber;
            richTextBox.CaretPosition.GetLineStartPosition(-someBigNumber, out lineMoved);
            currentLineNumber = -lineMoved;

            TextPointer tp1 = richTextBox.Selection.Start.GetLineStartPosition(0);
            TextPointer tp2 = richTextBox.Selection.Start;

            int column = tp1.GetOffsetToPosition(tp2);

            long count = 0;
            int position = 0;
            while ((position = textBox.IndexOf('\n', position)) != -1)
            {
                count++;
                position++;
            }

            status_block.Text = $"Символы: {textBox.Length}, Строк: {count}, Строка: {currentLineNumber + 1}, Позиция: {column}";
        }

        private void richTextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            TextRange textRange = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
            string textBox = textRange.Text;

            int someBigNumber = int.MaxValue;
            int lineMoved, currentLineNumber;
            richTextBox.CaretPosition.GetLineStartPosition(-someBigNumber, out lineMoved);
            currentLineNumber = -lineMoved;

            TextPointer tp1 = richTextBox.Selection.Start.GetLineStartPosition(0);
            TextPointer tp2 = richTextBox.Selection.Start;

            int column = tp1.GetOffsetToPosition(tp2);

            long count = 0;
            int position = 0;
            while ((position = textBox.IndexOf('\n', position)) != -1)
            {
                count++;
                position++;
            }

            status_block.Text = $"Символы: {textBox.Length}, Строк: {count}, Строка: {currentLineNumber + 1}, Позиция: {column}";
        }
    }
}
