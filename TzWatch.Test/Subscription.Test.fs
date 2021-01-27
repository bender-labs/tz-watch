namespace TzWatch.Test

module ``Subscription test`` =

    open Xunit
    open FsUnit.Xunit
    open TzWatch.Domain

    let hasValue =
        function
        | Some _ -> true
        | None -> false

    let value =
        function
        | Some v -> v
        | None -> failwith "Option is empty"

    type ``Given a subscription interested in one entry point with 0 confirmation``() =

        let subscription =
            { Contract = (ContractAddress.createUnsafe "KT")
              Interests = [ EntryPoint "mint" ]
              Confirmations = 1u }


        [<Fact>]
        let ``should not give update on other contract`` () =
            let log =
                Subscription.applyBlock subscription (blockWithContractAndEntryPoint "Other" "mint")

            log |> hasValue |> should equal false

        [<Fact>]
        let ``should not give update on other entrypoint`` () =
            let log =
                Subscription.applyBlock subscription (blockWithContractAndEntryPoint "KT" "burn")

            log |> hasValue |> should equal false

        [<Fact>]
        let ``should give update when entrypoint and contract match`` () =
            let block =
                blockWithContractAndEntryPoint "KT" "mint"

            let (_, updates) =
                Subscription.applyBlock subscription block
                |> value

            updates.Updates |> Seq.length |> should equal 1
            let update = updates.Updates |> Seq.head

            update.OperationHash
            |> should
                equal
                   (block
                       .Operations
                       .SelectToken("$.[0].hash")
                       .ToString())

            update.Value
            |> should
                equal
                   (EntryPointCall
                       { Entrypoint = "mint"
                         Parameters = block.Operations.SelectToken("$.[0].contents[0].parameters.value") })


        [<Fact>]
        let ``Should give update for internal operations`` () =
            let block = blockWithInternalCall "KT" "mint"

            let (_, updates) =
                Subscription.applyBlock subscription block
                |> value

            updates.Updates |> Seq.length |> should equal 1
            let update = updates.Updates |> Seq.head

            update.OperationHash
            |> should
                equal
                   (block
                       .Operations
                       .SelectToken("$.[0].hash")
                       .ToString())

            update.Value
            |> should
                equal
                   (EntryPointCall
                       { Entrypoint = "mint"
                         Parameters =
                             block.Operations.SelectToken
                                 ("$.[0].contents[0].metadata.internal_operation_results[0].parameters.value") })

        [<Fact>]
        let ``should ignore transfer`` () =
            let block = blockWithContractTransfer 0 "KT"

            let log =
                Subscription.applyBlock subscription block

            log |> hasValue |> should equal false
