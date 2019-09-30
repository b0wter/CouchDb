namespace b0wter.CouchDb.Tests.Integration.Server

module Details =
    open Xunit
    open FsUnit.Xunit
    open b0wter.CouchDb.Lib
    open b0wter.CouchDb.Tests.Integration
    
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
                let validateInfo (response: Server.Details.Response) =
                    match response.info with
                    | Some info ->
                        info.db_name |> should not' <| be NullOrEmptyString
                        info.disk_format_version |> should be (greaterThan 0)
                        info.disk_size |> should be (greaterThan 0)
                    | None -> failwith <| sprintf "The response does not contain a proper Info instance for database: '%s'." response.key
                    
                do if Initialization.createDatabases [ "test-db-1"; "test-db-2" ] |> Async.RunSynchronously = true then () else failwith "The database preparation failed."
                    
                let! result = Server.Details.query Initialization.defaultDbProperties [ "test-db-1"; "test-db-2" ]
                match result with
                | Server.Details.Result.Success response ->
                    do response.Length |> should equal 2
                    do response |> Array.iter validateInfo
                | Server.Details.Result.KeyError _ ->
                    failwith "Returned a KeyError where a Success was expected."
                | Server.Details.Result.Failure f ->
                    failwith <| sprintf "Returned a Failure where a Success was expected. Reason: %s" f.reason
            }
            
        [<Fact>]
        member this.``Retrieving server details for non-existing database returns no database infos`` () =
            async {
                let! result = Server.Details.query Initialization.defaultDbProperties [ "unknown-db-1"; "unknown-db-2" ]
                match result with
                | Server.Details.Result.Success s ->
                    do s.Length |> should equal 2
                    do s |> Array.forall (fun x -> x.info.IsNone) |> should be True
                | Server.Details.Result.KeyError e -> failwith <| sprintf "Encountered a KeyError response. This request needs to set keys! Details :%s" e.reason
                | Server.Details.Result.Failure e -> failwith <| sprintf "Encountered an error, details: %s" e.reason
            }
            
        [<Fact>]
        member this.``Retrieving server details without supplying keys returns an error`` () =
            async {
                let! result = Server.Details.query Initialization.defaultDbProperties [ ]
                match result with
                | Server.Details.Result.Success s -> failwith <| sprintf "Returned success for a request that should have failed. Details: %A" s
                | Server.Details.Result.KeyError x -> x.statusCode |> should equal 400
                | Server.Details.Result.Failure x -> failwith <| sprintf "The request failed in an unexpected way. Details: %s" x.reason
            }
    
    


