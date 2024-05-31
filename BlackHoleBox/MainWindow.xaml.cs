using System.Windows; // 引入 WPF 相关命名空间
using System.Windows.Controls;
using System.Windows.Input;
using System.Net.Http; // 引入用于发送 HTTP 请求的命名空间
using HtmlAgilityPack; // 引入用于 HTML 解析的命名空间
using System.Windows.Media;
using YourNamespace; // 引入你的自定义命名空间

namespace BlackHoleBox
{
    // 主窗口类
    public partial class MainWindow : Window
    {
        // 构造函数
        public MainWindow()
        {
            InitializeComponent();
            Get_links(); // 调用方法以获取链接并显示
        }

        // 获取链接的方法
        private async void Get_links()
        {
            // 异步获取链接
            Dictionary<char, List<(string text, string link)>> links = await ExtractLinksByAlphabetAsync();

            // 获取 View2 的 Grid 元素
            Grid grid = (Grid)FindName("View2_list");

            // 清空 Grid 中的所有子元素
            grid.Children.Clear();

            // 清空 Grid 中的行定义
            grid.RowDefinitions.Clear();

            // 清空 Grid 中的列定义
            grid.ColumnDefinitions.Clear();

            // 添加一列定义
            ColumnDefinition columnDefinition = new ColumnDefinition();
            columnDefinition.Width = GridLength.Auto;
            grid.ColumnDefinitions.Add(columnDefinition);

            // 遍历所有键值对
            foreach (var kvp in links)
            {
                char key = kvp.Key;
                List<(string text, string link)> value = kvp.Value;

                // 创建一个按钮来表示键
                Button keyButton = new Button();
                keyButton.Content = $"{key}";
                keyButton.Style = (Style)Resources["NoBorderButton"]; // 设置按钮样式

                // 添加点击事件处理程序
                keyButton.Click += (sender, e) =>
                {
                    // 显示当前键对应的网格
                    ShowGridForCurrentKey(key, grid, value);
                };

                // 将按钮添加到 Grid 中
                grid.Children.Add(keyButton);

                // 设置按钮在 Grid 中的位置
                Grid.SetRow(keyButton, 0); // 将按钮置于第一行
                Grid.SetColumn(keyButton, grid.ColumnDefinitions.Count - 1); // 将按钮置于最后一列

                // 设置列间距
                columnDefinition = new ColumnDefinition();
                columnDefinition.Width = GridLength.Auto;
                grid.ColumnDefinitions.Add(columnDefinition);

                if (kvp.Key == 'A')
                {
                    ShowGridForCurrentKey(key, grid, value);
                }
            }
        }

        // 根据当前键显示对应值的网格
        void ShowGridForCurrentKey(char currentKey, Grid grid, List<(string text, string link)> keyValuePairs)
        {
            // 清空非按钮元素
            for (int i = grid.Children.Count - 1; i >= 0; i--)
            {
                if (!(grid.Children[i] is Button))
                {
                    grid.Children.RemoveAt(i);
                }
            }
            grid.RowDefinitions.Clear();

            // 添加一些行间距
            RowDefinition rowDefinition = new RowDefinition();
            rowDefinition.Height = GridLength.Auto;
            grid.RowDefinitions.Add(rowDefinition);

            // 显示当前键对应的值
            foreach (var link in keyValuePairs)
            {
                // 创建一个 TextBlock 来显示链接的文本
                TextBlock linkTextBlock = new TextBlock();
                linkTextBlock.Text = $"{link.text}";

                // 将 TextBlock 添加到 Grid 中
                grid.Children.Add(linkTextBlock);

                // 添加一些行间距
                rowDefinition = new RowDefinition();
                rowDefinition.Height = GridLength.Auto;
                grid.RowDefinitions.Add(rowDefinition);

                // 设置 TextBlock 在 Grid 中的位置
                Grid.SetRow(linkTextBlock, grid.RowDefinitions.Count - 1);

                // 添加点击事件处理程序
                linkTextBlock.MouseLeftButtonUp += async (sender, e) =>
                {
                    // 调用下载方法
                    await DownloadWindow.HandleDownloadAsync(link.text, link.link);
                };

                // 添加鼠标进入事件处理程序
                linkTextBlock.MouseEnter += (sender, e) =>
                {
                    // 当鼠标进入时，改变文字颜色为红色
                    linkTextBlock.Foreground = Brushes.Red;
                };

                // 添加鼠标离开事件处理程序
                linkTextBlock.MouseLeave += (sender, e) =>
                {
                    // 当鼠标离开时，恢复文字颜色为默认颜色
                    linkTextBlock.Foreground = Brushes.Black; // 你可以根据需要设置为其他颜色
                };
            }
        }

