namespace TzWatch.Service.Domain

open System

[<AutoOpen>]
module Types =

    open System.Reactive.Subjects
    open FSharp.Control
    open Newtonsoft.Json.Linq

    type Block = { Level: int; Operations: JToken }

    type Level =
        | Head
        | Height of int
        static member ToLevel(value: int option) =
            match value with
            | Some i -> Height i
            | None -> Head

    type ISync =
        abstract Head: IConnectableObservable<JToken>
        abstract From: int -> AsyncSeq<JToken>
        abstract CatchupFrom: Level -> AsyncSeq<Block>

    type ContractAddress = private ContractAddress of string

    module ContractAddress =

        let create value =
            if String.IsNullOrEmpty(value) then Error "Bad Address" else Ok(ContractAddress value)

        let createUnsafe value = ContractAddress value

        let value (ContractAddress addr) = addr
