[<AutoOpen>]
module TzWatch.Test.Data

open Newtonsoft.Json.Linq




let private template = """[ { "protocol": "PsDELPH1Kxsxt8f9eWbxQeRxkjfbxoqM52jvs5Y5fBxWWh4ifpo",
    "chain_id": "NetXm8tYqnMWky1",
    "hash": "oom3y9QdYJmdUXzwAyYHV4K7ecbtJGqpv6yiSWxD85FB8aBXn8j",
    "branch": "BMWMe6dAyDoryigCLCA9X7Tvy1PznnWhQXVab6SSvdnvM4YANrW",
    "contents":
      [ { "kind": "transaction",
          "source": "tz1S792fHX5rvs6GYP49S1U58isZkp2bNmn6", "fee": "6199",
          "counter": "654654", "gas_limit": "58490", "storage_limit": "166",
          "amount": "0",
          "destination": "#DESTINATION#",
          "parameters":
            { "entrypoint": "#ENTRYPOINT#",
              "value":
                { "prim": "Pair",
                  "args":
                    [ { "prim": "Pair",
                        "args":
                          [ { "int": "10000000000000000000000000000000000" },
                            { "string": "ethTxId" } ] },
                      { "prim": "Pair",
                        "args":
                          [ { "string":
                                "tz1S792fHX5rvs6GYP49S1U58isZkp2bNmn6" },
                            { "string": "BOD" } ] } ] } },
          "metadata":
            { "balance_updates":
                [ { "kind": "contract",
                    "contract": "tz1S792fHX5rvs6GYP49S1U58isZkp2bNmn6",
                    "change": "-6199" },
                  { "kind": "freezer", "category": "fees",
                    "delegate": "tz1PirboHQVqkYqLSWfHUHEy3AdhYUNJpvGy",
                    "cycle": 51, "change": "6199" } ],
              "operation_result":
                { "status": "applied",
                  "storage":
                    { "prim": "Pair",
                      "args":
                        [ { "prim": "Pair",
                            "args":
                              [ { "prim": "Pair",
                                  "args":
                                    [ { "bytes":
                                          "000046f146853a32c121cfdcd4f446876ae36c4afc58" },
                                      { "bytes":
                                          "000046f146853a32c121cfdcd4f446876ae36c4afc58" } ] },
                                { "prim": "Pair",
                                  "args":
                                    [ { "int": "10" }, { "int": "2690" } ] } ] },
                          [ { "prim": "Elt",
                              "args":
                                [ { "string": "BOD" },
                                  { "bytes":
                                      "0187940a149937b2977c4a8c7151fa01c8b8c6d81800" } ] } ] ] },
                  "big_map_diff":
                    [ { "action": "update", "big_map": "2690",
                        "key_hash":
                          "expruWhSCN9NFBaUw4dZF6FLsD5PDtmAc7XrHa5FKEvcVEUkTtiKbV",
                        "key": { "string": "ethTxId" },
                        "value": { "prim": "Unit" } } ],
                  "balance_updates":
                    [ { "kind": "contract",
                        "contract": "tz1S792fHX5rvs6GYP49S1U58isZkp2bNmn6",
                        "change": "-16750" } ], "consumed_gas": "32695",
                  "consumed_milligas": "32694061", "storage_size": "1860",
                  "paid_storage_size_diff": "67" },
              "internal_operation_results":
                [ { "kind": "transaction",
                    "source": "KT1EwUrkbmGxjiRvmEAa8HLGhjJeRocqVTFi",
                    "nonce": 0, "amount": "0",
                    "destination": "KT1Lwe8cqgriABNvafPJGG4MJdvn1yYqLxHq",
                    "parameters":
                      { "entrypoint": "tokens",
                        "value":
                          { "prim": "Right",
                            "args":
                              [ [ { "prim": "Pair",
                                    "args":
                                      [ { "int":
                                            "9990000000000000000000000000000000" },
                                        { "bytes":
                                            "000046f146853a32c121cfdcd4f446876ae36c4afc58" } ] },
                                  { "prim": "Pair",
                                    "args":
                                      [ { "int":
                                            "10000000000000000000000000000000" },
                                        { "bytes":
                                            "000046f146853a32c121cfdcd4f446876ae36c4afc58" } ] } ] ] } },
                    "result":
                      { "status": "applied",
                        "storage":
                          { "prim": "Pair",
                            "args":
                              [ { "prim": "Pair",
                                  "args":
                                    [ { "prim": "Pair",
                                        "args":
                                          [ { "bytes":
                                                "0145bb8b22d117f81eb83f9c81384c8a14e035b74900" },
                                            { "prim": "False" } ] },
                                      { "prim": "None" } ] },
                                { "prim": "Pair",
                                  "args":
                                    [ { "prim": "Pair",
                                        "args":
                                          [ { "int": "2691" },
                                            { "int": "2692" } ] },
                                      { "prim": "Pair",
                                        "args":
                                          [ { "int": "2693" },
                                            { "int":
                                                "10000000000000000000000000000000000" } ] } ] } ] },
                        "big_map_diff":
                          [ { "action": "update", "big_map": "2691",
                              "key_hash":
                                "exprtm5gRU49NzNo5zyJqwyyDYY5YJfjGmv2SAxhxMwohEzntTVscE",
                              "key":
                                { "bytes":
                                    "000046f146853a32c121cfdcd4f446876ae36c4afc58" },
                              "value":
                                { "int":
                                    "10000000000000000000000000000000000" } } ],
                        "balance_updates":
                          [ { "kind": "contract",
                              "contract":
                                "tz1S792fHX5rvs6GYP49S1U58isZkp2bNmn6",
                              "change": "-24750" } ],
                        "consumed_gas": "25595",
                        "consumed_milligas": "25594880",
                        "storage_size": "4386",
                        "paid_storage_size_diff": "99" } } ] } } ],
    "signature":
      "sigVhrq834QeG9z4X35zZieJ9wn2n5mrirAdQHWuyy2PKwTfCk9Dza9GnX1PxQNtUB12jmSY2oKT99grDXtVFozyuhbwsRV5" } ]
""" 
open TzWatch.Service.Domain

let blockWithContractAndEntryPointAtLevel level contract entryPoint =
  let token = JToken.Parse (template
                              .Replace("#DESTINATION#", contract)
                              .Replace("#ENTRYPOINT#", entryPoint))
  { Level = level ; Operations = token}
let blockWithContractAndEntryPoint = blockWithContractAndEntryPointAtLevel 0

