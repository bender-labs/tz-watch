namespace TzWatch.Service.Domain

open FSharpx.Control
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open TzWatch.Service.Domain
open TzWatch.Service.Node.Types
open FSharp.Control

type Filter = Operation -> bool


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
      PendingOperations: Map<int, Operation List>
      Level: int }


module Subscription =
    let create contract level channel =
        let parameters =
            { Contract = contract
              Confirmations = 30
              Interests = [] }

        Ok
            { Parameters = parameters
              Channel = channel
              Level = 0
              PendingOperations = Map.empty }

    let send { Channel = channel } value = channel value

    let applyBlock (s: Subscription) (level: int) (block: JToken): (Subscription * Update seq) =
        let t =
            block
            |> Seq.collect (fun o -> o.["contents"])
            |> Seq.filter (fun e -> e.Value("kind") = "transaction")
            |> Seq.filter (fun e -> e.Value("destination") = (ContractAddress.value s.Parameters.Contract))
            |> Seq.filter (fun e -> not (isNull e.["parameters"]))
            |> Seq.map
                ((fun e ->
                    EntryPointCall
                        { Entrypoint = e.SelectToken("parameters.entrypoint").ToString()
                          Parameters = e.SelectToken("parameters.value") })
                 >> (fun u -> { Confirmations = 1; Value = u }))

        (s, t)


    let run (subscription: Subscription) (poller: ISync) (level: Level) =
        let polling: JToken AsyncSeq = poller.CatchupFrom level

        let handler (subscription: Subscription) (block: JToken) =
            async {
                let (newState, updates) = applyBlock subscription 1 block
                for e in updates do
                    do! send subscription (JsonConvert.SerializeObject e)

                return newState
            }

        polling
        |> AsyncSeq.foldAsync handler subscription
        |> Async.map ignore
