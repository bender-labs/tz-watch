namespace TzWatch.Service.Node

module Types =
    open FSharp.Data

    type Header = JsonProvider<"Node/data/header.json">
    type Operation = JsonProvider<"Node/data/content.json", SampleIsList=true>
