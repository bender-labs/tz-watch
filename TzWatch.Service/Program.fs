open System
open System.Net.Http
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
    ThreadPool.RegisterWaitForSingleObject(handle, (fun _ _ -> tsc.SetResult()), null, -1 , true)
    |> ignore
    tsc.Task


let subscribeHandler (mainLoop: MainLoop) (_: HttpFunc) (ctx: HttpContext) =
    task {
        let channel str =
            task {
                let! _ = ctx.WriteStringAsync(str)
                ()
            }
            |> Async.AwaitTask

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
        System.Console.WriteLine "done"
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
    (*mainLoop.Send
        (Subscribe
            ({ Address = "KT1VzsDKqm3pmHH6S85LvWUBeBRs6kLswQKe"
               Level = Some 83510
               Confirmations = 3
               Interests =
                   [ (EntryPoint "mint")
                     (EntryPoint "burn") ] }))*)
    // Add Giraffe to the ASP.NET Core pipeline
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
