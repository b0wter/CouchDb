namespace b0wter.CouchDb.Tests.Integration.Databases

module AddDocument =
    
    open FsUnit.Xunit
    open Xunit
    open b0wter.CouchDb.Lib
    open b0wter.CouchDb.Tests.Integration
    open FsUnit.CustomMatchers
    
    type Tests() =
        inherit Utilities.EmptySingleDatabaseTests("test-db")

        [<Fact>]
        member this.``Adding a document to an existing database returns Created`` () =
            async {
                let! result = Databases.AddDocument.query Initialization.defaultDbProperties this.DbName TestModels.Default.defaultInstance
                result |> should be (ofCase <@ Databases.AddDocument.Result.Created @>) 
            }
            
        [<Fact>]
        member this.``Adding a document to a non-existing database returns NotFound`` () =
            let dbNameNonExisting = this.DbName + "_non-existing"
            async {
                let! result = Databases.AddDocument.query Initialization.defaultDbProperties dbNameNonExisting TestModels.Default.defaultInstance
                result |> should be (ofCase <@ Databases.AddDocument.Result.DbDoesNotExist @>)
            }
            
        [<Fact>]
        member this.``Adding a null document returns DocumentIsNull`` () =
            async {
                let! result = Databases.AddDocument.query Initialization.defaultDbProperties this.DbName null
                result |> should be (ofCase <@ Databases.AddDocument.Result.DocumentIsNull @>)
            }
            
        [<Fact>]
        member this.``Adding a document using an invalid database name returns InvalidDbName`` () =
            async {
                let! result = Databases.AddDocument.query Initialization.defaultDbProperties "00-this-[is]-{an}-inv@lid-name" TestModels.Default.defaultInstance
                result |> should be (ofCase <@ Databases.AddDocument.Result.DbDoesNotExist @>)
            }
            
        [<Fact>]
        member this.``Adding a document with an id that is in use returns Conflict`` () =
            async {
                let! firstInsert = Databases.AddDocument.query Initialization.defaultDbProperties this.DbName TestModels.Default.defaultInstance
                firstInsert |> should be (ofCase <@ Databases.AddDocument.Result.Created @>)
                let! secondInsert = Databases.AddDocument.query Initialization.defaultDbProperties this.DbName TestModels.Default.defaultInstance
                secondInsert |> should be (ofCase <@ Databases.AddDocument.Result.DocumentIdConflict @>)
            }
            