        // 爬取链接的方法
        private async Task<Dictionary<char, List<(string text, string link)>>> ExtractLinksByAlphabetAsync()
        {
            // 创建存储链接的字典
            Dictionary<char, List<(string text, string link)>> linksByAlphabet = new Dictionary<char, List<(string text, string link)>>();

            // 创建 HttpClient 实例
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // 发送 GET 请求获取页面内容
                    HttpResponseMessage response = await client.GetAsync("https://flingtrainer.com/all-trainers-a-z/");

                    // 确保请求成功
                    response.EnsureSuccessStatusCode();

                    // 读取响应内容
                    string htmlContent = await response.Content.ReadAsStringAsync();

                    // 创建 HtmlDocument 实例并加载 HTML 内容
                    HtmlDocument htmlDocument = new HtmlDocument();
                    htmlDocument.LoadHtml(htmlContent);

                    // 提取所有以"a-z-listing-letter-"开头的 ID
                    for (char letter = 'A'; letter <= 'Z' || letter == '_'; letter = letter == 'Z' ? '_' : (char)(letter + 1))
                    {
                        string targetId = $"a-z-listing-letter-{letter}-1";
                        // 查找指定 ID 的节点
                        HtmlNode targetNode = htmlDocument.GetElementbyId(targetId);

                        // 如果找到了目标节点
                        if (targetNode != null)
                        {
                            // 查找目标节点内的所有 <a> 标签
                            HtmlNodeCollection aTags = targetNode.SelectNodes(".//a");

                            // 如果找到了 <a> 标签
                            if (aTags != null)
                            {
                                // 创建用于存储链接和文本内容的列表
                                List<(string text, string link)> links = new List<(string text, string link)>();

                                // 遍历所有 <a> 标签并添加到列表中
                                foreach (HtmlNode aTag in aTags)
                                {
                                    string text = aTag.InnerText.Trim();
                                    string link = aTag.GetAttributeValue("href", "");

                                    // 检查文本内容，如果不是以 "Back to top" 开头，则添加到列表中
                                    if (!text.StartsWith("Back to top"))
                                    {
                                        links.Add((text, link));
                                    }
                                }

                                // 将列表添加到字典中以字母为键
                                linksByAlphabet.Add(letter, links);
                            }
                            else
                            {
                                Console.WriteLine($"在 {targetId} 中未找到 <a> 标签。");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"未找到指定 ID {targetId} 的节点。");
                        }
                    }
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"HTTP 请求失败: {ex.Message}");
                }
            }

            return linksByAlphabet;
        }

        // 切换到视图1的方法
        private void PreviewBlock1_Click(object sender, RoutedEventArgs e)
        {
            // 切换到视图1
            View1.Visibility = Visibility.Visible;
            View2.Visibility = Visibility.Collapsed;
            View3.Visibility = Visibility.Collapsed;
        }

        // 切换到视图2的方法
        private void PreviewBlock2_Click(object sender, RoutedEventArgs e)
        {
            View1.Visibility = Visibility.Collapsed;
            View2.Visibility = Visibility.Visible;
            View3.Visibility = Visibility.Collapsed;
        }

        // 切换到视图3的方法
        private void PreviewBlock3_Click(object sender, RoutedEventArgs e)
        {
            View1.Visibility = Visibility.Collapsed;
            View2.Visibility = Visibility.Collapsed;
            View3.Visibility = Visibility.Visible;
        }

        // 当标题栏被按下时，开始拖动窗口
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        // 当关闭按钮被点击时关闭窗口
        private void CloseButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Close();
        }
    }
}
