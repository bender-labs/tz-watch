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

type OperationId = { OpgHash: string; Counter: int }

type UpdateId =
    | Operation of OperationId
    | InternalOperation of OperationId * int

type Update =
    { UpdateId: UpdateId
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

    let private toUpdate (id, (operation: JToken)) =
        { UpdateId = id
          Value =
              EntryPointCall
                  { Entrypoint =
                        operation
                            .SelectToken("parameters.entrypoint")
                            .Value<string>()
                    Parameters = operation.SelectToken("parameters.value") } }

    let private applyOperation (s: SubscriptionParameters) (op: JToken) =
        let isOperationValid (e: JToken) =
            e
                .SelectToken("metadata.operation_result.status")
                .Value<string>() = "applied"
            && e.Value("kind") = "transaction"

        let isInternalOpValid (e: JToken) =
            e.SelectToken("result.status").Value<string>() = "applied"
            && e.Value("kind") = "transaction"

        let hash: string = op.Value("hash")

        let operations =
            op.["contents"]
            |> Seq.map (fun  e -> ({ OpgHash = hash; Counter = e.Value("counter") }, e))
            |> Seq.filter (fun (_, e) -> isOperationValid e)

        let internalOperations =
            operations
            |> Seq.collect (fun (index, e) ->
                let internals =
                    e.["metadata"].["internal_operation_results"]

                let r =
                    if not (isNull internals) then internals else JArray() :> JToken

                r
                |> Seq.map (fun  e -> (InternalOperation(index, e.Value("nonce")), e)))
            |> Seq.filter (fun (_, e) -> isInternalOpValid e)

        let operations =
            operations
            |> Seq.map (fun (i, e) -> (Operation i, e))

        let r =
            Seq.append operations internalOperations
            |> Seq.filter (fun (_, e) -> e.Value("destination") = (ContractAddress.value s.Contract))
            |> Seq.filter (fun (_, e) -> check s.Interests e)
            |> Seq.map toUpdate

        r

    let applyBlock (s: SubscriptionParameters) (block: Block): (SubscriptionParameters * EventLog) option =
        let updates =
            block.Operations
            |> Seq.map (applyOperation s)
            |> Seq.concat

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
