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

        public MainWindow()
        {
            InitializeComponent();
            Watch();
        }

        async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _cts = Run(await ReadAsync());
        }

        void Button_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("notepad.exe", Path.Combine(Environment.CurrentDirectory, "hosts.txt"));
        }

        CancellationTokenSource Run(ConcurrentDictionary<PingableEntry, PingResponse> entries)
        {
            var cts = new CancellationTokenSource();
            foreach (var (entry, _) in entries)
            {
                Task.Run(() =>
                {
                    var httpClient = new HttpClient {BaseAddress = entry.Host};
                    while (true)
                    {
                        cts.Token.ThrowIfCancellationRequested();

                        entries[entry] = Helpers.Ping(httpClient);
                       
                        Dispatcher.Invoke(() =>
                        {
                            dataGrid1.ItemsSource = entries.Select(x => new
                            {
                                x.Key.Host,
                                x.Key.Name,
                                x.Value.StatusCode,
                                x.Value.ResponseTime
                            }).OrderBy(x => x.Host);
                        });

                        cts.Token.ThrowIfCancellationRequested();
                        Thread.Sleep(2000);
                    }
                }, cts.Token);
            }

            return cts;
        }

        async Task<ConcurrentDictionary<PingableEntry, PingResponse>> ReadAsync()
        {
            var raw = await File.ReadAllLinesAsync(Path.Combine(Environment.CurrentDirectory, "hosts.txt"));
            return Helpers.ParseHosts(raw);
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

            watch.Changed += (o, args) =>
            {
                if (args.FullPath != Path.Combine(Environment.CurrentDirectory, "hosts.txt")) return;

                _cts.Cancel();
                _cts = Run(ReadAsync().Result);
            };
        }
    }
}
