namespace TzWatch.Domain

open System

[<AutoOpen>]
module Types =

    open System.Reactive.Subjects
    open FSharp.Control
    open Newtonsoft.Json.Linq

    type Block = {
        Level: int
        Hash: string
        Timestamp: DateTimeOffset
        ChainId: string
        Operations: JToken
    }

    type Level =
        | Head
        | Height of int
        static member ToLevel(value: int option) =
            match value with
            | Some i -> Height i
            | None -> Head

    type ISync =
        abstract CatchupFrom: Level -> uint -> AsyncSeq<Block>

    type ContractAddress = private ContractAddress of string

    module ContractAddress =

        let create value =
            if String.IsNullOrEmpty(value) then Error "Bad Address" else Ok(ContractAddress value)

        let createUnsafe value = ContractAddress value

        let value (ContractAddress addr) = addr
