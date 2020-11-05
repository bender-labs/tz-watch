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
            update.Hash |> should equal (block.Operations.SelectToken("$.[0].hash").ToString())
            update.Value
            |> should
                equal
                   (EntryPointCall
                       { Entrypoint = "mint"
                         Parameters = block.Operations.SelectToken("$.[0].contents[0].parameters.value") })
                   
        [<Fact>]
        let ``should ignore transfer`` () =
            let block = blockWithContractTransfer 0 "KT"
            
            let(_, updates) = Subscription.applyBlock subscription block
            
            updates |> Seq.length |> should equal 0

    type ``Given a subscription with 2 confirmations``() =

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
            
            let (newSub, updates) = Subscription.applyBlock subscription block
            
            updates |> Seq.length |> should equal 0
            newSub.PendingOperations.[10] |> Seq.length |> should equal 1
            let pendingUpdate = newSub.PendingOperations.[10] |> Seq.head
            let expected = {
                Level= 10
                Hash="oom3y9QdYJmdUXzwAyYHV4K7ecbtJGqpv6yiSWxD85FB8aBXn8j"
                Value = EntryPointCall {
                         Entrypoint = "mint"
                         Parameters = block.Operations.SelectToken("$.[0].contents[0].parameters.value")} 
            }
            pendingUpdate |> should equal expected
            
        [<Fact>]
        let ``should trigger update on second observation``() =
            let block = blockWithContractAndEntryPointAtLevel 10 "KT" "mint"
            let (newSub, _) = Subscription.applyBlock subscription block
            
            let (sub, updates) = Subscription.applyBlock newSub (blockWithContractTransfer 11 "")
            
            updates |> Seq.length |> should equal 1
            let expected = {
                Level= 10
                Hash="oom3y9QdYJmdUXzwAyYHV4K7ecbtJGqpv6yiSWxD85FB8aBXn8j"
                Value = EntryPointCall {
                         Entrypoint = "mint"
                         Parameters = block.Operations.SelectToken("$.[0].contents[0].parameters.value")} 
            }
            updates |> Seq.head |> should equal expected
            sub.PendingOperations.[11] |> should be Empty
            sub.PendingOperations.ContainsKey 10 |> should be False