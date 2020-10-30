namespace TzWatch.Service.Domain

open Newtonsoft.Json.Linq
open TzWatch.Service.Node.Types
open FSharp.Control

type Filter = Operation -> bool    

type SubscriptionParameters =
    private { Contract: ContractAddress
              Confirmations: int
              Channel: Channel
              Level: Level
              Filters: Filter list }

type Subscription =
    private { Parameters: SubscriptionParameters
              mutable Level: int }


module Subscription =
    let create contract level channel =
        let parameters =
            { Contract = contract
              Confirmations = 30
              Channel = channel
              Level = level
              Filters = [] }

        Ok { Parameters = parameters; Level = 0 }


    let value { Contract = contract; Level = level; Confirmations = confirmations } =
        {| contract = contract
           level = level
           confirmations = confirmations |}

    let send { Channel = channel } value = channel value

    let ``process`` (s: Subscription) (o: JToken) = s.Parameters.Channel(o.ToString())

    let level { Parameters = parameters } = parameters.Level

    let witchLevel subscription level =
        subscription.Level <- level
        subscription

    let run (subscription: Subscription) (poller: Sync) =
        async {
            let handler = ``process`` subscription
            match subscription.Parameters.Level with
            | Height i -> do! poller.From i |> AsyncSeq.iterAsync handler
            | _ -> ()
            poller.Head.Subscribe(fun e -> (handler e) |> Async.StartImmediate)
        }
