namespace TzWatch.Domain

open System

[<AutoOpen>]
module Types =

    open FSharp.Control

    type OperationId = { OpgHash: string; Counter: int }

    type UpdateId =
        | Operation of OperationId
        | InternalOperation of OperationId * int

    type OperationStatus =
        | Applied
        | Error

    type SmartContractCall =
        { Entrypoint: string
          Parameters: string }

    type OperationKind = SmartContractCall of SmartContractCall

    type Operation =
        { Kind: OperationKind
          Id: UpdateId
          Source: string
          Destination: string
          Status: OperationStatus }

    type OperationGroup =
        { ChainId: string
          Hash: string
          Branch: string
          Operations: Operation seq }


    type Block =
        { Level: int
          Hash: string
          Timestamp: DateTimeOffset
          ChainId: string
          Operations: OperationGroup seq }

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
            if String.IsNullOrEmpty(value) then Result.Error "Bad Address" else Ok(ContractAddress value)

        let createUnsafe value = ContractAddress value

        let value (ContractAddress addr) = addr
