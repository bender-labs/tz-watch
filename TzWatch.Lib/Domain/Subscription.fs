namespace TzWatch.Domain

open System
open Newtonsoft.Json.Linq
open FSharp.Control
open FSharpx.Collections

type Interest =
    | EntryPoint of string
    | Balance
    | Storage

type SubscriptionParameters =
    { Contract: ContractAddress
      Interests: Interest list
      Confirmations: uint }

type BlockHeader =
    { Level: bigint
      Hash: string
      Timestamp: DateTimeOffset
      ChainId: string }

type Update =
    { OperationHash: string
      Value: UpdateValue }

and EntryPointCall =
    { Entrypoint: string
      Parameters: JToken }

and BalanceUpdate = { Former: uint64; Updated: uint64 }

and StorageUpdate = { Diff: JToken }

and UpdateValue =
    | EntryPointCall of EntryPointCall
    | BalanceUpdate of BalanceUpdate
    | StorageUpdate of StorageUpdate

type EventLog =
    { BlockHeader: BlockHeader
      Updates: Update seq }

module Subscription =
    let private check (interests: Interest list) (operation: JToken) =
        interests
        |> List.fold (fun acc i ->
            acc
            || match i with
               | EntryPoint v ->
                   not (isNull operation.["parameters"])
                   && operation.["parameters"].["entrypoint"]
                       .Value<string>() = v
               | _ -> false

            ) false

    let private toUpdate hash (operation: JToken) =
        { OperationHash = hash
          Value =
              EntryPointCall
                  { Entrypoint =
                        operation
                            .SelectToken("parameters.entrypoint")
                            .Value<string>()
                    Parameters = operation.SelectToken("parameters.value") } }

    let applyBlock (s: SubscriptionParameters) (block: Block): (SubscriptionParameters * EventLog) option =
        let updates =
            block.Operations
            |> Seq.collect (fun o ->
                o.["contents"]
                |> Seq.map (fun e -> (o.["hash"].Value<string>(), e))
                |> Seq.append
                    (o.["contents"]
                     |> Seq.collect (fun c ->
                         let internals =
                             c.["metadata"].["internal_operation_results"]

                         if not (isNull internals) then internals else JArray() :> JToken)
                     |> Seq.map (fun i -> (o.["hash"].Value<string>(), i))))
            |> Seq.filter (fun (_, e) -> e.Value("kind") = "transaction")
            |> Seq.filter (fun (_, e) -> e.Value("destination") = (ContractAddress.value s.Contract))
            |> Seq.filter (fun (_, e) -> check s.Interests e)
            |> Seq.map ((fun (h, e) -> toUpdate h e))

        if updates |> Seq.length > 0 then
            let t =
                { BlockHeader =
                      { Level = bigint block.Level
                        Hash = block.Hash
                        Timestamp = block.Timestamp
                        ChainId = block.ChainId }
                  Updates = updates }

            Some(s, t)
        else
            None


    let run (poller: ISync) (level: Level) (subscription: SubscriptionParameters) =
        let polling =
            poller.CatchupFrom level subscription.Confirmations

        let handler = applyBlock subscription

        polling
        |> AsyncSeq.map handler
        |> AsyncSeq.choose id
        |> AsyncSeq.map snd
