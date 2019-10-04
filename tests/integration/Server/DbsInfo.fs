namespace b0wter.CouchDb.Tests.Integration.Server

module DbsInfo =
    open Xunit
    open FsUnit.Xunit
    open b0wter.CouchDb.Lib
    open b0wter.CouchDb.Tests.Integration
    open CustomMatchers
    
    type Tests() =
        inherit Utilities.CleanDatabaseTests()
    
        [<Fact>]
        member this.``Retrieving server details for existing databases returns database infos`` () =
            async {
                // This method checks the properties:
                //    - db_name
                //    - disk_format_version
                //    - disk_size
                // for proper values to determine if it contains useful information.
                // Note that the CouchDb server should not return information for databases it cannot find and thus
                // this test should not be necessary).
                let validateInfo (response: Server.DbsInfo.Response) =
                    match response.info with
                    | Some info ->
                        info.db_name |> should not' <| be NullOrEmptyString
                        info.disk_format_version |> should be (greaterThan 0)
                        info.disk_size |> should be (greaterThan 0)
                    | None -> failwith <| sprintf "The response does not contain a proper Info instance for database: '%s'." response.key
                    
                do if Initialization.createDatabases [ "test-db-1"; "test-db-2" ] |> Async.RunSynchronously = true then () else failwith "The database preparation failed."
                    
                let! result = Server.DbsInfo.query Initialization.defaultDbProperties [ "test-db-1"; "test-db-2" ]
                match result with
                | Server.DbsInfo.Result.Success response ->
                    do response.Length |> should equal 2
                    do response |> Array.iter validateInfo
                | Server.DbsInfo.Result.KeyError _ ->
                    failwith "Returned a KeyError where a Success was expected."
                | Server.DbsInfo.Result.JsonDeserialisationError e ->
                    failwith <| sprintf "The result could not be parsed, json: %s | reason: %s" e.json e.reason
                | Server.DbsInfo.Result.Unknown f ->
                    failwith <| sprintf "Returned a Failure where a Success was expected. Reason: %s" f.content
            }
            
        [<Fact>]
        member this.``Retrieving server details for non-existing database returns no database infos`` () =
            async {
                let! result = Server.DbsInfo.query Initialization.defaultDbProperties [ "unknown-db-1"; "unknown-db-2" ]
                match result with
                | Server.DbsInfo.Result.Success s ->
                    do s.Length |> should equal 2
                    do s |> Array.forall (fun x -> x.info.IsNone) |> should be True
                | Server.DbsInfo.Result.KeyError e -> failwith <| sprintf "Encountered a KeyError response. This request needs to set keys! Details :%s" e.content
                | Server.DbsInfo.Result.Unknown e -> failwith <| sprintf "Encountered an error, details: %s" e.content
                | Server.DbsInfo.Result.JsonDeserialisationError e ->
                    failwith <| sprintf "The result could not be parsed, json: %s | reason: %s" e.json e.reason
            }
            
        [<Fact>]
        member this.``Retrieving server details without supplying keys returns an error`` () =
            async {
                let! result = Server.DbsInfo.query Initialization.defaultDbProperties [ ]
                result |> should be (ofCase <@ Server.DbsInfo.Result.KeyError @>)
            }
    
    


