namespace b0wter.CouchDb.Lib

module Utilities =

    open System
    
    module Json = 
        open Newtonsoft.Json

        let defaultConverters = [ FifteenBelow.Json.OptionConverter () :> Newtonsoft.Json.JsonConverter ]

        let private converterListToIList (converters: JsonConverter list) =
            System.Collections.Generic.List<Newtonsoft.Json.JsonConverter> (converters |> Seq.ofList)

        let jsonConverters = defaultConverters |> converterListToIList

        let jsonSettings = Newtonsoft.Json.JsonSerializerSettings(ContractResolver = Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(),
                                                                  Converters = jsonConverters,
                                                                  Formatting = Newtonsoft.Json.Formatting.Indented,
                                                                  NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)

        let jsonSettingsWithCustomConverter (customs: JsonConverter list) = 
            let converters = (customs @ defaultConverters) |> converterListToIList
            Newtonsoft.Json.JsonSerializerSettings(ContractResolver = Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(),
                                                   Converters = converters,
                                                   Formatting = Newtonsoft.Json.Formatting.Indented,
                                                   NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)