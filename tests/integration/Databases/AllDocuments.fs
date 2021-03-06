namespace b0wter.CouchDb.Tests.Integration.Databases

module AllDocuments =
    
    open FsUnit.Xunit
    open Xunit
    open b0wter.CouchDb.Lib
    open b0wter.CouchDb.Tests.Integration
    open FsUnit.CustomMatchers
    
    type Tests() =
        inherit Utilities.EmptySingleDatabaseTests("test-db")
        
        [<Fact>]
        member this.``Retrieving all documents for an empty but existing db returns empty result`` () =
            async {
                //TODO: Add documents :D
                let! result = Databases.AllDocuments.queryAll Initialization.defaultDbProperties this.DbName
                match result with
                | Databases.AllDocuments.Success s ->
                    s.TotalRows |> should equal 0
                    s.Rows |> should be Empty
                | _ -> failwith <| sprintf "Expected Database.AllDocuments.Success but got a %s" (result.GetType().FullName)
            }
            
        [<Fact>]
        member this.``Retrieving all documents for non-existing db returns failure`` () =
            async {
                let nonExistingDbName = "this-does-not-exist"
                let! result = Databases.AllDocuments.queryAll Initialization.defaultDbProperties nonExistingDbName
                result |> should be (ofCase <@ Databases.AllDocuments.NotFound @>)
            }
            
        [<Fact>]
        member this.``Retrieving selected documents for non-existing db returns NotFound`` () =
            async {
                let nonExistingDbName = "this-does-not-exist"
                let! result = Databases.AllDocuments.querySelected Initialization.defaultDbProperties nonExistingDbName [ "a"; "b"; "b" ]
                result |> should be (ofCase <@ Databases.AllDocuments.NotFound @>)
            }
