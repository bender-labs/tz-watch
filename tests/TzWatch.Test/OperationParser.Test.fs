module TzWatch.Test.``Block parser``

open System.IO
open FsUnit.Xunit
open TzWatch.Domain
open TzWatch.Test
open Xunit

[<Fact>]
let ``Should extract one entrypoint call`` () =
    let block =
        singleOperationTemplate
            .Replace("#DESTINATION#", "Destination")
            .Replace("#ENTRYPOINT#", "EntryPointName")

    let result =
        OperationGroupParser.parse (new StringReader(block))
        |> Seq.toList

    result |> should haveLength 1

    result.[0]
    |> should
        equal
           { ChainId = "NetXm8tYqnMWky1"
             Hash = "oom3y9QdYJmdUXzwAyYHV4K7ecbtJGqpv6yiSWxD85FB8aBXn8j"
             Branch = "BMWMe6dAyDoryigCLCA9X7Tvy1PznnWhQXVab6SSvdnvM4YANrW"
             Operations =
                 [ { Kind =
                         SmartContractCall
                             ({ Entrypoint = "EntryPointName"
                                Parameters =
                                    """{"prim":"Pair","args":[{"prim":"Pair","args":[{"int":"10000000000000000000000000000000000"},{"string":"ethTxId"}]},{"prim":"Pair","args":[{"string":"tz1S792fHX5rvs6GYP49S1U58isZkp2bNmn6"},{"string":"BOD"}]}]}""" })
                     Id =
                         Operation
                             { OpgHash = "oom3y9QdYJmdUXzwAyYHV4K7ecbtJGqpv6yiSWxD85FB8aBXn8j"
                               Counter = 654654 }
                     Source = "tz1S792fHX5rvs6GYP49S1U58isZkp2bNmn6"
                     Destination = "Destination"
                     Status = Applied } ] }

[<Fact>]
let ``Should survive big block`` () =
    let payload =
        File.ReadAllText("./sample/big_block.json")

    let result =
        OperationGroupParser.parse (new StringReader(payload))
        |> Seq.toList

    result |> should haveLength 0


[<Fact>]
let ``Should ignore simple transfers`` () =
    let result =
        OperationGroupParser.parse (new StringReader(transferTemplate))
        |> Seq.toList

    result |> should haveLength 0

[<Fact>]
let ``Should extract internal operations`` () =
    let payload =
        rawBlockWithInternalCall "target_contract" "target_entrypoint"

    let result =
        OperationGroupParser.parse (new StringReader(payload))
        |> Seq.toList

    result |> should haveLength 1
    result.[0].Operations |> should haveLength 2

    result.[0].Operations
    |> Seq.last
    |> should
        equal
           { Kind =
                 SmartContractCall
                     ({ Entrypoint = "target_entrypoint"
                        Parameters =
                            """{"prim":"Right","args":[[{"prim":"Pair","args":[{"int":"9990000000000000000000000000000000"},{"bytes":"000046f146853a32c121cfdcd4f446876ae36c4afc58"}]},{"prim":"Pair","args":[{"int":"10000000000000000000000000000000"},{"bytes":"000046f146853a32c121cfdcd4f446876ae36c4afc58"}]}]]}""" })
             Id =
                 InternalOperation
                     ({ OpgHash = "oom3y9QdYJmdUXzwAyYHV4K7ecbtJGqpv6yiSWxD85FB8aBXn8j"
                        Counter = 654654 },
                      0)
             Source = "KT1EwUrkbmGxjiRvmEAa8HLGhjJeRocqVTFi"
             Destination = "target_contract"
             Status = Applied }

[<Fact>]
let ``Should parse operation status`` () =
    let payload =
        singleOperationTemplate.Replace("applied", "error")

    let result =
        OperationGroupParser.parse (new StringReader(payload))
        |> Seq.toList

    result |> should haveLength 1

    (result.[0].Operations |> Seq.head).Status
    |> should equal Error
