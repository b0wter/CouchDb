open b0wter.CouchDb.Lib
open b0wter.FSharp

[<EntryPoint>]
let main argv =
    async {
        
        let json = System.IO.File.ReadAllText("/home/b0wter/downloads/test_data.json")
        let data = Newtonsoft.Json.JsonConvert.DeserializeObject<SharedEntities.Models.ArrangedDocumentContents.T>(json)
        
        let credentials = Credentials.create ("admin", "password")
        let props = DbProperties.create ("localhost", 5984, credentials, DbProperties.ConnectionType.Http)

        match props with
        | DbProperties.DbPropertiesCreateResult.HostIsEmpty ->
            printfn "Hostname fehlt"
            return 1
        | DbProperties.DbPropertiesCreateResult.PortIsInvalid ->
            printfn "Port ist ungültig"
            return 1
        | DbProperties.DbPropertiesCreateResult.Valid p ->
            match! Core.authenticate p with
            | Ok _ ->
                (*
                let! exists = Database.Exists.query p "test-db"
                let result = match exists with
                             | Database.Exists.Result.Exists -> "exists"
                             | Database.Exists.Result.DoesNotExist -> "does not exist"
                             | Database.Exists.Result.RequestError e -> "request error: " + e.reason
                //let result = exists |> Result.mapBoth (fun b -> b |> string) (fun e -> e |> string) //|> (fun (r: Result<string, string>) -> match r with Ok a -> a | Error b -> b)
                *)
                //let! result = Database.Details.queryMultiple p ["test-db"] //match! Database.Details.querySingle "test-db" with
                //let! result = Server.Info.query p
                let addToDb = fun x -> Database.AddDocument.query p "test-db" x |> Async.RunSynchronously

                //let platoon = data.platoons |> List.last
                //let result = Database.AddDocument.query p "test-db" platoon |> Async.RunSynchronously
                //let result = Database.AllDocuments.query p "test-db" |> Async.RunSynchronously
                //let! result = Server.Details.query p ["test-db2"]

                //let! result = Database.AllDocuments.queryAll p "test-db"
                //let! result = Database.AllDocuments.querySelected p "test-db" [ "791f157c2e2003dd065c12973d043781" ]
                //let! result = Database.AllDocuments.querySelected p "test-db" [ "garbage :D" ]
                let findSelector1 = Find.TypedSelector("name", "4. Zug", id) //Selector.StringSelector { Selector.TypedSelector.property = "name"; Selector.TypedSelector.value = "4. Zug" }
                let findSelector2 = Find.TypedSelector("type", "platoon", id) //Selector.StringSelector { Selector.TypedSelector.property = "name"; Selector.TypedSelector.value = "4. Zug" }
                let multiSelector = Find.MultiSelector([findSelector1; findSelector2])
                //let findSelector = Find.TypedSubFieldSelector("name", ["this"; "is"; "nested"], "4. Zug", id)
                let findParams = Find.createExpression multiSelector
                let! result = Database.Find.query<SharedEntities.Models.Platoon.T> p "test-db" findParams

                //do printfn "%A" result

                let conditional = { 
                                    b0wter.CouchDb.Lib.Mango.ConditionalOperator.name = "name"; 
                                    b0wter.CouchDb.Lib.Mango.ConditionalOperator.parents = []; 
                                    b0wter.CouchDb.Lib.Mango.ConditionalOperator.operation = Mango.Condition.Equal (Mango.DataType.String "4. Zug")
                }
                let combination = Mango.CombinationOperator.And [ conditional |> Mango.Operator.Conditional ] |> Mango.Operator.Combinator
                let combination2 = Mango.CombinationOperator.And [ combination ] |> Mango.Operator.Combinator
                let expression = Mango.createExpression combination2 //(Mango.Operator.Conditional combination)
                let operatorConverter = MangoConverters.OperatorJsonConverter() :> Newtonsoft.Json.JsonConverter
                let jsonSettings = Utilities.Json.jsonSettingsWithCustomConverter [ operatorConverter; FifteenBelow.Json.UnionConverter() :> Newtonsoft.Json.JsonConverter ]
                let serialized = Newtonsoft.Json.JsonConvert.SerializeObject(expression, jsonSettings)
                do printfn "%s" serialized

                (*
                //let inProperty = MangoConverters.dataTypesToJProperty "$in" [ Mango.DataType.Bool false; Mango.DataType.String "my string"; Mango.DataType.Int 16 ]
                let conditional = { 
                                    b0wter.CouchDb.Lib.Mango.ConditionalOperator.name = "name"; 
                                    b0wter.CouchDb.Lib.Mango.ConditionalOperator.parents = []; 
                                    b0wter.CouchDb.Lib.Mango.ConditionalOperator.operation = Mango.Condition.In [ Mango.DataType.Bool false; Mango.DataType.String "my string"; Mango.DataType.Int 16 ]
                }
                let expression = Mango.createExpression (Mango.Operator.Conditional conditional)
                let converter = MangoConverters.ConditionalJsonConverter ()
                let jsonSettings = Utilities.Json.jsonSettingsWithCustomConverter [ converter; FifteenBelow.Json.UnionConverter() :> Newtonsoft.Json.JsonConverter ]
                let serialized = Newtonsoft.Json.JsonConvert.SerializeObject(expression, jsonSettings)
                do printfn "%s" serialized 
                *)

                //let savedPlatoons = data.platoons |> List.map addToDb
                //let savedAssignments = data.assignments |> List.map addToDb

                
                //let! result = Database.AddDocument.query p "test-db" test
                            //| Database.Details.SingleResult.Failure e ->
                //do printfn "%A" savedPlatoons
                //do printfn "%A" savedAssignments
                
                return 0
                (*
                let! x = Database.all p
                match! Database.all p with
                | Ok names ->
                    do printfn "%A" names
                    return 0
                | Error reason ->
                    do printfn "%s" reason
                    return 1
                    *)
            | Error e ->
                printfn "Fehlerhafte Anfrage: %s (%i)" e.reason e.statusCode
                return 1
    } |> Async.RunSynchronously
    

