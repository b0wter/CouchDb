namespace b0wter.FSharp

module Utilities =

    open System
    
    module Json = 
        let jsonConverters = System.Collections.Generic.List<Newtonsoft.Json.JsonConverter> ([ FifteenBelow.Json.OptionConverter () :> Newtonsoft.Json.JsonConverter ] |> Seq.ofList)
        let jsonSettings = Newtonsoft.Json.JsonSerializerSettings(ContractResolver = Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(),
                                                                  Converters = jsonConverters,
                                                                  Formatting = Newtonsoft.Json.Formatting.Indented,
                                                                  NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)