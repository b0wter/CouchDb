namespace b0wter.CouchDb.Lib

module Json = 
    open Newtonsoft.Json

    let defaultConverters = [ FifteenBelow.Json.OptionConverter () :> Newtonsoft.Json.JsonConverter ]

    let private converterListToIList (converters: JsonConverter list) =
        System.Collections.Generic.List<Newtonsoft.Json.JsonConverter> (converters |> Seq.ofList)

    let converters = defaultConverters |> converterListToIList

    
    let settings () = Newtonsoft.Json.JsonSerializerSettings(ContractResolver = Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(),
                                                              Converters = converters,
                                                              Formatting = Newtonsoft.Json.Formatting.Indented,
                                                              NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)
    
    /// This function is run after an object has been serialized. You can use it to tweak the output.
    let mutable postProcessing = fun (serialized: string) -> serialized
    
    /// Returns `settings` but adds the converters specified as argument.
    let settingsWithCustomConverter (customs: JsonConverter list) = 
        let converters = (customs @ defaultConverters) |> converterListToIList
        let settings = settings ()
        do settings.Converters <- converters
        settings
        

