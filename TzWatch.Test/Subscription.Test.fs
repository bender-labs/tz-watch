namespace TzWatch.Test

module ``Subscription test`` =
    open Xunit
    open FsUnit.Xunit
    open TzWatch.Service.Domain


    [<Fact>]
    let ``Should create with empty operations`` () =
        let channel (_: string) = async { () }
        let addr = ContractAddress.createUnsafe "KTx"

        let parameters =
            { Contract = addr
              Confirmations = 30
              Interests = [ EntryPoint "mint" ] }

        let sub = Subscription.create parameters channel

        sub.Parameters |> should equal parameters
        sub.Channel |> should be (sameAs channel)
        sub.PendingOperations |> should be (Empty)

    type ``Given a subscription interested in one entry point with 0 confirmation``() =

        let subscription =
            { Parameters =
                  { Contract = (ContractAddress.createUnsafe "KT")
                    Interests = [ EntryPoint "mint" ]
                    Confirmations = 1 }
              Channel = (fun _ -> async { () })
              PendingOperations = Map.empty }

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
            update.Confirmations |> should equal 1
            update.Value
            |> should
                equal
                   (EntryPointCall
                       { Entrypoint = "mint"
                         Parameters = block.Operations.SelectToken("$.[0].contents[0].parameters.value") })

    type `` Given a subscription with 2 confirmations``() =

        let subscription =
            { Parameters =
                  { Contract = (ContractAddress.createUnsafe "KT")
                    Interests = [ EntryPoint "mint" ]
                    Confirmations = 2 }
              Channel = (fun str -> async { () })
              PendingOperations = Map.empty }

        [<Fact>]
        let ``should not trigger update on first observation``() =
            let block = blockWithContractAndEntryPointAtLevel 10 "KT" "mint"
            
            let (newSub, updates) = Subscription.applyBlock subscription (block)
            
            updates |> Seq.length |> should equal 0
            newSub.PendingOperations.[10] |> Seq.length |> should equal 1
            let pendingUpdate = newSub.PendingOperations.[10] |> Seq.head
            pendingUpdate |> should equal (EntryPointCall
                       { Entrypoint = "mint"
                         Parameters = block.Operations.SelectToken("$.[0].contents[0].parameters.value") })