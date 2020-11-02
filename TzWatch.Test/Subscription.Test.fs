namespace TzWatch.Test



module ``Subcription test`` =
    open FsUnit
    open FsToolkit.ErrorHandling
    open NUnit.Framework
    open TzWatch.Service.Domain



    [<Test>]
    let `` Should create subscription`` () =
        let channel (str: string) = async { () }
        let addr = ContractAddress.createUnsafe "KTx"
        let r = result {
            let! addr = ContractAddress.create "KTx"
            return! Subscription.create addr (Height 10) channel 
        }
        
        let expected : Subscription = {
            Parameters = {
                        Contract = addr
                        Confirmations = 30
                        Level = Height 10
                        }
            Channel = channel
            Level = 0
        }
        
        match r with
        | Ok {Parameters = p; Channel = c ;Level = _  } ->
            p |> should equal expected.Parameters
            c |> should be (sameAs channel)
        | Error e -> failwith e
       
        
