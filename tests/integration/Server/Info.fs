namespace b0wter.CouchDb.Tests.Integration.Server

module Info =
    open Xunit
    open FsUnit.Xunit
    open b0wter.CouchDb.Lib
    open b0wter.CouchDb.Tests.Integration
    
    type Tests() =
        inherit Utilities.CleanDatabaseTests()
        
        [<Fact>]
        member this.``Retrieving server info returns valid server infos`` () =
            // This tests every field to make sure that the names of the fields match the names of the response fields.
            async {
                let! result = Server.Info.query Initialization.defaultDbProperties
                match result with
                | Server.Info.Result.Success response -> 
                    response.couchdb |> should equal "Welcome"
                    response.version |> should not' <| be NullOrEmptyString
                    response.uuid |> should not' <| equal (System.Guid.Empty)
                    response.vendor.name |> should not' <| be NullOrEmptyString
                    response.git_sha |> should not' <| be NullOrEmptyString
                    response.features |> should not' <| be Empty
                | Server.Info.Result.JsonDeserialisationError e ->
                    failwith <| sprintf "The result could not be parsed:%s%s" System.Environment.NewLine e.content
                | Server.Info.Result.Unknown x ->
                    failwith x.content
            }
    

