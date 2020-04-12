using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Windows.Documents;
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
            _cts = Helpers.Run(raw, UpdateUi );
        }

        void Button_Click(object sender, RoutedEventArgs e) =>             
            Process.Start (new ProcessStartInfo
            {
                FileName = _filePath,
                UseShellExecute = true
            });

        void UpdateUi(IEnumerable<Entry> entries) => Dispatcher?.Invoke(() => dataGrid1.ItemsSource = entries.OrderBy(x => x.Host.AbsoluteUri));

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
                _cts = Helpers.Run(raw, UpdateUi );
            };
        }

        private void EventSetter_OnHandler(object sender, RoutedEventArgs e) =>
            Process.Start (new ProcessStartInfo
            {
                FileName = ((Hyperlink)e.Source).NavigateUri.AbsoluteUri,
                UseShellExecute = true
            });
    }
}
