namespace b0wter.CouchDb.Tests.Integration.Server

module DbsInfo =
    open Xunit
    open FsUnit.Xunit
    open b0wter.CouchDb.Lib
    open b0wter.CouchDb.Tests.Integration
    open FsUnit.CustomMatchers
    
    type Tests() =
        inherit Utilities.CleanDatabaseTests()
    
        [<Fact>]
        member this.``Retrieving server details for existing databases returns database infos`` () =
            async {
                // This method checks the properties:
                //    - db_name
                //    - disk_format_version
                // for proper values to determine if it contains useful information.
                // Note that the CouchDb server should not return information for databases it cannot find and thus
                // this test should not be necessary).

                // **NOTE**: There used to be a `disk_size` check but it would not report proper results
                //           when run on Azure DevOps so I had to remove it.

                let validateInfo (response: Server.DbsInfo.Response) =
                    match response.Info with
                    | Some info ->
                        info.DbName |> should not' <| be NullOrEmptyString
                        info.DiskFormatVersion |> should be (greaterThan 0)
                    | None -> failwith <| sprintf "The response does not contain a proper Info instance for database: '%s'." response.Key
                    
                do match Initialization.createDatabases false [ "test-db-1"; "test-db-2" ] |> Async.RunSynchronously with Ok _ -> () | Error e -> failwith e
                    
                let! result = Server.DbsInfo.query Initialization.defaultDbProperties [ "test-db-1"; "test-db-2" ]
                match result with
                | Server.DbsInfo.Result.Success response ->
                    do response.Length |> should equal 2
                    do response |> Array.iter validateInfo
                | Server.DbsInfo.Result.KeyError _ ->
                    failwith "Returned a KeyError where a Success was expected."
                | Server.DbsInfo.Result.JsonDeserialisationError e ->
                    failwith <| sprintf "The result could not be parsed:%s%s" System.Environment.NewLine e.Content
                | Server.DbsInfo.Result.Unknown f ->
                    failwith <| sprintf "Returned a Failure where a Success was expected. Reason: %s" f.Content
            }
            
        [<Fact>]
        member this.``Retrieving server details for non-existing database returns no database infos`` () =
            async {
                let! result = Server.DbsInfo.query Initialization.defaultDbProperties [ "unknown-db-1"; "unknown-db-2" ]
                match result with
                | Server.DbsInfo.Result.Success s ->
                    do s.Length |> should equal 2
                    do s |> Array.forall (fun x -> x.Info.IsNone) |> should be True
                | Server.DbsInfo.Result.KeyError e -> failwith <| sprintf "Encountered a KeyError response. This request needs to set keys! Details :%s" e.Content
                | Server.DbsInfo.Result.Unknown e -> failwith <| sprintf "Encountered an error, details: %s" e.Content
                | Server.DbsInfo.Result.JsonDeserialisationError e ->
                    failwith <| sprintf "The result could not be parsed:%s%s" System.Environment.NewLine e.Content
            }
            
        [<Fact>]
        member this.``Retrieving server details without supplying keys returns an error`` () =
            async {
                let! result = Server.DbsInfo.query Initialization.defaultDbProperties [ ]
                result |> should be (ofCase <@ Server.DbsInfo.Result.KeyError @>)
            }
    
    


