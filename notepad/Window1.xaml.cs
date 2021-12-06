using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace notepad
{
    /// <summary>
    /// Логика взаимодействия для Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        private Dictionary<string, CustomStyle> customStyles = new Dictionary<string, CustomStyle>();
        private Dictionary<string, int> stylesId = new Dictionary<string, int>();

        private MainWindow mainWindow;

        public Window1(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
            InitializeComponent();

            MySqlConnection connection = DBUtils.GetDBConnection();
            //Открытие соединения с БД
            connection.Open();

            //Обёртка try catch чтобы соединение было точно установлено и выполнена задача.
            try
            {
                //Исполнения метода
                refreshStylesBox(connection);
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

        private void refreshStylesBox(MySqlConnection connection)
        {
            //Очистка массива
            stylesBox.Items.Clear();
            //Команда как будет исполнена
            string sql = "SELECT * FROM styles";

            //Создание команды (Которая будет уже исполнена как в БД)
            MySqlCommand sqlCommand = new MySqlCommand();

            //Установка БД, в которой будет исполнена команда
            sqlCommand.Connection = connection;
            //Установка команды, которая будет исполнена
            sqlCommand.CommandText = sql;

            //Создание переменной с данными, которые получены из запроса
            using (DbDataReader reader = sqlCommand.ExecuteReader())
            {
                //Проверка на то, что есть хотя бы 1 строчка
                if (reader.HasRows)
                {
                    //Перескакивание (Чтение строк по 1)
                    while (reader.Read())
                    {
                        //Вытаскивание значения из столбца "name" - получение имени водителя в конкретной строке таблицы, дальше также
                        string name = reader.GetString(reader.GetOrdinal("name"));
                        int id = reader.GetInt32(reader.GetOrdinal("id"));

                        stylesBox.Items.Add($"{name}");

                        customStyles.Add(name, null);
                        stylesId.Add(name, id);
                    }
                }
            }

            foreach (string key in stylesId.Keys)
            {
                int id = stylesId[key];

                string sqlSelectStyles = $"SELECT * FROM styles_settings WHERE style_id={id}";

                //Создание команды (Которая будет уже исполнена как в БД)
                MySqlCommand sqlCommandSelectStyles = new MySqlCommand();

                //Установка БД, в которой будет исполнена команда
                sqlCommandSelectStyles.Connection = connection;
                //Установка команды, которая будет исполнена
                sqlCommandSelectStyles.CommandText = sqlSelectStyles;

                CustomStyle customStyle = new CustomStyle();

                using (DbDataReader readerStyles = sqlCommandSelectStyles.ExecuteReader())
                {
                    //Проверка на то, что есть хотя бы 1 строчка
                    if (readerStyles.HasRows)
                    {
                        //Перескакивание (Чтение строк по 1)
                        while (readerStyles.Read())
                        {
                            CustomStyleElement customStyleElement = new CustomStyleElement();

                            string param = readerStyles.GetString(readerStyles.GetOrdinal("param"));
                            string value = readerStyles.GetString(readerStyles.GetOrdinal("value"));
                            string elementName = readerStyles.GetString(readerStyles.GetOrdinal("element_name"));

                            Thickness thickness = new Thickness(-1, -1, -1, -1);
                            FontFamily font = null;
                            double fontSize = 0.0;
                            Brush backgroundColor = null;
                            Brush textColor = null;

                            if (customStyle.getElementByName(elementName) != null)
                                customStyleElement = customStyle.getElementByName(elementName);

                            switch (param)
                            {
                                case "border":
                                    string[] infoBorder = value.Split(';');

                                    double left = double.Parse(infoBorder[0]);
                                    double right = double.Parse(infoBorder[1]);
                                    double top = double.Parse(infoBorder[2]);
                                    double bottom = double.Parse(infoBorder[3]);

                                    thickness = new Thickness(left, top, right, bottom);
                                    customStyleElement.thickness = thickness;
                                    break;
                                case "textFont":
                                    font = new FontFamily(value);

                                    customStyleElement.font = font;
                                    break;
                                case "fontSize":
                                    fontSize = double.Parse(value);

                                    customStyleElement.fontSize = fontSize;
                                    break;
                                case "backgroundColor":
                                    {
                                        string[] infoBackgroundColor = value.Split(';');

                                        byte r = byte.Parse(infoBackgroundColor[0]);
                                        byte g = byte.Parse(infoBackgroundColor[1]);
                                        byte b = byte.Parse(infoBackgroundColor[2]);

                                        backgroundColor = new SolidColorBrush(Color.FromRgb(r, g, b));

                                        customStyleElement.backgroundColor = backgroundColor;
                                        break;
                                    }
                                case "textColor":
                                    {
                                        string[] infoTextColor = value.Split(';');

                                        byte r = byte.Parse(infoTextColor[0]);
                                        byte g = byte.Parse(infoTextColor[1]);
                                        byte b = byte.Parse(infoTextColor[2]);

                                        textColor = new SolidColorBrush(Color.FromRgb(r, g, b));

                                        customStyleElement.textColor = textColor;
                                        break;
                                    }
                                default:
                                    break;
                            }

                            customStyleElement.name = elementName;

                            customStyle.elements.Add(customStyleElement);
                        }
                    }
                }
                customStyle.name = key;
                customStyles[key] = customStyle;
            }
        }

        private bool startStyle = false;

        private void stylesBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (startStyle == false)
            {
                elements.Items.Add("Поле ввода");
                elements.Items.Add("Поле перевода");
                elements.Items.Add("Меню");
                elements.Items.Add("Окно");
                elements.Items.Add("Состояние");

                startStyle = true;
            }

            CustomStyle customStyle = customStyles[stylesBox.SelectedItem.ToString()];

            updateElements();

            foreach (CustomStyleElement item in customStyle.elements)
            {
                updatePreview(item);
            }
        }

        private void elements_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            updateElements();
        }

        private void updateElements()
        {
            if (stylesBox.SelectedItem != null)
            {
                if (elements.SelectedItem != null)
                {
                    string elementName = elements.SelectedItem.ToString();

                    string elementDbName = "";

                    switch (elementName)
                    {
                        case "Поле ввода":
                            elementDbName = "richtextbox";
                            break;
                        case "Поле перевода":
                            elementDbName = "textblock";
                            break;
                        case "Окно":
                            elementDbName = "window";
                            break;
                        case "Меню":
                            elementDbName = "menu";
                            break;
                        case "Состояние":
                            elementDbName = "label";
                            break;
                        default:
                            break;
                    }

                    CustomStyle customStyle = customStyles[stylesBox.SelectedItem.ToString()];

                    CustomStyleElement customStyleElement = null;

                    foreach (CustomStyleElement item in customStyle.elements)
                    {
                        if (item.name.Equals(elementDbName))
                        {
                            customStyleElement = item;
                        }
                    }

                    if (customStyleElement != null)
                        updatePreviewElement(customStyleElement);

                    if (customStyleElement == null || customStyleElement.backgroundColor == null)
                        backgroundColor.Color = Color.FromRgb(0, 0, 0);

                    if (customStyleElement == null || customStyleElement.textColor == null)
                        textColor.Color = Color.FromRgb(0, 0, 0);

                    if (customStyleElement == null || customStyleElement.font == null)
                        textFont.Text = "Segoe UI";

                    if (customStyleElement == null || customStyleElement.fontSize < 1)
                        textFontSize.Text = "12";


                    textColor.Visibility = Visibility.Hidden;
                    textColor_checkBox.Visibility = Visibility.Hidden;
                    backgroundColor.Visibility = Visibility.Hidden;
                    backgroundColor_checkBox.Visibility = Visibility.Hidden;
                    textFont.Visibility = Visibility.Hidden;
                    textFont_checkBox.Visibility = Visibility.Hidden;
                    textFont.Visibility = Visibility.Hidden;
                    textFont_checkBox.Visibility = Visibility.Hidden;
                    borderBottom.Visibility = Visibility.Hidden;
                    borderTop.Visibility = Visibility.Hidden;
                    borderLeft.Visibility = Visibility.Hidden;
                    borderRight.Visibility = Visibility.Hidden;
                    borderBottom_label.Visibility = Visibility.Hidden;
                    borderTop_label.Visibility = Visibility.Hidden;
                    borderLeft_label.Visibility = Visibility.Hidden;
                    borderRight_label.Visibility = Visibility.Hidden;
                    border_checkBox.Visibility = Visibility.Hidden;
                    textFontSize.Visibility = Visibility.Hidden;
                    textFontSize_checkbox.Visibility = Visibility.Hidden;

                    if (elementName.StartsWith("Поле ввода"))
                    {
                        textColor.Visibility = Visibility.Visible;
                        textColor_checkBox.Visibility = Visibility.Visible;
                        backgroundColor.Visibility = Visibility.Visible;
                        backgroundColor_checkBox.Visibility = Visibility.Visible;
                        textFont.Visibility = Visibility.Visible;
                        textFont_checkBox.Visibility = Visibility.Visible;
                        textFont.Visibility = Visibility.Visible;
                        textFont_checkBox.Visibility = Visibility.Visible;
                        borderBottom.Visibility = Visibility.Visible;
                        borderTop.Visibility = Visibility.Visible;
                        borderLeft.Visibility = Visibility.Visible;
                        borderRight.Visibility = Visibility.Visible;
                        borderBottom_label.Visibility = Visibility.Visible;
                        borderTop_label.Visibility = Visibility.Visible;
                        borderLeft_label.Visibility = Visibility.Visible;
                        borderRight_label.Visibility = Visibility.Visible;
                        border_checkBox.Visibility = Visibility.Visible;
                        textFontSize.Visibility = Visibility.Visible;
                        textFontSize_checkbox.Visibility = Visibility.Visible;
                    }
                    else if (elementName.StartsWith("Поле перевода"))
                    {
                        textColor.Visibility = Visibility.Visible;
                        textColor_checkBox.Visibility = Visibility.Visible;
                        backgroundColor.Visibility = Visibility.Visible;
                        backgroundColor_checkBox.Visibility = Visibility.Visible;
                        textFont.Visibility = Visibility.Visible;
                        textFont_checkBox.Visibility = Visibility.Visible;
                        textFont.Visibility = Visibility.Visible;
                        textFont_checkBox.Visibility = Visibility.Visible;
                        textFontSize.Visibility = Visibility.Visible;
                        textFontSize_checkbox.Visibility = Visibility.Visible;
                    }
                    else if (elementName.StartsWith("Окно"))
                    {
                        backgroundColor.Visibility = Visibility.Visible;
                        backgroundColor_checkBox.Visibility = Visibility.Visible;
                    }
                    else if (elementName.StartsWith("Меню"))
                    {
                        textColor.Visibility = Visibility.Visible;
                        textColor_checkBox.Visibility = Visibility.Visible;
                        backgroundColor.Visibility = Visibility.Visible;
                        backgroundColor_checkBox.Visibility = Visibility.Visible;
                        textFont.Visibility = Visibility.Visible;
                        textFont_checkBox.Visibility = Visibility.Visible;
                        textFont.Visibility = Visibility.Visible;
                        textFont_checkBox.Visibility = Visibility.Visible;
                        textFontSize.Visibility = Visibility.Visible;
                        textFontSize_checkbox.Visibility = Visibility.Visible;
                    }
                    else if (elementName.StartsWith("Состояние"))
                    {
                        textColor.Visibility = Visibility.Visible;
                        textColor_checkBox.Visibility = Visibility.Visible;
                        backgroundColor.Visibility = Visibility.Visible;
                        backgroundColor_checkBox.Visibility = Visibility.Visible;
                        textFont.Visibility = Visibility.Visible;
                        textFont_checkBox.Visibility = Visibility.Visible;
                        textFont.Visibility = Visibility.Visible;
                        textFont_checkBox.Visibility = Visibility.Visible;
                        textFontSize.Visibility = Visibility.Visible;
                        textFontSize_checkbox.Visibility = Visibility.Visible;
                    }
                }
            }
        }


        private void updatePreview(CustomStyleElement customStyleElement)
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

                            window.Background = backgroundColorStyle;
                        } else
                            window.Background = Brushes.White;

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

                            status.Background = backgroundColorStyle;
                        }
                        else
                            status.Background = null;

                        if (customStyleElement.textColor != null)
                        {
                            SolidColorBrush textColorStyle = (SolidColorBrush)customStyleElement.textColor;

                            status.Foreground = textColorStyle;
                        }
                        else
                            status.Foreground = null;

                        if (customStyleElement.font != null)
                            status.FontFamily = customStyleElement.font;
                        else
                            status.FontFamily = new FontFamily("Segoe UI");
                        if (customStyleElement.fontSize >= 1)
                            status.FontSize = customStyleElement.fontSize;
                        else
                            status.FontSize = 12;

                        break;
                    }
                default:
                    break;
            }
        }

        private void updatePreviewElement(CustomStyleElement customStyleElement)
        {
            Thickness thickness = customStyleElement.thickness;

            if (thickness.Left != -1 && thickness.Right != -1 && thickness.Top != -1 && thickness.Bottom != -1)
            {
                borderBottom.Text = thickness.Bottom + "";
                borderTop.Text = thickness.Top + "";
                borderLeft.Text = thickness.Left + "";
                borderRight.Text = thickness.Right + "";
            }

            if (customStyleElement.backgroundColor != null)
            {
                SolidColorBrush backgroundColorStyle = (SolidColorBrush)customStyleElement.backgroundColor;

                backgroundColor_checkBox.IsChecked = true;
                backgroundColor.Color = backgroundColorStyle.Color;
            }

            if (customStyleElement.textColor != null)
            {
                SolidColorBrush textColorStyle = (SolidColorBrush)customStyleElement.textColor;

                textColor_checkBox.IsChecked = true;
                textColor.Color = textColorStyle.Color;
            }

            if (customStyleElement.font != null)
                textFont.Text = customStyleElement.font.Source;

            if (customStyleElement.fontSize >= 1)
                textFontSize.Text = customStyleElement.fontSize + "";
        }

        private string[] dbnames = new string[] { "richtextbox", "textblock", "window", "menu", "label" };

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            if (stylesBox.SelectedItem != null &&  elements.SelectedItem != null)
            {
                string elementName = elements.SelectedItem.ToString();
                CustomStyle customStyle = customStyles[stylesBox.SelectedItem.ToString()];
                int id = 0;

                string elementDbName = "";

                switch (elementName)
                {
                    case "Поле ввода":
                        elementDbName = "richtextbox";
                        break;
                    case "Поле перевода":
                        elementDbName = "textblock";
                        break;
                    case "Окно":
                        elementDbName = "window";
                        break;
                    case "Меню":
                        elementDbName = "menu";
                        break;
                    case "Состояние":
                        elementDbName = "label";
                        break;
                    default:
                        break;
                }

                MySqlConnection connection = DBUtils.GetDBConnection();
                //Открытие соединения с БД
                connection.Open();

                //Обёртка try catch чтобы соединение было точно установлено и выполнена задача.
                try
                {
                    id = getId(customStyle.name, connection);

                    if (customStyle.getElementByName(elementDbName) == null)
                    {
                        CustomStyleElement newCustomElement = new CustomStyleElement();

                        newCustomElement.thickness = new Thickness(-1, -1, -1, -1);
                        newCustomElement.name = elementDbName;

                        customStyle.elements.Add(newCustomElement);
                    }

                    foreach (string str in dbnames)
                    {
                        CustomStyleElement item = customStyle.getElementByName(elementDbName);
                        if(item.name.Equals(str))
                        {
                            double textFontSizeStyle = 0.0;
                            Thickness thickness = item.thickness;
                            FontFamily font = null;
                            Brush textColorStyle = null;
                            Brush backgroundColorStyle = null;

                            if (elementName.StartsWith("Поле ввода"))
                            {
                                if (thickness.Left != -1 && thickness.Right != -1 && thickness.Top != -1 && thickness.Bottom != -1)
                                    thickness = new Thickness(double.Parse(borderLeft.Text), double.Parse(borderTop.Text), double.Parse(borderRight.Text), double.Parse(borderBottom.Text));

                                item.thickness = thickness;

                                if (textFontSize_checkbox.IsChecked ?? false)
                                {
                                    textFontSizeStyle = double.Parse(textFontSize.Text);
                                    item.fontSize = textFontSizeStyle;
                                }
                                else
                                {
                                    item.fontSize = 0;
                                }
                                if (textFont_checkBox.IsChecked ?? false)
                                {
                                    font = new FontFamily(textFont.Text.ToString());
                                    item.font = font;
                                }
                                else
                                {
                                    item.font = null;
                                }
                                if (textColor_checkBox.IsChecked ?? false)
                                {
                                    textColorStyle = new SolidColorBrush(textColor.Color);
                                    item.textColor = textColorStyle;
                                }
                                else
                                {
                                    item.textColor = null;
                                }
                                if (backgroundColor_checkBox.IsChecked ?? false)
                                {
                                    backgroundColorStyle = new SolidColorBrush(backgroundColor.Color);
                                    item.backgroundColor = backgroundColorStyle;
                                }
                                else
                                {
                                    item.backgroundColor = null;
                                }

                            }
                            if (elementName.StartsWith("Состояние"))
                            {
                                if (textFontSize_checkbox.IsChecked ?? false)
                                {
                                    textFontSizeStyle = double.Parse(textFontSize.Text);
                                    item.fontSize = textFontSizeStyle;
                                }
                                else
                                {
                                    item.fontSize = 0;
                                }
                                if (textFont_checkBox.IsChecked ?? false)
                                {
                                    font = new FontFamily(textFont.Text.ToString());
                                    item.font = font;
                                }
                                else
                                {
                                    item.font = null;
                                }
                                if (textColor_checkBox.IsChecked ?? false)
                                {
                                    textColorStyle = new SolidColorBrush(textColor.Color);
                                    item.textColor = textColorStyle;
                                }
                                else
                                {
                                    item.textColor = null;
                                }
                                if (backgroundColor_checkBox.IsChecked ?? false)
                                {
                                    backgroundColorStyle = new SolidColorBrush(backgroundColor.Color);
                                    item.backgroundColor = backgroundColorStyle;
                                }
                                else
                                {
                                    item.backgroundColor = null;
                                }
                            }
                            if (elementName.StartsWith("Поле перевода"))
                            {
                                if (textFontSize_checkbox.IsChecked ?? false)
                                {
                                    textFontSizeStyle = double.Parse(textFontSize.Text);
                                    item.fontSize = textFontSizeStyle;
                                }
                                else
                                {
                                    item.fontSize = 0;
                                }
                                if (textFont_checkBox.IsChecked ?? false)
                                {
                                    font = new FontFamily(textFont.Text.ToString());
                                    item.font = font;
                                }
                                else
                                {
                                    item.font = null;
                                }
                                if (textColor_checkBox.IsChecked ?? false)
                                {
                                    textColorStyle = new SolidColorBrush(textColor.Color);
                                    item.textColor = textColorStyle;
                                }
                                else
                                {
                                    item.textColor = null;
                                }
                                if (backgroundColor_checkBox.IsChecked ?? false)
                                {
                                    backgroundColorStyle = new SolidColorBrush(backgroundColor.Color);
                                    item.backgroundColor = backgroundColorStyle;
                                }
                                else
                                {
                                    item.backgroundColor = null;
                                }

                                MessageBox.Show(textFontSizeStyle + "");
                            }
                            if (elementName.StartsWith("Окно"))
                            {
                                if (backgroundColor_checkBox.IsChecked ?? false)
                                {
                                    backgroundColorStyle = new SolidColorBrush(backgroundColor.Color);
                                    item.backgroundColor = backgroundColorStyle;
                                }
                                else
                                {
                                    item.backgroundColor = null;
                                }
                            }
                            if (elementName.StartsWith("Меню"))
                            {
                                if (textFontSize_checkbox.IsChecked ?? false)
                                {
                                    textFontSizeStyle = double.Parse(textFontSize.Text);
                                    item.fontSize = textFontSizeStyle;
                                }
                                else
                                {
                                    item.fontSize = 0;
                                }
                                if (textFont_checkBox.IsChecked ?? false)
                                {
                                    font = new FontFamily(textFont.Text.ToString());
                                    item.font = font;
                                }
                                else
                                {
                                    item.font = null;
                                }
                                if (textColor_checkBox.IsChecked ?? false)
                                {
                                    textColorStyle = new SolidColorBrush(textColor.Color);
                                    item.textColor = textColorStyle;
                                }
                                else
                                {
                                    item.textColor = null;
                                }
                                if (backgroundColor_checkBox.IsChecked ?? false)
                                {
                                    backgroundColorStyle = new SolidColorBrush(backgroundColor.Color);
                                    item.backgroundColor = backgroundColorStyle;
                                }
                                else
                                {
                                    item.backgroundColor = null;
                                }
                            }

                            if (textFontSizeStyle > 1)
                            {
                                string param = "fontSize";

                                string updateStr = $"`value`='{textFontSizeStyle}'";

                                string sql = "";

                                bool existParam = false;
                                MySqlCommand commandSelect = new MySqlCommand();
                                commandSelect.Connection = connection;
                                commandSelect.CommandText = $"SELECT * FROM `styles_settings` WHERE style_id={id} AND element_name='{item.name}' AND param='{param}'";

                                using (DbDataReader readerStyles = commandSelect.ExecuteReader())
                                {
                                    if (readerStyles.HasRows)
                                    {
                                        existParam = true;
                                    }
                                }

                                if (existParam)
                                    sql = $"UPDATE `styles_settings` SET {updateStr} WHERE style_id={id} AND element_name='{item.name}' AND param='{param}'";
                                else
                                    sql = $"INSERT INTO `styles_settings`(`style_id`, `element_name`, `param`, `value`) VALUES ({id}, '{item.name}', '{param}', '{textFontSizeStyle}')";

                                MySqlCommand sqlCommand = new MySqlCommand();

                                sqlCommand.Connection = connection;
                                sqlCommand.CommandText = sql;

                                sqlCommand.ExecuteNonQuery();
                            }

                            if (font != null)
                            {
                                string param = "textFont";

                                string updateStr = $"`value`='{font.Source}'";

                                string sql = "";

                                bool existParam = false;
                                MySqlCommand commandSelect = new MySqlCommand();
                                commandSelect.Connection = connection;
                                commandSelect.CommandText = $"SELECT * FROM `styles_settings` WHERE style_id={id} AND element_name='{item.name}' AND param='{param}'";
                                using (DbDataReader readerStyles = commandSelect.ExecuteReader())
                                {
                                    if (readerStyles.HasRows)
                                    {
                                        existParam = true;
                                    }
                                }

                                if (existParam)
                                    sql = $"UPDATE `styles_settings` SET {updateStr} WHERE style_id={id} AND element_name='{item.name}' AND param='{param}'";
                                else
                                    sql = $"INSERT INTO `styles_settings`(`style_id`, `element_name`, `param`, `value`) VALUES ({id}, '{item.name}', '{param}', '{font.Source}')";

                                MySqlCommand sqlCommand = new MySqlCommand();

                                sqlCommand.Connection = connection;
                                sqlCommand.CommandText = sql;

                                sqlCommand.ExecuteNonQuery();
                            }

                            if (thickness.Left != -1 && thickness.Right != -1 && thickness.Top != -1 && thickness.Bottom != -1)
                            {
                                string param = "border";

                                string updateStr = $"`value`='{thickness.Left};{thickness.Right};{thickness.Top};{thickness.Bottom}'";

                                string sql = "";

                                bool existParam = false;
                                MySqlCommand commandSelect = new MySqlCommand();
                                commandSelect.Connection = connection;
                                commandSelect.CommandText = $"SELECT * FROM `styles_settings` WHERE style_id={id} AND element_name='{item.name}' AND param='{param}'";
                                using (DbDataReader readerStyles = commandSelect.ExecuteReader())
                                {
                                    if (readerStyles.HasRows)
                                    {
                                        existParam = true;
                                    }
                                }

                                if (existParam)
                                    sql = $"UPDATE `styles_settings` SET {updateStr} WHERE style_id={id} AND element_name='{item.name}' AND param='{param}'";
                                else
                                    sql = $"INSERT INTO `styles_settings`(`style_id`, `element_name`, `param`, `value`) VALUES ({id}, '{item.name}', '{param}', '{thickness.Left};{thickness.Right};{thickness.Top};{thickness.Bottom}')";


                                MySqlCommand sqlCommand = new MySqlCommand();

                                sqlCommand.Connection = connection;
                                sqlCommand.CommandText = sql;

                                sqlCommand.ExecuteNonQuery();
                            }

                            if (backgroundColorStyle != null)
                            {
                                SolidColorBrush color = (SolidColorBrush)backgroundColorStyle;
                                string param = "backgroundColor";

                                string updateStr = $"`value`='{color.Color.R};{color.Color.G};{color.Color.B}'";

                                string sql = "";

                                bool existParam = false;
                                MySqlCommand commandSelect = new MySqlCommand();
                                commandSelect.Connection = connection;
                                commandSelect.CommandText = $"SELECT * FROM `styles_settings` WHERE style_id={id} AND element_name='{item.name}' AND param='{param}'";
                                using (DbDataReader readerStyles = commandSelect.ExecuteReader())
                                {
                                    if (readerStyles.HasRows)
                                    {
                                        existParam = true;
                                    }
                                }

                                if (existParam)
                                    sql = $"UPDATE `styles_settings` SET {updateStr} WHERE style_id={id} AND element_name='{item.name}' AND param='{param}'";
                                else
                                    sql = $"INSERT INTO `styles_settings`(`style_id`, `element_name`, `param`, `value`) VALUES ({id}, '{item.name}', '{param}', '{color.Color.R};{color.Color.G};{color.Color.B}')";

                                MySqlCommand sqlCommand = new MySqlCommand();

                                sqlCommand.Connection = connection;
                                sqlCommand.CommandText = sql;

                                sqlCommand.ExecuteNonQuery();
                            }

                            if (textColorStyle != null)
                            {
                                SolidColorBrush color = (SolidColorBrush)textColorStyle;
                                string param = "textColor";

                                string updateStr = $"`value`='{color.Color.R};{color.Color.G};{color.Color.B}'";

                                string sql = "";

                                bool existParam = false;
                                MySqlCommand commandSelect = new MySqlCommand();
                                commandSelect.Connection = connection;
                                commandSelect.CommandText = $"SELECT * FROM `styles_settings` WHERE style_id={id} AND element_name='{item.name}' AND param='{param}'";
                                using (DbDataReader readerStyles = commandSelect.ExecuteReader())
                                {
                                    if (readerStyles.HasRows)
                                    {
                                        existParam = true;
                                    }
                                }

                                if (existParam)
                                    sql = $"UPDATE `styles_settings` SET {updateStr} WHERE style_id={id} AND element_name='{item.name}' AND param='{param}'";
                                else
                                    sql = $"INSERT INTO `styles_settings`(`style_id`, `element_name`, `param`, `value`) VALUES ({id}, '{item.name}', '{param}', '{color.Color.R};{color.Color.G};{color.Color.B}')";

                                MySqlCommand sqlCommand = new MySqlCommand();

                                sqlCommand.Connection = connection;
                                sqlCommand.CommandText = sql;

                                sqlCommand.ExecuteNonQuery();
                            }

                            updatePreview(item);
                        }
                    }
                }
                catch (MySqlException)
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

        private int getId(string customStyleName, MySqlConnection connection)
        {
            //Команда как будет исполнена
            string sql = $"SELECT * FROM styles WHERE name='{customStyleName}'";

            //Создание команды (Которая будет уже исполнена как в БД)
            MySqlCommand sqlCommand = new MySqlCommand();

            //Установка БД, в которой будет исполнена команда
            sqlCommand.Connection = connection;
            //Установка команды, которая будет исполнена
            sqlCommand.CommandText = sql;

            using (DbDataReader reader = sqlCommand.ExecuteReader())
            {
                //Проверка на то, что есть хотя бы 1 строчка
                if (reader.HasRows)
                {
                    //Перескакивание (Чтение строк по 1)
                    if (reader.Read())
                    {
                        return reader.GetInt32(reader.GetOrdinal("id"));
                    }
                }
            }

            return 0;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogCreateNewStyle dialog = new DialogCreateNewStyle();

            if (dialog.ShowDialog() == true)
            {
                MySqlConnection connection = DBUtils.GetDBConnection();
                //Открытие соединения с БД
                connection.Open();

                try
                {
                    MySqlCommand command = new MySqlCommand();
                    command.Connection = connection;
                    command.CommandText = $"INSERT INTO `styles`(`name`) VALUES ('{dialog.ResponseText}')";

                    command.ExecuteNonQuery();

                    int id = getId(dialog.ResponseText, connection);

                    foreach(string dbNameElement in dbnames)
                    {
                        string insertStyleSettings = $"INSERT INTO `styles_settings`(`style_id`, `element_name`, `param`, `value`) VALUES({id}, '{dbNameElement}', 'textFont', 'Segoe UI')";
                        command.CommandText = insertStyleSettings;
                        command.ExecuteNonQuery();

                        insertStyleSettings = $"INSERT INTO `styles_settings`(`style_id`, `element_name`, `param`, `value`) VALUES({id}, '{dbNameElement}', 'border', '1;1;1;1')";
                        command.CommandText = insertStyleSettings;
                        command.ExecuteNonQuery();

                        insertStyleSettings = $"INSERT INTO `styles_settings`(`style_id`, `element_name`, `param`, `value`) VALUES({id}, '{dbNameElement}', 'backgroundColor', '255;255;255')";
                        command.CommandText = insertStyleSettings;
                        command.ExecuteNonQuery();

                        insertStyleSettings = $"INSERT INTO `styles_settings`(`style_id`, `element_name`, `param`, `value`) VALUES({id}, '{dbNameElement}', 'textColor', '0;0;0')";
                        command.CommandText = insertStyleSettings;
                        command.ExecuteNonQuery();

                        insertStyleSettings = $"INSERT INTO `styles_settings`(`style_id`, `element_name`, `param`, `value`) VALUES({id}, '{dbNameElement}', 'fontSize', '12')";
                        command.CommandText = insertStyleSettings;
                        command.ExecuteNonQuery();
                    }
                }
                catch (MySqlException)
                {
                    throw;
                }
                finally
                {
                    connection.Close();
                    connection.Dispose();

                    this.Close();
                    new Window1(mainWindow).Show();
                }
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            DialogCreateNewStyle dialog = new DialogCreateNewStyle();

            if (stylesBox.SelectedItem != null)
            {
                string selectedStyle = stylesBox.SelectedItem.ToString();
                if (dialog.ShowDialog() == true)
                {
                    MySqlConnection connection = DBUtils.GetDBConnection();
                    connection.Open();

                    try
                    {
                        if (getId(selectedStyle, connection) != 0)
                        {
                            int id = getId(selectedStyle, connection);

                            MySqlCommand command = new MySqlCommand();
                            command.Connection = connection;
                            command.CommandText = $"UPDATE `styles` SET `name`='{dialog.ResponseText}' WHERE id={id}";

                            command.ExecuteNonQuery();
                        }
                        else
                        {
                            MessageBox.Show("Такого стиля не существует");
                        }
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                    finally
                    {
                        connection.Close();
                        connection.Dispose();

                        this.Close();
                        new Window1(mainWindow).Show();
                    }
                }
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if (stylesBox.SelectedItem != null)
            {
                string selectedStyle = stylesBox.SelectedItem.ToString();
                MySqlConnection connection = DBUtils.GetDBConnection();
                connection.Open();

                try
                {
                    if (getId(selectedStyle, connection) != 0)
                    {
                        int id = getId(selectedStyle, connection);

                        MySqlCommand command = new MySqlCommand();
                        command.Connection = connection;
                        command.CommandText = $"DELETE FROM `styles` WHERE id={id}";

                        command.ExecuteNonQuery();
                    }
                    else
                    {
                        MessageBox.Show("Такого стиля не существует");
                    }
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    connection.Close();
                    connection.Dispose();

                    this.Close();
                    new Window1(mainWindow).Show();
                }
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (stylesBox.SelectedItem != null)
            {
                string selectedStyle = stylesBox.SelectedItem.ToString();
                mainWindow.renderStyle(customStyles[selectedStyle]);
            }
        }
    }   
}
