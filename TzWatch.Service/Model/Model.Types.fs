namespace TzWatch.Service.Model

open Newtonsoft.Json.Linq
open TzWatch.Service.Model
open TzWatch.Service.Node.Types

type ContractAddress = private ContractAddress of string


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

        { Parameters = parameters; Level = 0 }


    let value { Contract = contract; Level = level; Confirmations = confirmations } =
        {| contract = contract
           level = level
           confirmations = confirmations |}

    let (|Record|) { Contract = contract; Level = level; Confirmations = confirmations } =
        struct (contract, level, confirmations)

    let send { Channel = channel } value = channel value

    let ``process`` (s: Subscription) (o: JToken) = s.Parameters.Channel (o.ToString())

    let level { Parameters = parameters } = parameters.Level

    let witchLevel subscription level =
        subscription.Level <- level
        subscription

module ContractAddress =
    open System

    let create value =
        if String.IsNullOrEmpty(value) then None else Some(ContractAddress value)

    let value (ContractAddress addr) = addr
