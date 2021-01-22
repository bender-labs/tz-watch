namespace TzWatch.Test

module ``Subscription test`` =

    open Xunit
    open FsUnit.Xunit
    open TzWatch.Domain

    type ``Given a subscription interested in one entry point with 0 confirmation``() =

        let subscription =
            { Contract = (ContractAddress.createUnsafe "KT")
              Interests = [ EntryPoint "mint" ]
              Confirmations = 1u }


        [<Fact>]
        let ``should not give update on other contract`` () =
            let (_, updates) =
                Subscription.applyBlock subscription (blockWithContractAndEntryPoint "Other" "mint")

            updates |> Seq.length |> should equal 0

        [<Fact>]
        let ``should not give update on other entrypoint`` () =
            let (_, updates) =
                Subscription.applyBlock subscription (blockWithContractAndEntryPoint "KT" "burn")

            updates |> Seq.length |> should equal 0

        [<Fact>]
        let ``should give update when entrypoint and contract match`` () =
            let block =
                blockWithContractAndEntryPoint "KT" "mint"

            let (_, updates) =
                Subscription.applyBlock subscription block

            updates |> Seq.length |> should equal 1
            let update = updates |> Seq.head

            update.Hash
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
        let ``should ignore transfer`` () =
            let block = blockWithContractTransfer 0 "KT"

            let (_, updates) =
                Subscription.applyBlock subscription block

            updates |> Seq.length |> should equal 0