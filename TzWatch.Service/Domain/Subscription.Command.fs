namespace TzWatch.Service.Domain

module Command =
    type CreateSubscription =
        { Address: string
          Level: int option
          Confirmations: int }

