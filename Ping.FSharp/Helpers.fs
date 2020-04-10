module Helpers
open System.IO 
open System
open System.Net
open Microsoft.FSharp.Control
open System.Linq
open Ping.FSharp
open System.Collections.Concurrent
open System.Net.Http
open System.Diagnostics

let ParseHosts (hosts:string[])= 
    let entries = hosts |> 
                    Array.filter(fun s -> not(String.IsNullOrWhiteSpace(s))) |> 
                    Array.map(fun (x:string) -> { 
                        PingableEntry.Host=new Uri(x.Split(" ").First()); 
                        Name=x.Remove(0, x.IndexOf(' ') + 1)})

    let dic = entries.ToDictionary((fun x -> x), 
                fun _ -> {PingResponse.StatusCode = HttpStatusCode.ServiceUnavailable; ResponseTime = Int64.MinValue})
    new ConcurrentDictionary<PingableEntry, PingResponse>(dic)

let Ping(client:HttpClient) = 
    let watch = Stopwatch.StartNew()
    
    try 
        let response = client.GetAsync("", HttpCompletionOption.ResponseHeadersRead).Result;
        {PingResponse.StatusCode = response.StatusCode; ResponseTime = watch.ElapsedMilliseconds}
    with
        | _ -> {PingResponse.StatusCode = HttpStatusCode.ServiceUnavailable; ResponseTime = Int64.MinValue}