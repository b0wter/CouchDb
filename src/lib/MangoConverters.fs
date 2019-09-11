namespace b0wter.CouchDb.Lib

    module MangoConverters =

        open Newtonsoft.Json.Linq
        open Mango

        let private conditionalOperatorToJObject (c: ConditionalOperator) =
            let object = JObject()

            match c with
            