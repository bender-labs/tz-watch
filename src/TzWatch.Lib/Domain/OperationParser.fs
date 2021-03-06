namespace TzWatch.Domain

open System.IO
open FSharp.Data
open FSharp.Data.JsonExtensions


[<RequireQualifiedAccess>]
module OperationGroupParser =

    let parse (reader: TextReader): OperationGroup seq =

        let value = JsonValue.Load(reader)

        let (|EpCall|_|) (op: JsonValue) =
            if op?kind.AsString() <> "transaction" then
                None
            else
                match op.TryGetProperty "parameters" with
                | Some _ -> Some op
                | None -> None

        let extractParameters (v: JsonValue) =
            let stringWriter = new StringWriter()

            v?value
                .WriteTo(stringWriter, JsonSaveOptions.None)

            { Entrypoint = v?entrypoint.AsString()
              Parameters =
                  v?value
                      .ToString(JsonSaveOptions.DisableFormatting) }

        let toOperation id status (v: JsonValue) =
            let status =
                if status = "applied" then Applied else Error

            { Kind = SmartContractCall(extractParameters v?parameters)
              Source = v?source.AsString()
              Destination = v?destination.AsString()
              Id = id
              Status = status }

        let parseMetadata id (v: JsonValue) =

            let meta = v?metadata

            let internalOps =
                match meta.TryGetProperty("internal_operation_results") with
                | Some internals ->
                    internals.AsArray()
                    |> Seq.map (fun i ->
                        match i with
                        | EpCall _ ->
                            let nonce = i?nonce.AsInteger()
                            let status = i?result?status.AsString()
                            Some(toOperation (InternalOperation(id, nonce)) status i)
                        | _ -> None)
                    |> Seq.choose Operators.id
                    |> Seq.toList
                | None -> []

            (meta?operation_result?status.AsString(), internalOps)

        let parseOp (hash: string) (operation: JsonValue): Operation list =
            match operation with
            | EpCall _ ->
                let id =
                    { OpgHash = hash
                      Counter = operation?counter.AsInteger() }

                let (status, internalOps) = parseMetadata id operation

                (toOperation (Operation id) status operation)
                :: internalOps
            | _ -> []



        let parseOpg (v: JsonValue) =
            let ops =
                v?contents.AsArray()
                |> Seq.collect (parseOp (v?hash.AsString()))
                |> Seq.toList

            if ops |> Seq.isEmpty then
                None
            else
                Some
                    { ChainId = v?chain_id.AsString()
                      Hash = v?hash.AsString()
                      Branch = v?branch.AsString()
                      Operations = ops }

        let ops =
            match value with
            | JsonValue.Array a -> a |> Seq.map parseOpg
            | _ -> failwith "Unexpected payload"

        ops |> Seq.choose id
