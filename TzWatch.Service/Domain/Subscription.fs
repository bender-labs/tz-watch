namespace TzWatch.Service.Domain

open FSharpx.Control
open Newtonsoft.Json.Linq
open TzWatch.Service.Domain
open FSharp.Control
open FSharpx.Collections

type Interest =
    | EntryPoint of string
    | Balance
    | Storage

type SubscriptionParameters =
    { Contract: ContractAddress
      Interests: Interest list
      Confirmations: int }

type Update =
    { Level: int
      Hash: string
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

type Channel = Update -> Async<Unit>

type Subscription =
    { Parameters: SubscriptionParameters
      Channel: Channel
      PendingOperations: Map<int, Update seq> }


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

    let private toUpdate level hash (operation: JToken) =
        { Level = level
          Hash = hash
          Value =
              EntryPointCall
                  { Entrypoint = operation.SelectToken("parameters.entrypoint").Value<string>()
                    Parameters = operation.SelectToken("parameters.value") } }

    let applyBlock (s: Subscription) (block: Block): (Subscription * Update seq) =
        let t =
            block.Operations
            |> Seq.collect (fun o ->
                o.["contents"]
                |> Seq.map (fun e -> (o.["hash"].Value<string>(), e)))
            |> Seq.filter (fun (_, e) -> e.Value("kind") = "transaction")
            |> Seq.filter (fun (_, e) -> e.Value("destination") = (ContractAddress.value s.Parameters.Contract))
            |> Seq.filter (fun (_, e) -> check s.Parameters.Interests e)
            |> Seq.map ((fun (h, e) -> toUpdate block.Level h e))

        if s.Parameters.Confirmations > 1 then
            let levelToSend =
                block.Level - s.Parameters.Confirmations + 1

            let updates =
                s.PendingOperations
                |> Map.findOrDefault levelToSend Seq.empty

            ({ s with
                   PendingOperations = s.PendingOperations.Add(block.Level, t).Remove(levelToSend) },
             updates)
        else
            (s, t)


    let run (subscription: Subscription) (poller: ISync) (level: Level) =
        let polling = poller.CatchupFrom level

        let handler (subscription: Subscription) (block: Block) =
            async {
                let (newState, updates) = applyBlock subscription block
                for e in updates do
                    do! send subscription e

                return newState
            }

        polling
        |> AsyncSeq.foldAsync handler subscription
        |> Async.map ignore
