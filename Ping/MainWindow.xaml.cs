using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using Ping.FSharp;
using Path = System.IO.Path;

namespace Ping
{
    public partial class MainWindow : Window
    {
        CancellationTokenSource _cts = new CancellationTokenSource();
        readonly string _filePath = Path.Combine(Environment.CurrentDirectory, "hosts.txt");

        public MainWindow()
        {
            InitializeComponent();
            Watch();
        }

        async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var raw = await File.ReadAllLinesAsync(_filePath);
            var entries = Helpers.ParseHosts(raw);
            _cts = Helpers.Run(entries, UpdateUi );
        }

        void Button_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("notepad.exe", _filePath);
        }

        object UpdateUi(ConcurrentDictionary<PingableEntry, PingResponse> entries)
        {
            return Dispatcher.Invoke(() => dataGrid1.ItemsSource = entries.OrderBy(x => x.Key.Host.AbsoluteUri).Select(x => new
            {
                x.Key.Host,
                x.Key.Name,
                x.Value.StatusCode,
                x.Value.ResponseTime
            }));
        }

        void Watch()
        {
            var watch = new FileSystemWatcher
            {
                Path = Environment.CurrentDirectory,
                Filter = "hosts.txt",
                NotifyFilter = NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };

            watch.Changed += async (o, args) =>
            {
                if (args.FullPath != _filePath) return;

                _cts.Cancel();
                var raw = await File.ReadAllLinesAsync(_filePath);
                var entries = Helpers.ParseHosts(raw);
                _cts = Helpers.Run(entries, UpdateUi );
            };
        }
    }
}
