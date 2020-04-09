using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
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

        CancellationTokenSource Run(ConcurrentDictionary<Uri, PingableEntry> entries)
        {
            var cts = new CancellationTokenSource();
            foreach (var (uri, entry) in entries)
            {
                Task.Run(() => 
                {
                    while (true)
                    {
                        cts.Token.ThrowIfCancellationRequested();

                        var pingResponse = PingHostWithHttpClient(entry);

                        entries[uri].ResponseTime = pingResponse.ResponseTime;
                        entries[uri].StatusCode = pingResponse.StatusCode;

                        Dispatcher.Invoke(() =>
                        {
                            dataGrid1.ItemsSource = entries.Values.OrderBy(x => x.Host.AbsoluteUri);
                        });

                        cts.Token.ThrowIfCancellationRequested();
                        Thread.Sleep(2000);
                    }
                }, cts.Token);
            }

            return cts;
        }

        async Task<ConcurrentDictionary<Uri, PingableEntry>> ReadAsync()
        {
            var raw = await File.ReadAllLinesAsync(Path.Combine(Environment.CurrentDirectory, "hosts.txt"));
            var entries = raw.Where(s => !string.IsNullOrWhiteSpace(s)).Select(x => new PingableEntry(
                new Uri(x.Split(" ").First()), 
                x.Remove(0, x.IndexOf(' ') + 1)));

            return new ConcurrentDictionary<Uri, PingableEntry>(entries.ToDictionary(
                x => x.Host,
                x => x)); 
        }

        PingResponse PingHostWithHttpClient(PingableEntry entry)
        {
            var watch = Stopwatch.StartNew();

            try
            {
                var response = entry.Client.GetAsync("", HttpCompletionOption.ResponseHeadersRead).Result;
                return new PingResponse {ResponseTime = watch.ElapsedMilliseconds, StatusCode = response.StatusCode};
            }
            catch (Exception)
            {
                return new PingResponse { ResponseTime = watch.ElapsedMilliseconds};
            }
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
