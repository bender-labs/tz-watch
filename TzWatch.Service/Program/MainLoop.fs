namespace TzWatch.Service.Program

open FSharp.Control
open Microsoft.Extensions.Logging
open TzWatch.Service.Domain
open TzWatch.Service.Domain.Command
open FsToolkit.ErrorHandling

type Message = Subscribe of CreateSubscription

module CommandHandler =
    let subscribe (poller: ISync) (log: string -> unit) (command: CreateSubscription) =
        result {
            let! address = ContractAddress.create command.Address
            let parameters = {
                Contract = address
                Interests = []
                Confirmations = 0
            }
            let sub = Subscription.create parameters (fun s -> async { log s })
            return Subscription.run sub poller (Level.ToLevel command.Level)
        }

type MainLoop(poller: ISync, log: ILogger<MainLoop>) =

    let subscribe =
        CommandHandler.subscribe poller log.LogInformation

    let agent =
        MailboxProcessor.Start(fun inbox ->
            let rec messageLoop () =
                async {
                    let! msg = inbox.Receive()

                    (log.LogInformation "Message {msg}", msg)
                    |> ignore

                    match msg with
                    | Subscribe c ->
                        subscribe c
                        |> Result.mapError (fun e -> log.LogError("Error {e}", e))
                        |> Result.bind (fun workflow ->
                            Async.Start workflow
                            Ok workflow)
                        |> ignore

                    return! messageLoop ()
                }

            messageLoop ())

    member this.Send message = agent.Post message
