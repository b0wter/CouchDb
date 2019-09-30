namespace b0wter.CouchDb.Tests.Integration.Database

module Create =
    
    open FsUnit.Xunit
    open Xunit
    open b0wter.CouchDb.Lib
    open b0wter.CouchDb.Tests.Integration
    
    type Tests() =
        inherit Utilities.CleanDatabaseTests()
        
        [<Fact>]
        member this.``Creating a new database with a valid name returns Accepted-result`` () =
            async {
                let dbName = "test-db-1"
                let! result = Database.Create.query Initialization.defaultDbProperties dbName None None
                match result with
                | Database.Create.Result.Created _ | Database.Create.Result.Accepted _ -> true
                | _ -> false
                |> should be True
            }

        [<Fact>]
        member this.``Creating a new database with an invalid name returns InvalidDbName-result`` () =
            async {
                let dbName = "00-this-[is]-{an}-inv@lid-name"
                let! result = Database.Create.query Initialization.defaultDbProperties dbName None None
                match result with
                | Database.Create.Result.InvalidDbName _ -> true
                | _ -> false
                |> should be True
            }

        [<Fact>]
        member this.``Creating a new database with an existing name returns AlreadyExists-result`` () =
            async {
                let dbName = "test-db-1"
                let createCommand = fun () -> Database.Create.query Initialization.defaultDbProperties dbName None None
                do createCommand () |> Async.RunSynchronously |> ignore
                let! result = createCommand ()
                match result with
                | Database.Create.Result.AlreadyExists _ -> true
                | _ -> false
                |> should be True
            }
            
        [<Fact>]
        member this.``Creating a new database with an empty name returns InvalidDbName-result`` () =
            async {
                let dbName = ""
                let! result = Database.Create.query Initialization.defaultDbProperties dbName None None
                match result with
                | Database.Create.Result.InvalidDbName _ -> true
                | _ -> false
                |> should be True
            }

