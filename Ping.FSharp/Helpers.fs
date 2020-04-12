module Helpers
open System.IO 
open System
open System.Net
open Microsoft.FSharp.Control
open System.Linq
open Ping.FSharp
open System.Collections.Concurrent
open System.Collections.Generic
open System.Collections.Generic
open System.Net.Http
open System.Diagnostics
open System.Threading
open System.Threading.Tasks

let NoResponse = {PingResponse.StatusCode = HttpStatusCode.ServiceUnavailable; ResponseTime = -1L}

let ParseHosts (hosts:string[])=
    
    let ToPingableEntries (s:string): KeyValuePair<PingableEntry, PingResponse> =
        KeyValuePair<PingableEntry, PingResponse>(
            {
                PingableEntry.Host = Uri(s.Split(" ").First())
                Name=s.Remove(0, s.IndexOf(' ') + 1)
            },
            NoResponse)
  
    let entries = hosts |> 
                    Array.filter(fun s -> not(String.IsNullOrWhiteSpace(s))) |>
                    Array.map(fun (s:string) -> s.Trim()) |>
                    Array.filter(fun s -> Uri.IsWellFormedUriString(s.Split(" ").First(), UriKind.Absolute)) |>
                    Array.map(ToPingableEntries) |>
                    Seq.distinct 

    ConcurrentDictionary<PingableEntry, PingResponse>(entries)

let Ping(client:HttpClient) = 
    let watch = Stopwatch.StartNew()
    
    try 
        let response = client.GetAsync("", HttpCompletionOption.ResponseHeadersRead).Result;
        {PingResponse.StatusCode = response.StatusCode; ResponseTime = watch.ElapsedMilliseconds}
    with
        | _ -> NoResponse

type UpdateUiDelegate = delegate of (IEnumerable<Entry>) -> unit

let Run(raw:string[], updateUi:UpdateUiDelegate) =
    
    let Project (x:KeyValuePair<PingableEntry, PingResponse>) =
        {
            Entry.Host = x.Key.Host
            Name = x.Key.Name
            StatusCode = x.Value.StatusCode
            ResponseTime = x.Value.ResponseTime
        }

    let entries = ParseHosts raw
    
    let cts = new CancellationTokenSource()
    
    for e in entries do
        Task.Run(fun _ -> 
            let client = new HttpClient(BaseAddress = e.Key.Host)

            while true do 
                cts.Token.ThrowIfCancellationRequested()
                entries.TryUpdate(e.Key, Ping client, entries.[e.Key]) |> ignore
                
                let projected = entries.ToArray() |> Array.map Project
                updateUi.Invoke projected 

                cts.Token.ThrowIfCancellationRequested()
                Thread.Sleep(1000)
            ,cts.Token) |> ignore
    cts
    
    