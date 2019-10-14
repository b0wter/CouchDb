namespace b0wter.CouchDb.Lib
open Newtonsoft.Json.Serialization

module Json = 
    open Newtonsoft.Json
    
    let defaultConverters = [ FifteenBelow.Json.OptionConverter () :> Newtonsoft.Json.JsonConverter ]

    let private converterListToIList (converters: JsonConverter list) =
        System.Collections.Generic.List<Newtonsoft.Json.JsonConverter> (converters |> Seq.ofList)

    let converters = defaultConverters |> converterListToIList

    /// Creates a new instance of the default settings.
    let settings = Newtonsoft.Json.JsonSerializerSettings(ContractResolver = CamelCasePropertyNamesContractResolver(),
                                                              Converters = converters,
                                                              Formatting = Newtonsoft.Json.Formatting.Indented,
                                                              NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)
    
    /// This function is run after an object has been serialized. You can use it to tweak the output.
    let mutable postProcessing = fun (serialized: string) -> serialized
    
    /// Returns `settings` but adds the converters specified as argument.
    let settingsWithCustomConverter (customs: JsonConverter list) =
        // TODO: make this nice ^^
        let converters = (customs @ defaultConverters) |> converterListToIList
        let s = Newtonsoft.Json.JsonSerializerSettings()
        do s.Context <- settings.Context
        do s.Culture <- settings.Culture
        do s.ContractResolver <- settings.ContractResolver
        do s.ConstructorHandling <- settings.ConstructorHandling
        do s.CheckAdditionalContent <- settings.CheckAdditionalContent
        do s.DateFormatHandling <- settings.DateFormatHandling
        do s.DateFormatString <- settings.DateFormatString
        do s.DateParseHandling <- settings.DateParseHandling
        do s.DateTimeZoneHandling <- settings.DateTimeZoneHandling
        do s.DefaultValueHandling <- settings.DefaultValueHandling
        do s.EqualityComparer <- settings.EqualityComparer
        do s.FloatFormatHandling <- settings.FloatFormatHandling
        do s.Formatting <- settings.Formatting
        do s.FloatParseHandling <- settings.FloatParseHandling
        do s.MaxDepth <- settings.MaxDepth
        do s.MetadataPropertyHandling <- settings.MetadataPropertyHandling
        do s.MissingMemberHandling <- settings.MissingMemberHandling
        do s.NullValueHandling <- settings.NullValueHandling
        do s.ObjectCreationHandling <- settings.ObjectCreationHandling
        do s.PreserveReferencesHandling <- settings.PreserveReferencesHandling
        do s.ReferenceResolverProvider <- settings.ReferenceResolverProvider
        do s.ReferenceLoopHandling <- settings.ReferenceLoopHandling
        do s.StringEscapeHandling <- settings.StringEscapeHandling
        do s.TraceWriter <- settings.TraceWriter
        do s.TypeNameHandling <- settings.TypeNameHandling
        do s.SerializationBinder <- settings.SerializationBinder
        do s.TypeNameAssemblyFormatHandling <- settings.TypeNameAssemblyFormatHandling
        do s.Converters <- converters
        s
