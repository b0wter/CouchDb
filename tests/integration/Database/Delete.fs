namespace b0wter.CouchDb.Tests.Integration.Database
open b0wter.CouchDb.Tests.Integration

module Delete =
    
    open FsUnit.Xunit
    open Xunit
    open b0wter.CouchDb.Lib
    open b0wter.FSharp
    
    type Tests() =
        inherit Utilities.PrefilledDatabaseTests()

        [<Fact>]
        member this.``Deleting an existing database results in deletion`` () =
            this.FailIfNotInitialized ()
            let dbName = "database-delete-test-1" 
            let toRun = fun () ->
                async {
                    let! result = Database.Delete.query Initialization.defaultDbProperties dbName
                    result |> Union.isCase <@ Database.Delete.Result.Deleted @> |> should be True
                }
            this.RunWithDatabases [ dbName ] toRun
            
        [<Fact>]
        member this.``Deleting an non-existing database results in deletion`` () =
            this.FailIfNotInitialized ()
            let dbNameCreation = "database-delete-test-2" 
            let dbNameDeletion = "database-delete-test-2-non-existing" 
            let toRun = fun () ->
                async {
                    let! result = Database.Delete.query Initialization.defaultDbProperties dbNameDeletion
                    result |> Union.isCase <@ Database.Delete.Result.NotFound @> |> should be True
                }
            this.RunWithDatabases [ dbNameCreation ] toRun
            
        [<Fact>]
        member this.``Deleting an database with invalid name results a NotFound-result`` () =
            this.FailIfNotInitialized ()
            let dbName = "00-this-[is]-{an}-inv@lid-name"
            let toRun = fun () ->
                async {
                    let! result = Database.Delete.query Initialization.defaultDbProperties dbName
                    result |> Union.isCase <@ Database.Delete.Result.NotFound @> |> should be True
                }
            this.Run toRun
