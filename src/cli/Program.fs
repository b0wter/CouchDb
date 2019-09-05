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

                let platoon = data.platoons |> List.last
                let result = Database.AddDocument.query p "test-db" platoon |> Async.RunSynchronously

                do printfn "%A" result

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
    

