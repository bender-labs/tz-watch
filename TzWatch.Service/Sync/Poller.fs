module TzWatch.Service.Sync

open System
open System.Reactive.Linq
open FSharp.Control
open FSharp.Control.Reactive

open FSharpx.Control
open Netezos.Rpc
open TzWatch.Service.Domain
open TzWatch.Service.Node.Types

type SyncNode(node: TezosRpc) =

    let obs =
        Observable.create (fun observer ->
            let rec loop (level: int option) =
                async {
                    let! headJson =
                        node.Blocks.Head.Header.GetAsync()
                        |> Async.AwaitTask

                    let head = headJson.ToString() |> Header.Parse

                    match level with
                    | None ->
                        let! block =
                            node.Blocks.Head.Operations.[3].GetAsync()
                            |> Async.AwaitTask

                        observer.OnNext block

                        do! Async.Sleep(TimeSpan.FromSeconds(30.0).Milliseconds)
                    | Some i ->
                        if head.Level > i then
                            let! block =
                                node.Blocks.Head.Operations.[3].GetAsync()
                                |> Async.AwaitTask

                            observer.OnNext block
                        else
                            do! Async.Sleep(TimeSpan.FromSeconds(2.0).Milliseconds)

                    return! loop (Some head.Level)
                }

            let dispose = loop None |> Async.StartDisposable
            dispose.Dispose)

    let obs' =
        let rec pollHead (observer: IObserver<_>) (level: int option) =
            async {
                let! headJson =
                    node.Blocks.Head.Header.GetAsync()
                    |> Async.AwaitTask

                let head = headJson.ToString() |> Header.Parse

                match level with
                | None ->
                    let! block =
                        node.Blocks.Head.Operations.[3].GetAsync()
                        |> Async.AwaitTask

                    observer.OnNext block

                    do! Async.Sleep(TimeSpan.FromSeconds(30.0).Milliseconds)
                | Some i ->
                    if head.Level > i then
                        let! block =
                            node.Blocks.Head.Operations.[3].GetAsync()
                            |> Async.AwaitTask

                        observer.OnNext block
                    else
                        do! Async.Sleep(TimeSpan.FromSeconds(2.0).Milliseconds)

                return! pollHead observer (Some head.Level)
            }

        { new IObservable<_> with
            member x.Subscribe(observer) =
                pollHead observer None |> Async.StartDisposable }


    interface ISync with
        member this.Head =
            let published = obs |> Observable.Publish
            published.Connect() |> ignore
            published


        member this.From(level: int) =
            let rec loop (current: int) =
                asyncSeq {
                    let! head =
                        node.Blocks.Head.Header.GetAsync()
                        |> Async.AwaitTask

                    let header = Header.Parse(head.ToString())

                    let! value =
                        node.Blocks.[current].Operations.[3].GetAsync()
                        |> Async.AwaitTask

                    yield value

                    if current < header.Level then yield! loop (current + 1)
                }

            loop (level)

        member this.CatchupFrom(level: Level) =
            let rec loop (current: Level) =
                asyncSeq {
                    let! head =
                        node.Blocks.Head.Header.GetAsync()
                        |> Async.AwaitTask

                    let header = Header.Parse(head.ToString())
                    
                    let actualLevel =
                        match current with
                        | Height i -> i
                        | Head -> header.Level
                    if actualLevel > header.Level then
                        do! Async.Sleep(TimeSpan.FromSeconds(30.0).Milliseconds)
                        yield! loop(Height actualLevel)
                    else     
                        let! value = node.Blocks.[actualLevel].Operations.[3].GetAsync() |> Async.AwaitTask
                        yield {Level = actualLevel; Operations = value}
                    yield! loop(Height (actualLevel + 1))
                }

            loop (level)
