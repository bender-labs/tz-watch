namespace TzWatch.Http.Adapters

open System
open Newtonsoft.Json
open Newtonsoft.Json.Serialization
open TzWatch.Domain
open TzWatch.Http.Program

module Json =

    let private config =
        JsonSerializerSettings(ContractResolver = DefaultContractResolver(NamingStrategy = CamelCaseNamingStrategy()))



    type UpdateDto =
        { OperationId: OperationId
          Nonce: int Nullable
          Type: string
          Payload: Map<String, Object> }


    type BlockDto =
        { Level: int
          Hash: string
          ChainId: string
          Timestamp: DateTimeOffset
          Updates: UpdateDto list }

    [<CLIMutable>]
    type SubscribeDto =
        { Address: string
          Interests: InterestDto list
          Level: Nullable<int>
          Confirmations: Nullable<int> }

    and InterestDto = { Type: string; Value: string }

    let private opId =
        function
        | Operation v -> v
        | InternalOperation (v, _) -> v

    let nonce =
        function
        | InternalOperation (_, i) -> Nullable i
        | _ -> Nullable()

    let private payload (update: Update) payloadType payload =
        { OperationId = opId update.UpdateId
          Nonce = nonce update.UpdateId
          Type = payloadType
          Payload = payload }

    let private toDto ({ BlockHeader = header
                         Updates = updates }: EventLog) =
        let updates =
            updates
            |> Seq.map (fun update ->
                match update.Value with
                | EntryPointCall { Entrypoint = ep; Parameters = p } ->
                    payload
                        update
                        "entrypoint"
                        (Map.empty<String, Object>
                            .Add("name", ep)
                            .Add("parameters", p))
                | _ -> failwith "Not yet")
            |> Seq.toList

        { Level = int header.Level
          Hash = header.Hash
          Timestamp = header.Timestamp
          ChainId = header.ChainId
          Updates = updates }

    let updateToJson (update: EventLog) =
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
