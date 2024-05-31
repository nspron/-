using System.Windows; // 引入 WPF 相关命名空间
using System.Windows.Input;
using System.Diagnostics; // 引入用于进程控制的命名空间
using System.IO; // 引入文件操作相关的命名空间
using System.Net.Http; // 引入用于 HTTP 请求的命名空间
using HtmlAgilityPack; // 引入用于 HTML 解析的命名空间
using SharpCompress.Archives; // 引入用于解压缩文件的命名空间
using SharpCompress.Common; // 引入用于解压缩文件的命名空间
using System.Text; // 引入用于字符串操作的命名空间
using System.Windows.Media; // 引入用于界面元素的命名空间
using System.Windows.Controls; // 引入用于界面元素的命名空间

namespace YourNamespace
{
    // 下载窗口类
    public partial class DownloadWindow : Window
    {
        // 构造函数
        public DownloadWindow()
        {
            InitializeComponent(); // 初始化组件
        }

        // 当标题栏被按下时，开始拖动窗口
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) // 如果左键被按下
                DragMove(); // 拖动窗口
        }

        // 取消按钮点击事件处理程序
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close(); // 关闭窗口
        }

        // 处理下载的异步方法
        public static async Task HandleDownloadAsync(string linkText, string linkUrl)
        {
            HttpClient client = new HttpClient(); // 创建 HTTP 客户端实例
            string html = await client.GetStringAsync(linkUrl); // 发送 HTTP 请求并获取 HTML 内容

            HtmlDocument doc = new HtmlDocument(); // 创建 HTML 文档实例
            doc.LoadHtml(html); // 加载 HTML 内容

            // 获取包含指定类名的链接
            var links = doc.DocumentNode.SelectNodes("//a[contains(@class, 'attachment-link')]");
            if (links != null) // 如果找到链接
            {
                StringBuilder allLinksText = new StringBuilder(); // 创建字符串构建器以存储所有链接文本
                // 在同一个窗口中显示所有链接文本
                DownloadWindow downloadWindow = new DownloadWindow(); // 创建下载窗口实例
                downloadWindow.Show(); // 显示窗口
                Grid grid = downloadWindow.View1; // 获取 Grid 元素

                int row = 0; // 初始化行数

                foreach (var a in links) // 遍历找到的链接
                {
                    string href = a.GetAttributeValue("href", ""); // 获取链接 URL
                    string text = a.InnerText; // 获取链接文本
                    Console.WriteLine($"Link: {href}, Text: {text}"); // 输出链接信息到控制台

                    // 创建一个新的 TextBlock 实例
                    TextBlock textBlock = new TextBlock();
                    textBlock.Text = text;

                    // 创建一个新的 RowDefinition 并设置其高度为 Auto
                    RowDefinition rowDefinition = new RowDefinition();
                    rowDefinition.Height = GridLength.Auto;
                    grid.RowDefinitions.Add(rowDefinition);

                    // 将 TextBlock 添加到 Grid 的子元素中
                    grid.Children.Add(textBlock);
                    Grid.SetRow(textBlock, row); // 设置 TextBlock 的行数

                    // 添加点击事件处理程序
                    textBlock.MouseLeftButtonDown += async (sender, e) =>
                    {
                        // 在点击时下载文件
                        await DownloadFileAsync(client, href, text);
                    };

                    // 添加鼠标进入事件处理程序
                    textBlock.MouseEnter += (sender, e) =>
                    {
                        // 当鼠标进入时，改变文字颜色为红色
                        textBlock.Foreground = Brushes.Red;
                    };

                    // 添加鼠标离开事件处理程序
                    textBlock.MouseLeave += (sender, e) =>
                    {
                        // 当鼠标离开时，恢复文字颜色为默认颜色
                        textBlock.Foreground = Brushes.Black; // 你可以根据需要设置为其他颜色
                    };

                    row++; // 增加行数，以便下一个 TextBlock 放在下一行
                }
            }
            else
            {
                Console.WriteLine("No links found in the HTML with class='attachment-link'.");
            }
        }

        // 下载文件的异步方法
        private static async Task DownloadFileAsync(HttpClient client, string url, string fileName)
        {
            // 获取当前目录
            string currentDirectory = Directory.GetCurrentDirectory();
            // 组合得到 tool 目录路径
            string toolDirectory = Path.Combine(currentDirectory, "tool");
            // 在 tool 目录下创建与文件名相同的文件夹路径
            string innerDirectoryPath = Path.Combine(toolDirectory, fileName);

            // 组合得到文件具体路径
            string filePath = Path.Combine(innerDirectoryPath, $"{fileName}.rar");

            // 如果该文件不存在
            if (!File.Exists(filePath))
            {
                using (var response = await client.GetAsync(url)) // 发送 HTTP 请求以下载文件
                {
                    response.EnsureSuccessStatusCode(); // 确保请求成功
                    var content = await response.Content.ReadAsByteArrayAsync(); // 读取文件内容

                    Directory.CreateDirectory(innerDirectoryPath);  // 创建文件夹

                    File.WriteAllBytes(filePath, content); // 将文件内容写入文件
                }
            }

            await ExtractAndOpenFilesAsync(filePath, innerDirectoryPath); // 解压并打开文件
        }
        // 解压并打开文件的异步方法
        private static async Task ExtractAndOpenFilesAsync(string filePath, string directoryPath)
        {
            using (Stream stream = File.OpenRead(filePath)) // 打开文件流
            {
                using (var archive = ArchiveFactory.Open(stream)) // 使用 SharpCompress 打开归档文件
                {
                    foreach (var entry in archive.Entries) // 遍历归档文件中的条目
                    {
                        if (!entry.IsDirectory) // 如果条目不是目录
                        {
                            string destinationPath = Path.Combine(directoryPath, entry.Key); // 构建目标路径

                            entry.WriteToFile(destinationPath, new ExtractionOptions { ExtractFullPath = true, Overwrite = true }); // 将条目写入文件

                            string extension = Path.GetExtension(entry.Key); // 获取文件扩展名
                            string fullPath = Path.Combine(directoryPath, entry.Key); // 获取完整路径

                            if (extension.Equals(".txt", StringComparison.OrdinalIgnoreCase) || // 如果是文本文件或可执行文件
                                extension.Equals(".exe", StringComparison.OrdinalIgnoreCase))
                            {
                                await TryOpenFileAsync(fullPath); // 尝试打开文件
                            }
                        }
                    }
                }
            }
        }

        // 尝试打开文件的异步方法
        private static async Task TryOpenFileAsync(string filePath)
        {
            Console.WriteLine($"Trying to open {filePath}"); // 输出尝试打开文件信息到控制台
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(filePath) // 创建进程启动信息
                {
                    UseShellExecute = true // 使用系统外壳执行
                };
                await Task.Run(() => Process.Start(psi)); // 使用 Task.Run 在后台线程上执行
                Console.WriteLine($"Opened {filePath}"); // 输出打开文件信息到控制台
            }
            catch (Exception ex) // 捕获异常
            {
                Console.WriteLine($"Failed to open {filePath}: {ex.Message}"); // 输出打开文件失败信息到控制台
            }
        }
    }
}
