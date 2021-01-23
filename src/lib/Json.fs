namespace b0wter.CouchDb.Lib
open Newtonsoft.Json.Serialization

module Json = 
    open Newtonsoft.Json
    
    /// List of default converters.
    let defaultConverters = [ FifteenBelow.Json.OptionConverter () :> JsonConverter ]

    /// Takes a list of converters and turns them into an `IList<JsonConverter>`.
    let private converterListToIList (converters: JsonConverter list) =
        System.Collections.Generic.List<JsonConverter> (converters |> Seq.ofList)

    /// Default `IList` of json converters.
    let converters = defaultConverters |> converterListToIList

    /// Creates a new instance of the default settings.
    let settings () = JsonSerializerSettings(ContractResolver = CamelCasePropertyNamesContractResolver(),
                                                              Converters = converters,
                                                              Formatting = Formatting.Indented,
                                                              NullValueHandling = NullValueHandling.Ignore)
    
    /// This function is run after an object has been serialized. You can use it to tweak the output.
    let mutable postProcessing = fun (serialized: string) -> serialized
    
    /// Returns `settings` but adds the converters specified as argument.
    let settingsWithCustomConverter (customs: JsonConverter list) =
        // TODO: make this nice ^^
        let converters = (customs @ defaultConverters) |> converterListToIList
        let settings = settings ()
        let s = JsonSerializerSettings()
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

    module JObject =
        open Newtonsoft.Json.Linq

        let asJObject s : Result<JObject, string> =
            try
                Ok (JObject.Parse(s))
            with
                | :? JsonException as ex -> Error ex.Message

        let getJArray (p: JProperty) =
            match p.Value.Type with
            | JTokenType.Array -> Ok (p.Value :?> JArray)
            | _ -> Error <| sprintf "Is of type %s." (p.Value.Type.ToString())

        let getProperty propertyName (j: JObject) =
            if j.ContainsKey propertyName then Ok (j.Property propertyName)
            else Error "JObject does not contain given key."

        let toObject<'a> (customConverters: JsonConverter list) (doc: JObject) =
            let settings = if customConverters.IsEmpty then settings () else settingsWithCustomConverter customConverters
            let serializer = JsonSerializer.Create(settings)
            try
                Ok <| doc.ToObject<'a>(serializer)
            with
            | ex -> Error ex.Message

        let toObjects<'a> (customConverters: JsonConverter list) (docs: JObject list) =
            let settings = if customConverters.IsEmpty then settings () else settingsWithCustomConverter customConverters
            let serializer = JsonSerializer.Create(settings)
            try
                Ok (docs |> List.map (fun doc -> 
                    doc.ToObject<'a>(serializer)))
            with
            | ex -> Error ex.Message

        let jArrayAsJObjects (a: JArray) : Result<JObject list, string> =
            try
                Ok [ for item in a do yield (item :?> JObject) ]
            with
            | ex -> Error ex.Message
