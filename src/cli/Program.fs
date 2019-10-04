open b0wter.CouchDb.Lib
open b0wter.FSharp

[<EntryPoint>]
let main argv =
    async {
        
        //let json = System.IO.File.ReadAllText("/home/b0wter/downloads/test_data.json")
        //let data = Newtonsoft.Json.JsonConvert.DeserializeObject<SharedEntities.Models.ArrangedDocumentContents.T>(json)
        
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
            match! Server.Authenticate.query p with
            | Server.Authenticate.Result.Success _ ->
                printfn "Finished program."
                return 0
            | _ ->
                printfn "Authentifizierung fehlgeschlagen." 
                return 1
    } |> Async.RunSynchronously
    

