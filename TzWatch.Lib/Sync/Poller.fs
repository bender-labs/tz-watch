module TzWatch.Sync

open System
open FSharp.Control
open FSharpx.Control
open Netezos.Rpc
open TzWatch.Domain
open TzWatch.Node.Types


type SyncNode(node: TezosRpc, chainId: string) =

    interface ISync with

        member this.CatchupFrom (level: Level) (confirmations: uint) =
            let rec loop (current: Level) =
                asyncSeq {
                    let! head =
                        node.Blocks.Head.Header.GetAsync()
                        |> Async.AwaitTask


                    let header = Header.Parse(head.ToString())
                    if header.ChainId <> chainId then failwith "Invalid Chain"
                    let headLevel = header.Level

                    let actualLevel =
                        match current with
                        | Height i -> i
                        | Head -> headLevel

                    if actualLevel + int confirmations > headLevel then
                        do! Async.Sleep(TimeSpan.FromSeconds(30.0))
                        yield! loop (Height actualLevel)
                    else
                        let! value =
                            node.Blocks.[actualLevel].Operations.[3]
                                .GetAsync()
                            |> Async.AwaitTask

                        yield
                            { Level = actualLevel
                              Operations = value }

                    yield! loop (Height(actualLevel + 1))
                }

            loop (level)
