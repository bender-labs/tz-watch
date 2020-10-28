namespace TzWatch.Service.Program


open FSharp.Control
open Microsoft.Extensions.Logging
open TzWatch.Service.Model

type Message = Subscribe of CreateSubscription

module CommandHandler =
    let subscribe (poller: Sync) (log: string -> unit) (command: CreateSubscription) =
        async {
            match ContractAddress.create command.Address with
            | Some a ->
                let s =
                    Subscription.create a (Level.ToLevel command.Level) (fun s -> async { log s })

                let handler = Subscription.``process`` s

                match Subscription.level s with
                | Height i -> do! poller.From i |> AsyncSeq.iterAsync handler

                | _ -> ()
                log "at head"
                return Ok(poller.Head.Subscribe(fun e -> (handler e) |> Async.StartImmediate))
            | None -> return Error "nop"
        }


type MainLoop(poller: Sync, log: ILogger<MainLoop>) =

    let subscribe =
        CommandHandler.subscribe poller log.LogInformation

    let agent =
        MailboxProcessor.Start(fun inbox ->
            let rec messageLoop () =
                async {
                    let! msg = inbox.Receive()

                    match msg with
                    | Subscribe c -> (subscribe c) |> Async.StartAsTask |> ignore

                    log.LogInformation("Message {msg}", msg)

                    return! messageLoop ()
                }

            messageLoop ())

    member this.Send message = agent.Post message
