namespace b0wter.CouchDb.Lib

module MangoConverters =

    open Newtonsoft.Json.Linq
    open Mango

    let dateTimeFormat = "yyyy-MM-ddTHH:mm:ss"

    /// <summary>
    /// Builds a 
    /// `"$eq": "Lars von Trier"`
    /// from a `DataType` and the operation (already in text form).
    /// </summary>
    let private dataTypeAndNameToJProperty (d: DataType) (operation: string) =
        match d with
        | Bool b    -> JProperty(operation, b)
        | Integer i -> JProperty(operation, i)
        | Float f   -> JProperty(operation, f)
        | Text s    -> JProperty(operation, s)
        | Date d    -> JProperty(operation, d.ToString(dateTimeFormat))
        | Id i      -> JProperty(operation, i.ToString())

    let private typeFieldToString (t: TypeField) =
        match t with
        | Null    -> "null"
        | Boolean -> "boolean"
        | Number  -> "number"
        | String  -> "string"
        | Array   -> "array"
        | Object  -> "object"

    let private nameAndPropertyToJOject (name: string) (property: JProperty) =
        JProperty(name, property)

    let dataTypesToJProperty (propertyName: string) (types: DataType list) =
        let asJValues = types |> List.map (fun x -> match x with
                                                    | Bool b -> JValue(b)
                                                    | Integer i  -> JValue(i)
                                                    | Float f -> JValue(f)
                                                    | Text s -> JValue(s)
                                                    | Date d -> JValue(d.ToString(dateTimeFormat))
                                                    | Id i -> JValue(i.ToString()))
        let jArray = JArray(asJValues)
        JProperty(propertyName, jArray)

    let private conditionalOperatorToJObject (c: ConditionalOperator) =
        let object = JObject()
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

    // DO NOT DELETE THIS CONVERTER!
    // Although it is marked obsolete it is still used by the `OperatorJsonConverter`.
    // The direct use of this converter is heavily discouraged.
    //
    [<System.Obsolete("Everything moved to the OperatorJsonConverter.")>]
    type ConditionalJsonConverter() =
        inherit Newtonsoft.Json.JsonConverter()

        override this.CanConvert(t) =
            typeof<ConditionalOperator>.IsAssignableFrom(t)

        override this.ReadJson(_, _, _, _) =
            failwith "Reading this type (ConditionalOperator) is not supported."

        override this.WriteJson(writer, value, _) =
            let castValue = value :?> ConditionalOperator
            let jObject = castValue |> conditionalOperatorToJObject

            let parentJObject = JObject()
            do parentJObject.Add(castValue.name, jObject)
            do parentJObject.WriteTo(writer)


    let rec private combinationToJObject (combinator: CombinationOperator) : JObject =
        let matchOperator (o: Operator) =
            match o with
            | Combinator combinator -> combinator |> combinationToJObject
            | Conditional conditional -> let elementSelector = conditional |> conditionalOperatorToJObject
                                         let name = if conditional.parents.IsEmpty then conditional.name else System.String.Join(".", conditional.parents |> Seq.ofList) + "." + conditional.name
                                         if System.String.IsNullOrWhiteSpace(name) then
                                             elementSelector
                                         else
                                             let parent = JObject()
                                             do parent.Add(name, elementSelector)
                                             parent

        let operatorsToJObjects (os: Operator list) =
            os |> List.map matchOperator

        let combinationToJObject (operator: Operator) (combinatorName: string) : JObject =
             let operatorObject = operator |> matchOperator
             let property = JProperty(combinatorName, operatorObject)
             let subObject = JObject()
             do subObject.Add(property)
             subObject
        
        let property =  match combinator with
                        | And x -> JProperty("$and", x |> operatorsToJObjects)
                        | Or x -> JProperty("$or", x |> operatorsToJObjects)
                        | Not x -> JProperty("$not", x |> matchOperator)
                        | Nor x -> JProperty("$nor", x |> operatorsToJObjects)
                        | All x -> JProperty("$all", x |> operatorsToJObjects)
                        | ElementMatch (operator, key) ->
                             let subObject = combinationToJObject operator "$elemMatch"
                             JProperty(key, subObject)
                        | AllMatch (operator, key) ->
                             let subObject = combinationToJObject operator "$allMatch"
                             JProperty(key, subObject)
        
        let jObject = JObject()
        do jObject.Add(property)
        jObject


    // DO NOT DELETE THIS CONVERTER!
    // Although it is marked obsolete it is still used by the `OperatorJsonConverter`.
    // The direct use of this converter is heavily discouraged.
    //
    [<System.Obsolete("Everything moved to the OperatorJsonConverter.")>]
    type CombinationJsonConverter() =
        inherit Newtonsoft.Json.JsonConverter()

        override this.CanConvert(t) =
            typeof<CombinationOperator>.IsAssignableFrom(t)

        override this.ReadJson(_, _, _, _) =
            failwith "Reading this type (ConditionalOperator) is not supported."

        override this.WriteJson(writer, value, _) =
            let castValue = value :?> CombinationOperator
            let jObject = castValue |> combinationToJObject
            jObject.WriteTo(writer)


    type OperatorJsonConverter(printSerializedOperators: bool) =
        inherit Newtonsoft.Json.JsonConverter()

        let printSerializedOperators = printSerializedOperators
        
        override this.CanRead = false

        override this.CanConvert(t) =
            typeof<Operator>.IsAssignableFrom(t)

        override this.ReadJson(_, _, _, _) =
            failwith "Reading this type (ConditionalOperator) is not supported."

        override this.WriteJson(writer, value, _) =
            let castObject = value :?> Operator
            match castObject with
            | Operator.Conditional conditional -> 
                // TODO: this is a nasty hack that needs to be addressed
                let converter = ConditionalJsonConverter () :> Newtonsoft.Json.JsonConverter
                let settings = Json.settingsWithCustomConverter [ converter ]
                let serialized = Newtonsoft.Json.JsonConvert.SerializeObject(conditional, settings)
                if printSerializedOperators then 
                    do printfn "Serialized a condition to:"
                    do printfn "%s" serialized
                else ()
                let jObject = JObject.Parse(serialized)
                do jObject.WriteTo(writer)
            | Operator.Combinator combinator -> 
                // TODO: this is a nasty hack that needs to be addressed
                let converter = CombinationJsonConverter () :> Newtonsoft.Json.JsonConverter
                let settings = Json.settingsWithCustomConverter [ converter ]
                let serialized = Newtonsoft.Json.JsonConvert.SerializeObject(combinator, settings)
                if printSerializedOperators then
                    do printfn "Serialized a combinator to:"
                    do printfn "%s" serialized
                else ()
                let jObject = JObject.Parse(serialized)
                do jObject.WriteTo(writer)