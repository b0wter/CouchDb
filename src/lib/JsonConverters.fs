namespace b0wter.CouchDb.Lib

    module Json = 

        open Newtonsoft.Json

        let private singleSelectorToJson (name: string) (value: string) : Newtonsoft.Json.Linq.JObject =
            let object = Newtonsoft.Json.Linq.JObject()
            let prop = Newtonsoft.Json.Linq.JProperty(name, value)
            do object.Add(prop)
            object

        let private multipleSelectorsToJson (selectors: (string * string) list) : Newtonsoft.Json.Linq.JObject =
            let mapTupleToToken = fun (name: string, value: string) -> Newtonsoft.Json.Linq.JProperty(name, value)
            let object = Newtonsoft.Json.Linq.JObject() 
            do selectors |> List.iter (fun s -> do object.Add(s |> mapTupleToToken))
            object

        type FindSelectorConverter() =
            inherit JsonConverter()

            override this.CanConvert(t) =
                // Check wether the given type is derived from the Selector-type.
                typeof<Find.Selector>.IsAssignableFrom(t) && t <> typeof<Find.MultiSelector>

            override this.WriteJson(writer, value, serializer) =
                let castValue = value :?> Find.Selector
                let object = singleSelectorToJson castValue.Name castValue.TranslatedValue
                do object.WriteTo(writer)

            override this.ReadJson(reader, objectType, existingValue, serializer) =
                failwith "Reading this type (Selector) is not supported."
        let findSelectorConverter = FindSelectorConverter () :> JsonConverter
        

        type FindMultiSelectorConverter() =
            inherit JsonConverter()

            override this.CanConvert(t) =
                // Check wether the given type is derived from the Selector-type.
                typeof<Find.MultiSelector>.IsAssignableFrom(t) 

            override this.WriteJson(writer, value, serializer) =
                let castValue = value :?> Find.MultiSelector
                let props = castValue.Selectors |> List.map (fun s -> s.Name, s.TranslatedValue)
                let object = multipleSelectorsToJson props 
                do object.WriteTo(writer)

            override this.ReadJson(reader, objectType, existingValue, serializer) =
                failwith "Reading this type (MultiSelector) is not supported."
        let findMultiSelectorConverter = FindMultiSelectorConverter () :> JsonConverter


        type FindSortConverter() =
            inherit JsonConverter()

            let comparisonType = typedefof<Find.Sorting>

            override this.CanConvert(t) =
                // Check wether the given type is derived from the Selector-type.
                typeof<Find.Selector>.IsAssignableFrom(t)

            override this.WriteJson(writer, value, serializer) =
                failwith "Not yet implemented!"
                let castValue = value :?> Find.Selector
                let object = Newtonsoft.Json.Linq.JObject()
                let prop = Newtonsoft.Json.Linq.JProperty(castValue.Name, castValue.TranslatedValue)
                do object.Add(prop)
                do object.WriteTo(writer)

            override this.ReadJson(reader, objectType, existingValue, serializer) =
                failwith "Reading this type (Selector) is not supported."

        let findSortConverter = FindSortConverter () :> JsonConverter