namespace b0wter.CouchDb.Lib

    module Json = 

        open Newtonsoft.Json

        type FindSelectorConverter() =
            inherit JsonConverter()

            let comparisonType = typedefof<Selector.Selector>

            override this.CanConvert(t) =
                // Check wether the given type is derived from the Selector-type.
                typeof<Selector.Selector>.IsAssignableFrom(t)

            override this.WriteJson(writer, value, serializer) =
                let castValue = value :?> Selector.Selector
                let object = Newtonsoft.Json.Linq.JObject()
                let prop = Newtonsoft.Json.Linq.JProperty(castValue.Name, castValue.TranslateValue())
                do object.Add(prop)
                do object.WriteTo(writer)

            override this.ReadJson(reader, objectType, existingValue, serializer) =
                failwith "Reading this type (Selector) is not supported."

        let findSelectorConverter = FindSelectorConverter () :> JsonConverter