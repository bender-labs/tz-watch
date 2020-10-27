namespace TzWatch.Service.Model


open System
open System.Reactive.Subjects
open System.Reactive.Subjects
open FSharp.Control
open Newtonsoft.Json.Linq

type Channel = string -> Async<Unit>

type CreateSubscription =
    { Address: string
      Level: int option
      Confirmations: int }

type Level =
    | Head
    | Height of int
    static member ToLevel(value: int option) =
        match value with
        | Some i -> Height i
        | None -> Head

type Poller = Level -> AsyncSeq<JToken>

type Sync =
    abstract Head: IConnectableObservable<JToken>
    abstract From: int -> AsyncSeq<JToken>
