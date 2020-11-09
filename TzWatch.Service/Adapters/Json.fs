namespace TzWatch.Service.Adapters

open System
open Newtonsoft.Json
open Newtonsoft.Json.Serialization
open TzWatch.Service.Domain

module Json =

    let private config =
        JsonSerializerSettings(ContractResolver = DefaultContractResolver(NamingStrategy = CamelCaseNamingStrategy()))

    type UpdateDto =
        { Level: int
          Hash: string
          Type: string
          Payload: Map<String, Object> }

    let private toJson (update: Update) payloadType payload =
        { Level = update.Level
          Hash = update.Hash
          Type = payloadType
          Payload = payload }

    let private toDto update =
        match update.Value with
        | EntryPointCall { Entrypoint = ep; Parameters = p } ->
            toJson update "entrypoint" (Map.empty<String, Object>.Add("name", ep).Add("parameters", p))
        | _ -> failwith "Not yet"

    let updateToJson (update: Update) =
        JsonConvert.SerializeObject(toDto update, config)
