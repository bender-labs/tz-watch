namespace TzWatch.Service.Adapters

open System
open System.Xml.Schema
open Newtonsoft.Json
open Newtonsoft.Json.Serialization
open TzWatch.Service.Domain
open TzWatch.Service.Program

module Json =

    let private config =
        JsonSerializerSettings(ContractResolver = DefaultContractResolver(NamingStrategy = CamelCaseNamingStrategy()))

    type UpdateDto =
        { Level: int
          Hash: string
          Type: string
          Payload: Map<String, Object> }



    [<CLIMutable>]
    type SubscribeDto =
        { Address: string
          Interests: InterestDto list
          Level: Nullable<int>
          Confirmations: Nullable<int> }

    and InterestDto = { Type: string; Value: string }

    let private payload (update: Update) payloadType payload =
        { Level = update.Level
          Hash = update.Hash
          Type = payloadType
          Payload = payload }

    let private toDto (update: Update) =
        match update.Value with
        | EntryPointCall { Entrypoint = ep; Parameters = p } ->
            payload update "entrypoint" (Map.empty<String, Object>.Add("name", ep).Add("parameters", p))
        | _ -> failwith "Not yet"

    let updateToJson (update: Update) =
        JsonConvert.SerializeObject(toDto update, config)

    let toSubscribe (dto: SubscribeDto) (channel: Channel): CreateSubscription =
        { Address = dto.Address
          Level = if dto.Level.HasValue then Some dto.Level.Value else None
          Channel = channel
          Confirmations = if dto.Confirmations.HasValue then dto.Confirmations.Value else 1
          Interests =
              dto.Interests
              |> List.map (fun v ->
                  match v.Type with
                  | "entrypoint" -> EntryPoint v.Value
                  | _ -> failwith "Unknown") }
