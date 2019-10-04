namespace b0wter.CouchDb.Tests.Integration.Server

module AllDbs =
    
    open FsUnit.Xunit
    open Xunit
    open b0wter.CouchDb.Lib
    open b0wter.CouchDb.Tests.Integration
    open b0wter.FSharp.Operators
    
    type Tests() =
        inherit Utilities.CleanDatabaseTests()

        [<Fact>]
        member this.``Querying all databases on a prefilled database returns all databases`` () =
            async {
                let dbNames = [ "test-db-1"; "test-db-2"; "test-db-3" ]
                match! Initialization.createDatabases dbNames with
                | true ->
                    let! result = Server.AllDbs.query Initialization.defaultDbProperties
                    match result with
                    | Server.AllDbs.Result.Success s ->
                        s |> should haveLength dbNames.Length
                        dbNames |> List.iter (fun x -> s |> should contain x)
                    | Server.AllDbs.Result.JsonDeserialisationError e ->
                        failwith <| sprintf "The result could not be parsed:%s%s" System.Environment.NewLine e.content
                    | Server.AllDbs.Result.Unknown e ->
                        failwith <| sprintf "Request returned an error (status code: %i): %s" (e.statusCode |?| -1) e.content
                | false ->
                   return failwith "The database creation (preparation) failed."
            }

        [<Fact>]
        member this.``Querying empty server returns empty list`` () =
            async {
                let! result = Server.AllDbs.query Initialization.defaultDbProperties
                match result with
                | Server.AllDbs.Result.Success s ->
                    s |> should be Empty
                | Server.AllDbs.Result.JsonDeserialisationError e ->
                    failwith <| sprintf "The result could not be parsed:%s%s" System.Environment.NewLine e.content
                | Server.AllDbs.Result.Unknown _ ->
                    failwith "The result is not empty when it should be."
            }