open System.Text
open System.Threading
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Netezos.Rpc
open TzWatch.Service.Program
open TzWatch.Service.Sync
open TzWatch.Service.Domain
open Giraffe
open FSharp.Control.Tasks.V2.ContextInsensitive


let wait handle =
    let tsc = TaskCompletionSource<unit>()
    ThreadPool.RegisterWaitForSingleObject(handle, (fun _ _ -> tsc.SetResult()), null, -1, true)
    |> ignore
    tsc.Task

let channel (ctx: HttpContext) (str: string) =
    task {
        let bytes = Encoding.UTF8.GetBytes str
        do! ctx.Response.Body.WriteAsync(bytes, 0, bytes.Length)
        do! ctx.Response.Body.FlushAsync()
        ()
    }
    |> Async.AwaitTask


let subscribeHandler (mainLoop: MainLoop) (_: HttpFunc) (ctx: HttpContext) =
    task {
        let channel = channel ctx

        do! ctx.Response.Body.FlushAsync()
        mainLoop.Send
            (Subscribe
                ({ Address = "KT1VzsDKqm3pmHH6S85LvWUBeBRs6kLswQKe"
                   Level = Some 83510
                   Channel = channel
                   Confirmations = 3
                   Interests =
                       [ (EntryPoint "mint")
                         (EntryPoint "burn") ] }))
        do! wait ctx.RequestAborted.WaitHandle
        // todo cancel sub
        return Some ctx
    }

let webApp (mainLoop: MainLoop) =
    choose [ route "/test"
             >=> setHttpHeader "Content-Type" "text/event-stream"
             >=> subscribeHandler mainLoop
             route "/ping" >=> text "pong" ]

let configureApp (app: IApplicationBuilder) =
    use rpc =
        new TezosRpc("https://delphinet-tezos.giganode.io")

    let sync = SyncNode rpc

    let logger =
        app.ApplicationServices.GetService<ILogger<MainLoop>>()

    let mainLoop = MainLoop(sync, logger)
    
    app.UseGiraffe(webApp mainLoop)


let configureServices (services: IServiceCollection) =
    // Add Giraffe dependencies
    services.AddGiraffe() |> ignore


[<EntryPoint>]
let main argv =
    Host.CreateDefaultBuilder()
        .ConfigureWebHostDefaults(fun webHostBuilder ->
        webHostBuilder.Configure(configureApp).ConfigureServices(configureServices)
        |> ignore).Build().Run()
    0
