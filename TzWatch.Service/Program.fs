open System
open System.Threading.Tasks
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Netezos.Rpc
open TzWatch.Service.Program
open TzWatch.Service.Sync

open TzWatch.Service.Node

type Worker(logger: ILogger<Worker>, subLogger: ILogger<MainLoop>) =
    inherit BackgroundService()


    override bs.ExecuteAsync stoppingToken =
        use rpc =
            new TezosRpc("https://delphinet-tezos.giganode.io")

        let sync = SyncNode rpc
        
        let mainLoop = MainLoop(sync, subLogger)
        mainLoop.Send
            (Subscribe
                ({ Address = "KT1L75tfiRvokyYF9bmw2CrTHtyVDTYqftLa"
                   Level = Some 145748
                   Confirmations = 3 }))

        let f: Async<unit> =
            async {
                while not stoppingToken.IsCancellationRequested do
                    do! Async.Sleep(1000)
            }

        Async.StartAsTask(computation = f, cancellationToken = stoppingToken) :> Task

let CreateHostBuilder argv: IHostBuilder =
    let builder = Host.CreateDefaultBuilder(argv)
    builder.UseWindowsService()
           .ConfigureServices(fun hostContext services ->
           services.AddHostedService<Worker>()
           |> ignore<IServiceCollection>)



[<EntryPoint>]
let main argv =
    let hostBuilder = CreateHostBuilder argv
    hostBuilder.Build().Run()
    0 // return an integer exit code
