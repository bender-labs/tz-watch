namespace TzWatch.Domain

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

    let private toUpdate level hash (operation: JToken) =
        { Level = level
          Hash = hash
          Value =
              EntryPointCall
                  { Entrypoint =
                        operation
                            .SelectToken("parameters.entrypoint")
                            .Value<string>()
                    Parameters = operation.SelectToken("parameters.value") } }

    let applyBlock (s: SubscriptionParameters) (block: Block): (SubscriptionParameters * Update seq) =
        let t =
            block.Operations
            |> Seq.collect (fun o ->
                o.["contents"]
                |> Seq.map (fun e -> (o.["hash"].Value<string>(), e)))
            |> Seq.filter (fun (_, e) -> e.Value("kind") = "transaction")
            |> Seq.filter (fun (_, e) -> e.Value("destination") = (ContractAddress.value s.Contract))
            |> Seq.filter (fun (_, e) -> check s.Interests e)
            |> Seq.map ((fun (h, e) -> toUpdate block.Level h e))

        (s, t)


    let run (poller: ISync) (level: Level) (subscription: SubscriptionParameters) =
        let polling =
            poller.CatchupFrom level subscription.Confirmations

        let handler =
            applyBlock subscription
            >> (fun (_, u) -> AsyncSeq.ofSeq u)

        polling |> AsyncSeq.collect handler
