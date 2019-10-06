namespace b0wter.CouchDb.Tests.Integration.Database

module AllDocuments =
    
    open FsUnit.Xunit
    open Xunit
    open b0wter.CouchDb.Lib
    open b0wter.CouchDb.Tests.Integration
    open CustomMatchers
    
    type Tests() =
        inherit Utilities.EmptySingleDatabaseTests("test-db")
        let dbName = "test-db"
        
        [<Fact>]
        member this.``Retrieving all documents for an empty but existing db returns empty result`` () =
            async {
                //TODO: Add documents :D
                let! result = Database.AllDocuments.queryAll Initialization.defaultDbProperties dbName
                match result with
                | Database.AllDocuments.Success s ->
                    s.total_rows |> should equal 0
                    s.rows |> should be Empty
                | _ -> failwith <| sprintf "Expected Database.AllDocuments.Success but got a %s" (result.GetType().FullName)
            }
            
        [<Fact>]
        member this.``Retrieving all documents for non-existing db returns failure`` () =
            async {
                let nonExistingDbName = "this-does-not-exist"
                let! result = Database.AllDocuments.queryAll Initialization.defaultDbProperties nonExistingDbName
                result |> should be (ofCase <@ Database.AllDocuments.NotFound @>)
            }
