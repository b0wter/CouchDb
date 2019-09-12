namespace b0wter.CouchDb.Lib

    module MangoConverters =

        open Newtonsoft.Json.Linq
        open Mango

        /// <summary>
        /// Builds a 
        /// `"$eq": "Lars von Trier"`
        /// from a `DataType` and the operation (already in text form).
        /// </summary>
        let private dataTypeAndNameToJProperty (d: DataType) (operation: string) =
            match d with
            | Bool b            -> JProperty(operation, b)
            | Int i             -> JProperty(operation, i)
            | Float f           -> JProperty(operation, f)
            | DataType.String s -> JProperty(operation, s)
            | Date d            -> JProperty(operation, d.ToString("yyyy-mm-DD"))
            | Id i              -> JProperty(operation, i.ToString())

        let private typeFieldToString (t: Mango.TypeField) =
            match t with
            | Null -> "null"
            | Boolean -> "boolean"
            | Number -> "number"
            | String -> "string"
            | Array -> "array"
            | Object -> "object"

        let private nameAndPropertyToJOject (name: string) (property: JProperty) =
            JProperty(name, property)

        let dataTypesToJProperty (propertyName: string) (types: DataType list) =
            let asJValues = types |> List.map (fun x -> match x with
                                                        | Bool b -> JValue(b)
                                                        | Int i  -> JValue(i)
                                                        | Float f -> JValue(f)
                                                        | DataType.String s -> JValue(s)
                                                        | Date d -> JValue(d.ToString("yyyy-mm-DD"))
                                                        | Id i -> JValue(i.ToString()))
            let jArray = JArray(asJValues)
            JProperty(propertyName, jArray)

        let private conditionalOperatorToJObject (c: ConditionalOperator) =
            let object = JObject()
            let name = if c.parents.IsEmpty then c.name else System.String.Join(".", c.parents |> Seq.ofList) + "." + c.name
            let jProperty = match c.operation with
                            | Less x                    -> dataTypeAndNameToJProperty x "$lt"
                            | LessOrEqual x             -> dataTypeAndNameToJProperty x "$lte"
                            | Equal x                   -> dataTypeAndNameToJProperty x "$eq"
                            | NotEqual x                -> dataTypeAndNameToJProperty x "$ne"
                            | GreaterOrEqual x          -> dataTypeAndNameToJProperty x "$gte"
                            | Greater x                 -> dataTypeAndNameToJProperty x "$gt"
                            | Exists x                  -> JProperty ("$exists", x)
                            | Type x                    -> JProperty ("$type", x |> typeFieldToString)
                            | In x                      -> dataTypesToJProperty "$in" x
                            | NotIn x                   -> dataTypesToJProperty "$nin" x
                            | Size x                    -> JProperty ("$size", x)
                            | Mod (divisor, remainder)  -> let items = [| divisor; remainder |]
                                                           let array = JArray(items)
                                                           JProperty ("$mod", array)
                            | Regex x                   -> JProperty ("$regex", x.ToString())
            do object.Add(jProperty)
            object

        type ConditionalJsonConverter() =
            inherit Newtonsoft.Json.JsonConverter()

            override this.CanConvert(t) =
                typeof<ConditionalOperator>.IsAssignableFrom(t)

            override this.ReadJson(reader, objectType, existingValue, serializer) =
                failwith "Reading this type (ConditionalOperator) is not supported."

            override this.WriteJson(writer, value, _) =
                let castValue = value :?> ConditionalOperator
                let jObject = castValue |> conditionalOperatorToJObject

                //let parentJProperty = JProperty(castValue.name, jObject)
                //do printfn "===Conditional Json Converter==="
                //do printfn "%s" <| parentJProperty.ToString()
                //do parentJProperty.WriteTo(writer)

                let parentJObject = JObject()
                do parentJObject.Add(castValue.name, jObject)
                do parentJObject.WriteTo(writer)


        let private combinationToJProperty = 0

        type CombinationJsonConverter() =
            inherit Newtonsoft.Json.JsonConverter()

            override this.CanConvert(t) =
                typeof<CombinationOperator>.IsAssignableFrom(t)

            override this.ReadJson(reader, objectType, existingValue, serializer) =
                failwith "Reading this type (ConditionalOperator) is not supported."

            override this.WriteJson(writer, value, _) =
                failwith "Not yet implemented."


        type OperatorJsonConverter() =
            inherit Newtonsoft.Json.JsonConverter()

            override this.CanConvert(t) =
                typeof<Operator>.IsAssignableFrom(t)

            override this.ReadJson(reader, objectType, existingValue, serializer) =
                failwith "Reading this type (ConditionalOperator) is not supported."

            override this.WriteJson(writer, value, _) =
                (*
                let serialized = JObject.FromObject(value)
                do printfn "===JOBJECT==="
                do printfn "%A" serialized
                *)
                let castObject = value :?> Operator
                match castObject with
                | Operator.Combinator combinator -> failwith "not implemented"
                | Operator.Conditional conditional -> 
                    let jObject = JObject.FromObject(conditional)
                    do jObject.WriteTo(writer)
                    

        type ExpressionJsonConverter() =
            inherit Newtonsoft.Json.JsonConverter()

            override this.CanConvert(t) =
                typeof<Expression>.IsAssignableFrom(t)

            override this.ReadJson(reader, objectType, existingValue, serializer) =
                failwith "Reading this type (ConditionalOperator) is not supported."

            override this.WriteJson(writer, value, _) =
                failwith "Not yet implemented."
