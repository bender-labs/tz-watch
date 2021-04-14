namespace TzWatch.Test

open System

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

    let operationHash =
        "oom3y9QdYJmdUXzwAyYHV4K7ecbtJGqpv6yiSWxD85FB8aBXn8j"

    let counter = 654654
    let nonce = 10


    let blockWithContractAndEntryPoint destination ep status: Block =
        { Level = 19
          Hash = "Hash"
          Timestamp = DateTimeOffset.Now
          ChainId = "Chain_id"
          Operations =
              [ { Hash = operationHash
                  ChainId = "ChainId"
                  Branch = "Branch"
                  Operations =
                      [ { Kind =
                              SmartContractCall
                                  ({ Entrypoint = ep
                                     Parameters = """{"hello":"there"}""" })
                          Id =
                              Operation
                                  { OpgHash = "oom3y9QdYJmdUXzwAyYHV4K7ecbtJGqpv6yiSWxD85FB8aBXn8j"
                                    Counter = counter }
                          Source = "tz1S792fHX5rvs6GYP49S1U58isZkp2bNmn6"
                          Destination = destination
                          Status = status } ] } ] }

    let blockWithSuccessfulContractAndEntryPoint destination ep: Block =
        blockWithContractAndEntryPoint destination ep Applied

    let blockWithRejectedOperation destination ep: Block =
        blockWithContractAndEntryPoint destination ep Error

    let blockWithInternalCall destination ep: Block =
        { Level = 19
          Hash = "Hash"
          Timestamp = DateTimeOffset.Now
          ChainId = "Chain_id"
          Operations =
              [ { Hash = operationHash
                  ChainId = "ChainId"
                  Branch = "Branch"
                  Operations =
                      [ { Kind =
                              SmartContractCall
                                  ({ Entrypoint = ep
                                     Parameters = """{"hello":"there"}""" })
                          Id =
                              InternalOperation
                                  ({ OpgHash = "oom3y9QdYJmdUXzwAyYHV4K7ecbtJGqpv6yiSWxD85FB8aBXn8j"
                                     Counter = counter },
                                   nonce)
                          Source = "tz1S792fHX5rvs6GYP49S1U58isZkp2bNmn6"
                          Destination = destination
                          Status = Applied } ] } ] }


    type ``Given a subscription interested in one entry point with 0 confirmation``() =

        let subscription =
            { Contract = (ContractAddress.createUnsafe "KT")
              Interests = [ EntryPoint "mint" ]
              Confirmations = 1u }


        [<Fact>]
        let ``should not give update on other contract`` () =
            let (_, u) =
                Subscription.applyBlock subscription (blockWithSuccessfulContractAndEntryPoint "Other" "mint")

            u.Updates |> Seq.length |> should equal 0

        [<Fact>]
        let ``should not give update on other entrypoint`` () =
            let (_, u) =
                Subscription.applyBlock subscription (blockWithSuccessfulContractAndEntryPoint "KT" "burn")

            u.Updates |> Seq.length |> should equal 0

        [<Fact>]
        let ``should give update when entrypoint and contract match`` () =
            let block =
                blockWithSuccessfulContractAndEntryPoint "KT" "mint"

            let (_, updates) =
                Subscription.applyBlock subscription block


            updates.Updates |> Seq.length |> should equal 1
            let update = updates.Updates |> Seq.head

            let expectedId =
                Operation
                    { OpgHash = operationHash
                      Counter = counter }

            update.UpdateId |> should equal expectedId

            update.Value
            |> should
                equal
                   (EntryPointCall
                       { Entrypoint = "mint"
                         Parameters = """{"hello":"there"}""" })


        [<Fact>]
        let ``Should give update for internal operations`` () =
            let block = blockWithInternalCall "KT" "mint"

            let (_, updates) =
                Subscription.applyBlock subscription block

            updates.Updates |> Seq.length |> should equal 1
            let update = updates.Updates |> Seq.head

            let expectedId =
                InternalOperation
                    ({ OpgHash = operationHash
                       Counter = counter },
                     nonce)

            update.UpdateId |> should equal expectedId

            update.Value
            |> should
                equal
                   (EntryPointCall
                       { Entrypoint = "mint"
                         Parameters = """{"hello":"there"}""" })

        [<Fact>]
        let ``should ignore failed operations`` () =
            let block = blockWithRejectedOperation "KT" "mint"

            let (_, u) =
                Subscription.applyBlock subscription block

            u.Updates |> Seq.length |> should equal 0
