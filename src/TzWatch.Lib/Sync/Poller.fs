module TzWatch.Sync

open System
open System.IO
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
                            |> Async.map (fun v -> OperationGroupParser.parse (new StringReader(v.ToString())))

                        let! blockHeader =
                            node.Blocks.[actualLevel].Header.GetAsync()
                            |> Async.AwaitTask

                        let blockHeader = Header.Parse(blockHeader.ToString())

                        yield
                            { Level = blockHeader.Level
                              Hash = blockHeader.Hash
                              ChainId = blockHeader.ChainId
                              Timestamp = blockHeader.Timestamp
                              Operations = value }

                    yield! loop (Height(actualLevel + 1))
                }

            loop (level)
