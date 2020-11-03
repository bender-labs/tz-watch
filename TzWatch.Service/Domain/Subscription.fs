namespace TzWatch.Service.Domain

open FSharpx.Control
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open TzWatch.Service.Domain
open TzWatch.Service.Node.Types
open FSharp.Control

type Interest =
    | EntryPoint of string
    | Balance
    | Storage

type SubscriptionParameters =
    { Contract: ContractAddress
      Interests: Interest list
      Confirmations: int }

type Update =
    { Confirmations: int
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

type Subscription =
    { Parameters: SubscriptionParameters
      Channel: Channel
      PendingOperations: Map<int, UpdateValue seq> }


module Subscription =
    let create parameters channel =
        { Parameters = parameters
          Channel = channel
          PendingOperations = Map.empty }

    let send { Channel = channel } value = channel value

    let private check (interests: Interest list) (operation: JToken) =
        interests
        |> List.fold (fun acc i ->
            acc
            || match i with
               | EntryPoint v ->
                   not (isNull operation.["parameters"])
                   && operation.["parameters"].["entrypoint"].Value<string>() = v
               | _ -> false

            ) false

    let applyBlock (s: Subscription) (block: Block): (Subscription * Update seq) =
        let t =
            block.Operations
            |> Seq.collect (fun o -> o.["contents"])
            |> Seq.filter (fun e -> e.Value("kind") = "transaction")
            |> Seq.filter (fun e -> e.Value("destination") = (ContractAddress.value s.Parameters.Contract))
            |> Seq.filter (fun e -> check s.Parameters.Interests e)
            |> Seq.map
                ((fun e ->
                    EntryPointCall
                        { Entrypoint = e.SelectToken("parameters.entrypoint").ToString()
                          Parameters = e.SelectToken("parameters.value") }))

        if s.Parameters.Confirmations > 1 then
            ({ s with
                   PendingOperations = s.PendingOperations.Add(block.Level, t) },
             Seq.empty)
        else
            (s,
             t
             |> Seq.map (fun u -> { Confirmations = 1; Value = u }))


    let run (subscription: Subscription) (poller: ISync) (level: Level) =
        let polling = poller.CatchupFrom level

        let handler (subscription: Subscription) (block: Block) =
            async {
                let (newState, updates) = applyBlock subscription block
                for e in updates do
                    do! send subscription (JsonConvert.SerializeObject e)

                return newState
            }

        polling
        |> AsyncSeq.foldAsync handler subscription
        |> Async.map ignore
