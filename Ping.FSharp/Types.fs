namespace Ping.FSharp

open System.Net
open System

type PingResponse = 
    { ResponseTime:int64;  StatusCode:HttpStatusCode}

type PingableEntry = 
    { Host:Uri;  Name:string}
    
    
        
    

