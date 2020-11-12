open System.Text
open System.Threading
open System.Threading.Tasks
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Giraffe.EndpointRouting
open Netezos.Rpc
open TzWatch.Service.Adapters.Json
open TzWatch.Service.Program
open TzWatch.Service.Sync
open TzWatch.Service.Domain
open FSharp.Control.Tasks.V2.ContextInsensitive


let wait handle =
    let tsc = TaskCompletionSource<unit>()

    ThreadPool.RegisterWaitForSingleObject(handle, (fun _ _ -> tsc.SetResult()), null, -1, true)
    |> ignore

    tsc.Task


let channel (ctx: HttpContext) (update: Update) =
    task {
        let str =
            updateToJson update |> Encoding.UTF8.GetBytes

        do! ctx.Response.Body.WriteAsync(str, 0, str.Length)
        do! ctx.Response.Body.FlushAsync()
        ()
    }
    |> Async.AwaitTask


let pocJson (payload: SubscribeDto) (_: HttpFunc) (ctx: HttpContext) =
    task {
        let channel = channel ctx
        let mainLoop = ctx.GetService<MainLoop>()
        do! ctx.Response.Body.FlushAsync()
        let! subscriptionId = mainLoop.Subscribe(toSubscribe payload channel)
        do! wait ctx.RequestAborted.WaitHandle
        mainLoop.CancelSubscription subscriptionId
        return Some ctx
    }

let endpoints =
    [ GET => route "/ping" (text "pong")
      POST
      => route
          "/subscriptions"
             (setHttpHeader "Content-Type" "text/event-stream"
              >=> (bindJson<SubscribeDto> pocJson)) ]


let configureApp (app: IApplicationBuilder) =
    app
        .UseRouting()
        .UseEndpoints(fun e -> e.MapGiraffeEndpoints endpoints)
    |> ignore


type IServiceCollection with
    member this.AddTzWatch(configuration: IConfiguration) =
        let host = configuration.["TezosNode:Endpoint"]

        this
            .AddSingleton<TezosRpc>(fun c -> new TezosRpc(host))
            .AddSingleton<ISync, SyncNode>()
            .AddSingleton<MainLoop, MainLoop>()


let configureServices (hostContext: WebHostBuilderContext) (services: IServiceCollection) =
    services
        .AddTzWatch(hostContext.Configuration)
        .AddRouting()
        .AddGiraffe()
    |> ignore


[<EntryPoint>]
let main args =
    WebHost
        .CreateDefaultBuilder(args)
        .UseKestrel()
        .Configure(configureApp)
        .ConfigureServices(configureServices)
        .Build()
        .Run()

    0
