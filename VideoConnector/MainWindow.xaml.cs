using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Path = System.IO.Path;

namespace VideoConnector
{
    public partial class MainWindow : Window
    {
        private readonly OpenFileDialog addDialog;
        private readonly SaveFileDialog saveDialog;

        public MainWindow()
        {
            InitializeComponent();


            addDialog = new OpenFileDialog
            {
                InitialDirectory = Directory.GetCurrentDirectory(),
                RestoreDirectory = true,
                Multiselect = true,
                Title = "请选择要添加的视频文件"
            };

            saveDialog = new SaveFileDialog
            {
                FileName = "output",
                DefaultExt = ".mp4",
                Filter = "H264 Videos (.mp4)|*.mp4",
                InitialDirectory = Directory.GetCurrentDirectory(),
                RestoreDirectory = true,
                Title = "请选择输出文件"
            };


            buttonVideoAdd.Click += (sender, e) =>
            {
                if (addDialog.ShowDialog() ?? false)
                {
                    foreach (var filename in addDialog.FileNames)
                    {
                        listFiles.Items.Add(filename);
                    }
                }
            };

            buttonVideoRemove.Click += (sender, e) =>
            {
                if (listFiles.SelectedItems?.Count > 0)
                {
                    while (listFiles.SelectedIndex != -1)
                    {
                        listFiles.Items.RemoveAt(listFiles.SelectedIndex);
                    }
                }
            };

            buttonVideoUp.Click += (sender, e) =>
            {
                if (listFiles.SelectedItems?.Count > 0)
                {
                    var selectedIndex = listFiles.SelectedIndex;

                    if (selectedIndex > 0)
                    {
                        var item = listFiles.Items[selectedIndex];
                        listFiles.Items.RemoveAt(selectedIndex);
                        listFiles.Items.Insert(selectedIndex - 1, item);
                        listFiles.SelectedIndex = selectedIndex - 1;
                    }
                }
            };

            buttonVideoDown.Click += (sender, e) =>
            {
                if (listFiles.SelectedItems?.Count > 0)
                {
                    var selectedIndex = listFiles.SelectedIndex;

                    if (selectedIndex != -1 && selectedIndex < listFiles.Items.Count - 1)
                    {
                        var item = listFiles.Items[selectedIndex];
                        listFiles.Items.RemoveAt(selectedIndex);
                        listFiles.Items.Insert(selectedIndex + 1, item);
                        listFiles.SelectedIndex = selectedIndex + 1;
                    }
                }
            };

            buttonSelectPath.Click += (sender, e) =>
            {
                if (saveDialog.ShowDialog() ?? false)
                {
                    textPath.Text = saveDialog.FileName;
                }
            };

            buttonStart.Click += async (sender, e) =>
            {
                if (listFiles.Items.Count <= 0)
                {
                    SetProgress(0, "请选择文件");
                    return;
                }
                if (string.IsNullOrWhiteSpace(textPath.Text))
                {
                    SetProgress(0, "请指定输出目录");
                    return;
                }

                await RunAsync();

            };

            SetProgress(0, "就绪, 等待操作");
        }

        private void SetProgress(double progress, string message)
        {
            progressBar.Value = progress;
            textProgress.Text = message;
            LogDebugInfo(message);
        }

        private void LogDebugInfo(string info)
        {
            textDebug.Text += $"{info}{Environment.NewLine}";
            textDebug.ScrollToEnd();
        }

        private async Task RunAsync()
        {
            var items = listFiles.Items.OfType<string>().ToList();
            var outpath = textPath.Text;

            SetProgress(1, "正在创建临时文件夹");
            var tempFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            LogDebugInfo($"临时文件夹: {tempFolder}");
            Directory.CreateDirectory(tempFolder);


            var ffmpeg = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "lib", "ffmpeg.exe");
            var filelistBuilder = new StringBuilder();

            for (int i = 0; i < items.Count; i++)
            {
                SetProgress(1 + 59 * i / (double)items.Count, $"正在转换第{i}/{items.Count}个视频");

                var filename = Path.Combine(tempFolder, $"{i}.mp4");
                filelistBuilder.AppendLine($"file '{filename}'");

                var subprocess = Process.Start(ffmpeg, $"-i '{items[i]}' -af apad -c:v copy -c:a aac -b:a 256k -shortest '{filename}'");
                await subprocess.WaitForExitAsync();
            }

            var filelistPath = Path.Combine(tempFolder, "filelist");
            File.WriteAllText(filelistPath, filelistBuilder.ToString());

            var combineProcess = Process.Start(ffmpeg, $"-f concat -safe 0 -i '{filelistPath}' -c copy '{outpath}'");
            await combineProcess.WaitForExitAsync();

            SetProgress(99, "正在清理临时文件");
            Directory.Delete(tempFolder, recursive: true);

            SetProgress(100, "任务已完成");
        }
    }
}
