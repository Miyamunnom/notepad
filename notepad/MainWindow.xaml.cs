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
using MindFusion.Charting.Wpf;
using System.Collections.Specialized;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;
using System.Diagnostics;
using MySql.Data.MySqlClient;
using System.Data.Common;

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
        public CustomStyle customStyle { get; set; }

        private static readonly HttpClient client = new HttpClient();

        public MainWindow()
        {
            InitializeComponent();

            MySqlConnection connection = DBUtils.GetDBConnection();
            connection.Open();

            try
            {
                string sql = "SELECT * FROM languages";

                MySqlCommand sqlCommand = new MySqlCommand();

                sqlCommand.Connection = connection;
                sqlCommand.CommandText = sql;

                using (DbDataReader reader = sqlCommand.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            string code = reader.GetString(reader.GetOrdinal("code"));

                            MenuItem menuItem = new MenuItem()
                            {
                                Header = code,
                                Name = code,
                            };

                            menuItem.Click += Menu_Click;

                            translator.Items.Add(menuItem);
                        }
                    }
                }

                sqlCommand.CommandText = "SELECT * FROM `encodes`";

                using (DbDataReader reader = sqlCommand.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            string code = reader.GetString(reader.GetOrdinal("code"));

                            MenuItem menuItem = new MenuItem()
                            {
                                Header = code
                            };

                            menuItem.Click += Encode_Click;

                            encodes.Items.Add(menuItem);
                        }
                    }
                }

                sqlCommand.CommandText = "SELECT * FROM `lastfiles` ORDER BY id DESC";

                int addedFiles = 0;

                using (DbDataReader reader = sqlCommand.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            if (addedFiles >= 5)
                                break;

                            string path = reader.GetString(reader.GetOrdinal("path"));

                            MenuItem menuItem = new MenuItem()
                            {
                                Header = path
                            };

                            lastFiles.Items.Add(menuItem);

                            ++addedFiles;
                        }
                    }
                }

                for (int i = addedFiles; i < 5; ++i)
                {
                    lastFiles.Items.Add("Пусто");
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                //Закрытие соединения с базой данных и удаление переменной из памяти (dispose)
                connection.Close();
                connection.Dispose();
            }

            lineChart.XAxes.CollectionChanged += new NotifyCollectionChangedEventHandler(OnAxesCollectionChanged);
            lineChart.YAxes.CollectionChanged += new NotifyCollectionChangedEventHandler(OnAxesCollectionChanged);
            lineChart.Y2Axes.CollectionChanged += new NotifyCollectionChangedEventHandler(OnAxesCollectionChanged);

            InitializeChart();

            RunPeriodicSave();
        }

        private int minutesPassed = 0;

        async Task RunPeriodicSave()
        {
            double[] cpuMassive = new double[10];
            double[] ramMassive = new double[10];
            double[] ethernetMassive = new double[10];

            while (true)
            {
                await Task.Delay(1000);

                var process = Process.GetCurrentProcess();

                var name = string.Empty;

                foreach (var instance in new PerformanceCounterCategory("Process").GetInstanceNames())
                {
                    if (instance.StartsWith(process.ProcessName))
                    {
                        using (var processId = new PerformanceCounter("Process", "ID Process", instance, true))
                        {
                            if (process.Id == (int)processId.RawValue)
                            {
                                name = instance;
                                break;
                            }
                        }
                    }
                }

                double full = 512;

                var cpu = new PerformanceCounter("Process", "% Processor Time", name, true);
                var ram = new PerformanceCounter("Process", "Private Bytes", name, true);

                var performanceCounterReceived = new PerformanceCounter("Network Interface", "Bytes Received/sec", new PerformanceCounterCategory("Network Interface").GetInstanceNames()[0]);

                double usedMemory = ram.NextValue() / 1024 / 1024;
                double receivedEthernet = performanceCounterReceived.NextValue() / 1024;

                if (minutesPassed < 10)
                {
                    cpuMassive[minutesPassed] = cpu.NextValue();

                    ramMassive[minutesPassed] = usedMemory / (full / 100);

                    ethernetMassive[minutesPassed] = receivedEthernet / (full / 100);

                    Red.YData = new DoubleCollection(cpuMassive);
                    Yellow.YData = new DoubleCollection(ramMassive);
                    Green.YData = new DoubleCollection(ethernetMassive);

                    ++minutesPassed;
                }
                else
                {
                    minutesPassed = 0;
                }
            }
        }

        private void InitializeChart()
        {
            lineChart.XAxes.Clear();
            lineChart.YAxes.Clear();
            lineChart.Y2Axes.Clear();

            lineChart.XAxes.Add(CreateAxis(lineChart.XAxes));
            lineChart.YAxes.Add(CreateAxis(lineChart.YAxes));
            lineChart.Y2Axes.Add(CreateAxis(lineChart.Y2Axes));
        }

        private void OnAxesCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
        }

        private double MinX(int index)
        {
            return 0 + index * 1;
        }

        private double MaxX(int index)
        {
            return 9 + index * 1;
        }

        private double MinY(int index)
        {
            return 0 + index * 10;
        }

        private double MaxY(int index)
        {
            return 100 + index * 10;
        }

        private double MaxYRam(int index)
        {
            return 512 + index * 64;
        }

        private Axis CreateAxis(AxesCollection collection)
        {
            Axis axis = new Axis();
            int index = collection.Count;

            if (collection == lineChart.XAxes)
            {
                double min = MinX(index);
                double max = MaxX(index);

                axis.MinValue = Math.Round(min);
                axis.MaxValue = Math.Round(max);
                axis.Title = "Минуты";
                axis.Interval = 1;
                axis.Tick = 1;
                axis.Tag = "Минуты";
                axis.IntervalCount = 9;
            }
            else if(collection == lineChart.YAxes)
            {
                double min = MinY(index);
                double max = MaxY(index);

                axis.MinValue = min;
                axis.MaxValue = max;
                axis.Title =  "CPU %";
                axis.TitleRotationAngle = 270;
                axis.Interval = 10;
                axis.Tick = 3;
                axis.Tag = "Озу/интернет";
                axis.IntervalCount = 10;
            } else if (collection == lineChart.Y2Axes)
            {
                double min = MinY(index);
                double max = MaxYRam(index);

                axis.MinValue = min;
                axis.MaxValue = max;
                axis.Title = "Озу/интернет";
                axis.TitleRotationAngle = 90;
                axis.Interval = 10;
                axis.Tick = 3;
                axis.Tag = "Озу/интернет";
                axis.IntervalCount = 8;
            }

            axis.LabelType = LabelType.AutoScale;
            return axis;
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

        private void help_Clicked(object sender, RoutedEventArgs e)
        {
            new Help().Show();
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

                MySqlConnection connection = DBUtils.GetDBConnection();
                connection.Open();

                try
                {
                    string sql = $"INSERT INTO `lastfiles`(`path`) VALUES('{openFileDialog.FileName}')";

                    MySqlCommand sqlCommand = new MySqlCommand();

                    sqlCommand.Connection = connection;
                    sqlCommand.CommandText = sql;

                    sqlCommand.ExecuteNonQuery();

                    bool foundedEmpty = false;

                    for (int i = 0; i < 5; ++i)
                    {
                        if (lastFiles.Items[i].ToString().Equals("Пусто"))
                        {
                            lastFiles.Items[i] = openFileDialog.FileName;
                            foundedEmpty = true;
                            break;
                        }
                    }

                    if (!foundedEmpty)
                    {
                        for (int i = 0; i < 5; ++i)
                        {
                            if (!lastFiles.Items[i].ToString().Equals("Пусто"))
                            {
                                lastFiles.Items[i] = openFileDialog.FileName;
                                foundedEmpty = true;
                                break;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    //Закрытие соединения с базой данных и удаление переменной из памяти (dispose)
                    connection.Close();
                    connection.Dispose();
                }
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

        private Encoding previewEncoding = Encoding.Default;
        private Encoding encodingCurrent;

        void Encode_Click(object sender, RoutedEventArgs e)
        {
            MenuItem clickedEncode = (MenuItem)sender;

            if (encodingCurrent != null)
                previewEncoding = encodingCurrent;

            encodingCurrent = Encoding.GetEncoding(clickedEncode.Header.ToString());

            string fullText = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd).Text;

            byte[] utfArr = previewEncoding.GetBytes(fullText);
            byte[] winArr = Encoding.Convert(encodingCurrent, previewEncoding, utfArr);

            richTextBox.Document.Blocks.Clear();

            richTextBox.AppendText(encodingCurrent.GetString(winArr));
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

        private void stylesMenu_Click(object sender, RoutedEventArgs e)
        {
            new Window1(this).Show();
        }

        public void renderStyle(CustomStyle customStyle)
        {
            this.customStyle = customStyle;

            foreach(CustomStyleElement customStyleElement in customStyle.elements)
            {
                switch (customStyleElement.name)
                {
                    case "richtextbox":
                        {
                            Thickness thickness = customStyleElement.thickness;

                            if (thickness.Left != -1 && thickness.Right != -1 && thickness.Top != -1 && thickness.Bottom != -1)
                            {
                                richTextBox.BorderThickness = thickness;
                            }
                            else
                                richTextBox.BorderThickness = new Thickness(1, 1, 1, 1);

                            if (customStyleElement.backgroundColor != null)
                            {
                                SolidColorBrush backgroundColorStyle = (SolidColorBrush)customStyleElement.backgroundColor;

                                richTextBox.Background = backgroundColorStyle;
                            }
                            else
                                richTextBox.Background = null;

                            if (customStyleElement.textColor != null)
                            {
                                SolidColorBrush textColorStyle = (SolidColorBrush)customStyleElement.textColor;

                                richTextBox.Foreground = customStyleElement.textColor;
                            }
                            else
                                richTextBox.Foreground = null;

                            if (customStyleElement.font != null)
                                richTextBox.FontFamily = customStyleElement.font;
                            else
                                richTextBox.FontFamily = new FontFamily("Segoe UI");
                            if (customStyleElement.fontSize >= 1)
                                richTextBox.FontSize = customStyleElement.fontSize;
                            else
                                richTextBox.FontSize = 12;

                            break;
                        }
                    case "textblock":
                        {
                            if (customStyleElement.backgroundColor != null)
                            {
                                SolidColorBrush backgroundColorStyle = (SolidColorBrush)customStyleElement.backgroundColor;

                                translatedBox.Background = backgroundColorStyle;
                            }
                            else
                                translatedBox.Background = null;

                            if (customStyleElement.textColor != null)
                            {
                                SolidColorBrush textColorStyle = (SolidColorBrush)customStyleElement.textColor;

                                translatedBox.Foreground = textColorStyle;
                            }
                            else
                                translatedBox.Foreground = null;

                            if (customStyleElement.font != null)
                                translatedBox.FontFamily = customStyleElement.font;
                            else
                                translatedBox.FontFamily = new FontFamily("Segoe UI");
                            if (customStyleElement.fontSize >= 1)
                                translatedBox.FontSize = customStyleElement.fontSize;
                            else
                                translatedBox.FontSize = 12;

                            break;
                        }
                    case "window":
                        {
                            if (customStyleElement.backgroundColor != null)
                            {
                                SolidColorBrush backgroundColorStyle = (SolidColorBrush)customStyleElement.backgroundColor;

                                this.Background = backgroundColorStyle;
                            }
                            else
                                this.Background = Brushes.White;

                            break;
                        }
                    case "menu":
                        {
                            if (customStyleElement.backgroundColor != null)
                            {
                                SolidColorBrush backgroundColorStyle = (SolidColorBrush)customStyleElement.backgroundColor;

                                menu.Background = backgroundColorStyle;
                            }
                            else
                                menu.Background = null;

                            if (customStyleElement.textColor != null)
                            {
                                SolidColorBrush textColorStyle = (SolidColorBrush)customStyleElement.textColor;

                                menu.Foreground = textColorStyle;
                            }
                            else
                                menu.Foreground = null;

                            if (customStyleElement.font != null)
                                menu.FontFamily = customStyleElement.font;
                            else
                                menu.FontFamily = new FontFamily("Segoe UI");
                            if (customStyleElement.fontSize >= 1)
                                menu.FontSize = customStyleElement.fontSize;
                            else
                                menu.FontSize = 12;

                            break;
                        }
                    case "label":
                        {
                            if (customStyleElement.backgroundColor != null)
                            {
                                SolidColorBrush backgroundColorStyle = (SolidColorBrush)customStyleElement.backgroundColor;

                                status_block.Background = backgroundColorStyle;
                            }
                            else
                                status_block.Background = null;

                            if (customStyleElement.textColor != null)
                            {
                                SolidColorBrush textColorStyle = (SolidColorBrush)customStyleElement.textColor;

                                status_block.Foreground = textColorStyle;
                            }
                            else
                                status_block.Foreground = null;

                            if (customStyleElement.font != null)
                                status_block.FontFamily = customStyleElement.font;
                            else
                                status_block.FontFamily = new FontFamily("Segoe UI");
                            if (customStyleElement.fontSize >= 1)
                                status_block.FontSize = customStyleElement.fontSize;
                            else
                                status_block.FontSize = 12;

                            break;
                        }
                    default:
                        break;
                }
            }
        }
    }
}
