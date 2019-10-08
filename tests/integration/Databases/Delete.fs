namespace b0wter.CouchDb.Tests.Integration.Databases

module Delete =
    
    open FsUnit.Xunit
    open FsUnit.CustomMatchers
    open Xunit
    open b0wter.CouchDb.Lib
    open b0wter.CouchDb.Tests.Integration
    
    type Tests() =
        inherit Utilities.EmptySingleDatabaseTests("database-delete-test")

        [<Fact>]
        member this.``Deleting an existing database results in Deleted`` () =
            async {
                let! result = Databases.Delete.query Initialization.defaultDbProperties this.DbName
                result |> should be (ofCase <@ Databases.Delete.Result.Deleted @>)
            }
            
        [<Fact>]
        member this.``Deleting an non-existing database results in NotFound`` () =
            let dbNameDeletion = this.DbName + "_non-existing-suffix"
            async {
                let! result = Databases.Delete.query Initialization.defaultDbProperties dbNameDeletion
                result |> should be (ofCase<@ Databases.Delete.Result.NotFound @>)
            }
            
        [<Fact>]
        member this.``Deleting an database with invalid name results a NotFound-result`` () =
            let dbNameInvalid = "00-this-[is]-{an}-inv@lid-name"
            async {
                let! result = Databases.Delete.query Initialization.defaultDbProperties dbNameInvalid
                result |> should be (ofCase<@ Databases.Delete.Result.NotFound @>)
            }
