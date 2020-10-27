module TzWatch.Service.Sync

open System
open System.Reactive.Linq
open System.Security.Cryptography.X509Certificates
open FSharp.Control
open FSharp.Control.Reactive

open FSharpx.Control
open Netezos.Rpc
open TzWatch.Service.Model
open TzWatch.Service.Node.Types

type SyncNode(node: TezosRpc) =

    let obs =
        Observable.create (fun observer () ->
            let rec loop (level: int option) =
                async {
                    let! headJson = node.Blocks.Head.GetAsync() |> Async.AwaitTask
                    let head = headJson.ToString() |> Header.Parse

                    match level with
                    | None ->
                        let! block =
                            node.Blocks.Head.Operations.[3].GetAsync()
                            |> Async.AwaitTask

                        observer.OnNext block

                        do! Async.Sleep(TimeSpan.FromSeconds(30.0).Milliseconds)
                    | Some i ->
                        if head.Level = i then
                            let! block =
                                node.Blocks.Head.Operations.[3].GetAsync()
                                |> Async.AwaitTask

                            observer.OnNext block
                        else
                            do! Async.Sleep(TimeSpan.FromSeconds(2.0).Milliseconds)

                    return! loop (Some head.Level)
                }

            loop None |> ignore)

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


    interface Sync with
        member this.Head =
            let published = obs' |> Observable.Publish
            published.Connect() |> ignore
            published


        member this.From(level: int) =
            asyncSeq {
                let mutable finish = false
                let mutable current = level
                while not finish do
                    let! head =
                        node.Blocks.Head.Header.GetAsync()
                        |> Async.AwaitTask

                    let header = Header.Parse(head.ToString())

                    let! value =
                        node.Blocks.[current].Operations.[3].GetAsync()
                        |> Async.AwaitTask

                    yield value

                    finish <- current = header.Level
                    current <- current + 1
            }




let poll (node: TezosRpc) (level: Level) =
    asyncSeq {
        let! head =
            node.Blocks.Head.Header.GetAsync()
            |> Async.AwaitTask

        let header = Header.Parse(head.ToString())

        match level with
        | Height i ->
            for curr in seq { i .. header.Level } do
                let! value =
                    node.Blocks.[i].Operations.[3].GetAsync()
                    |> Async.AwaitTask

                yield value
        | Head ->
            let! value =
                node.Blocks.Head.Operations.[3].GetAsync()
                |> Async.AwaitTask

            yield value
    }
