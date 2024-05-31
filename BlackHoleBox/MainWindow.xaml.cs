using System.Windows; // 引入 WPF 相关命名空间
using System.Windows.Controls; // WPF 控件库 按钮、文本框
using System.Windows.Input;  //处理输入事件的命名空间
using System.Net.Http; // 引入用于发送 HTTP 请求的命名空间
using System.IO; // 引入文件操作相关的命名空间
using System.Windows.Media;  //媒体相关的类，如颜色、图像等
using HtmlAgilityPack; // 引入用于 HTML 解析的命名空间
using YourNamespace;


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

        private const string FilePath = "./data.bin"; // 存储文件路径

        public async Task<Dictionary<char, List<(string text, string link)>>> ExtractLinksByAlphabetAsync()
        {
            Dictionary<char, List<(string text, string link)>> linksByAlphabet = new Dictionary<char, List<(string text, string link)>>();

            // 检查文件是否存在，如果存在则从文件加载数据
            if (File.Exists(FilePath))
            {
                string fileContent = File.ReadAllText(FilePath); // 读取文件内容
                linksByAlphabet = DeserializeLinks(fileContent); // 反序列化文件内容
            }
            else
            {
                // 如果文件不存在，则从网络获取数据
                linksByAlphabet = await FetchLinksFromWeb(); // 从网络获取数据
                                                             // 将获取的数据保存到文件中
                string serializedData = SerializeLinks(linksByAlphabet); // 序列化获取的数据
                File.WriteAllText(FilePath, serializedData); // 写入文件
            }

            return linksByAlphabet; // 返回获取的数据
        }

        private async Task<Dictionary<char, List<(string text, string link)>>> FetchLinksFromWeb()
        {
            Dictionary<char, List<(string text, string link)>> linksByAlphabet = new Dictionary<char, List<(string text, string link)>>();

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync("https://flingtrainer.com/all-trainers-a-z/"); // 发送GET请求
                    response.EnsureSuccessStatusCode(); // 确保请求成功
                    string htmlContent = await response.Content.ReadAsStringAsync(); // 读取响应内容
                    HtmlDocument htmlDocument = new HtmlDocument();
                    htmlDocument.LoadHtml(htmlContent); // 加载HTML内容

                    for (char letter = 'A'; letter <= 'Z' || letter == '_'; letter = letter == 'Z' ? '_' : (char)(letter + 1))
                    {
                        string targetId = $"a-z-listing-letter-{letter}-1";
                        HtmlNode targetNode = htmlDocument.GetElementbyId(targetId); // 查找指定ID的节点

                        if (targetNode != null)
                        {
                            List<(string text, string link)> links = ExtractLinksFromNode(targetNode); // 从节点中提取链接
                            linksByAlphabet.Add(letter, links); // 将提取的链接添加到字典中
                        }
                        else
                        {
                            Console.WriteLine($"未找到指定 ID {targetId} 的节点。"); // 输出错误信息
                        }
                    }
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"HTTP 请求失败: {ex.Message}"); // 输出HTTP请求失败的错误信息
                }
            }

            return linksByAlphabet; // 返回获取的数据
        }

        private List<(string text, string link)> ExtractLinksFromNode(HtmlNode node)
        {
            List<(string text, string link)> links = new List<(string text, string link)>();
            HtmlNodeCollection aTags = node.SelectNodes(".//a"); // 查找节点内的所有<a>标签

            if (aTags != null)
            {
                foreach (HtmlNode aTag in aTags)
                {
                    string text = aTag.InnerText.Trim(); // 提取链接文本
                    string link = aTag.GetAttributeValue("href", ""); // 提取链接地址

                    if (!text.StartsWith("Back to top"))
                    {
                        links.Add((text, link)); // 将提取的链接添加到列表中
                    }
                }
            }
            else
            {
                Console.WriteLine($"在节点中未找到 <a> 标签。"); // 输出错误信息
            }

            return links; // 返回提取的链接列表
        }

        private string SerializeLinks(Dictionary<char, List<(string text, string link)>> linksByAlphabet)
        {
            // 将链接序列化为字符串（例如，使用JSON）
            // 这里只是简单地将数据连接起来
            List<string> serializedData = new List<string>();
            foreach (var kvp in linksByAlphabet)
            {
                foreach (var linkTuple in kvp.Value)
                {
                    serializedData.Add($"{kvp.Key}-{linkTuple.text}-{linkTuple.link}");
                }
            }
            return string.Join("\n", serializedData); // 返回序列化后的字符串
        }

        private Dictionary<char, List<(string text, string link)>> DeserializeLinks(string serializedData)
        {
            // 将字符串反序列化为字典
            Dictionary<char, List<(string text, string link)>> linksByAlphabet = new Dictionary<char, List<(string text, string link)>>();
            string[] lines = serializedData.Split('\n');
            foreach (string line in lines)
            {
                string[] parts = line.Split('-');
                char letter = parts[0][0];
                string text = parts[1];
                string link = parts[2];
                if (!linksByAlphabet.ContainsKey(letter))
                {
                    linksByAlphabet[letter] = new List<(string text, string link)>();
                }
                linksByAlphabet[letter].Add((text, link));
            }
            return linksByAlphabet; // 返回反序列化后的字典
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
