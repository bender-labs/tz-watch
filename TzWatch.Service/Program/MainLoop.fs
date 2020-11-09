namespace TzWatch.Service.Program

open System
open System.Threading
open FSharp.Control
open Microsoft.Extensions.Logging
open TzWatch.Service.Domain
open FsToolkit.ErrorHandling

type CreateSubscription =
    { Address: string
      Level: int option
      Confirmations: int
      Channel: Channel
      Interests: Interest list }

type Message =
    | Subscribe of CreateSubscription * replyChannel: AsyncReplyChannel<Guid>
    | Cancel of Guid

module CommandHandler =
    let subscribe (poller: ISync)(command: CreateSubscription) =
        result {
            let! address = ContractAddress.create command.Address
            let parameters =
                { Contract = address
                  Interests = command.Interests
                  Confirmations = 0 }

            let w =
                Subscription.create parameters command.Channel
                |> Subscription.run poller (Level.ToLevel command.Level)

            let token = new CancellationTokenSource()

            Async.Start(w, token.Token)

            return (Guid.NewGuid(), token)
        }

type MainLoop(poller: ISync, log: ILogger<MainLoop>) =

    let subscribe =
        CommandHandler.subscribe poller

    let agent =
        MailboxProcessor.Start(fun inbox ->
            let rec messageLoop (state: Map<Guid, CancellationTokenSource>) =
                async {
                    let! msg = inbox.Receive()
                    log.LogInformation("Message {msg}", [msg])
                    let newState =
                        match msg with
                        | Subscribe (c, r) ->
                            match (subscribe c) with
                            | Ok (id, token) ->
                                r.Reply id
                                state.Add(id, token)
                            | Error e ->
                                log.LogError("Error {e}", [e])
                                state
                        | Cancel id ->
                            if state.ContainsKey(id) then
                                state.[id].Cancel()
                                state.Remove(id)
                            else state

                    return! messageLoop newState
                }

            messageLoop Map.empty)

    member this.Subscribe(p: CreateSubscription) =
        agent.PostAndAsyncReply(fun rc -> Subscribe(p, rc))

    member this.CancelSubscription id = agent.Post(Cancel id)
