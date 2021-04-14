namespace TzWatch.Domain

open System
open FSharp.Control
open FSharpx.Collections

type Interest = EntryPoint of string

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
    { UpdateId: UpdateId
      Value: UpdateValue }

and EntryPointCall =
    { Entrypoint: string
      Parameters: string }

and BalanceUpdate = { Former: uint64; Updated: uint64 }

and StorageUpdate = { Diff: string }

and UpdateValue =
    | EntryPointCall of EntryPointCall
    | BalanceUpdate of BalanceUpdate
    | StorageUpdate of StorageUpdate

type EventLog =
    { BlockHeader: BlockHeader
      Updates: Update seq }

type CatchupOptions = { Level: Level; YieldEmpty: Boolean }

module Subscription =
    let private check (interests: Interest list) (operation: Operation) =
        interests
        |> List.fold (fun acc i ->
            acc
            || match i, operation.Kind with
               | EntryPoint v, SmartContractCall ({ Entrypoint = ep }) -> ep = v) false

    let private toUpdate (operation: Operation) =
        match operation.Kind with
        | SmartContractCall { Entrypoint = ep; Parameters = p } ->
            { UpdateId = operation.Id
              Value = EntryPointCall { Entrypoint = ep; Parameters = p } }

    let private applyOperation (s: SubscriptionParameters) (op: OperationGroup) =
        let r =
            op.Operations
            |> Seq.filter (fun o -> o.Destination = (ContractAddress.value s.Contract))
            |> Seq.filter (fun o -> o.Status = OperationStatus.Applied)
            |> Seq.filter (check s.Interests)
            |> Seq.map toUpdate

        r

    let applyBlock (s: SubscriptionParameters) (block: Block): (SubscriptionParameters * EventLog) =
        let updates =
            block.Operations
            |> Seq.map (applyOperation s)
            |> Seq.concat

        let t =
            { BlockHeader =
                  { Level = bigint block.Level
                    Hash = block.Hash
                    Timestamp = block.Timestamp
                    ChainId = block.ChainId }
              Updates = updates }

        s, t


    let run (poller: ISync) (options: CatchupOptions) (subscription: SubscriptionParameters) =
        let polling =
            poller.CatchupFrom options.Level subscription.Confirmations

        let handler = applyBlock subscription

        polling
        |> AsyncSeq.map handler
        |> AsyncSeq.filter (fun (_, u) ->
            (options.YieldEmpty
             || (u.Updates |> Seq.length) > 0))
        |> AsyncSeq.map snd
