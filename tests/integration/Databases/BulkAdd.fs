namespace b0wter.CouchDb.Tests.Integration.Databases

module BulkAdd =
    
    // TODO: Add tests for ExpectationFailed.
    
    open FsUnit.Xunit
    open Xunit
    open b0wter.CouchDb.Lib
    open b0wter.CouchDb.Tests.Integration
    open FsUnit.CustomMatchers
    
    let id1 = System.Guid.Parse("39346820-3700-4d09-b86c-e68653c98ca7")
    let id2 = System.Guid.Parse("c51b3eae-73a5-4e18-9c29-701645cfb91e")
    let id3 = System.Guid.Parse("94429f08-0b16-4076-be3e-bc47d4deea21")
    
    let model1 = DocumentTestModels.Default.create (id1, 1, "one",   "eno",   2.5,  System.DateTime(1980, 1, 1, 12, 0, 0))
    let model2 = DocumentTestModels.Default.create (id2, 2, "one",   "owt",   2.5,  System.DateTime(1990, 10, 10, 20, 0, 0))
    let model3 = DocumentTestModels.Default.create (id3, 3, "three", "eerht", 3.14, System.DateTime(2000, 10, 10, 20, 0, 0))
    
    type Tests() =
        inherit Utilities.EmptySingleDatabaseTests("database-find-tests")
        
        [<Fact>]
        member this.``Adding documents to an empty database returns Created with only Success results`` () =
            async {
                let models = [model1; model2; model3]
                let! result = Databases.BulkAdd.query Initialization.defaultDbProperties this.DbName models
                do result |> should be (ofCase <@ Databases.BulkAdd.Result.Created @>)
                
                match result with
                | Databases.BulkAdd.Result.Created c ->
                    c |> should haveLength 3
                    c |> List.iter (fun x -> x |> should be (ofCase <@ Databases.BulkAdd.InsertResult.Success @>))
                | _ -> failwith "Adding the documents returned a Result that is not Created."
            }
    
        [<Fact>]
        member this.``Adding same documents twice returns returns Created with only Failure results`` () =
            async {
                let models = [model1; model2; model3]
                let! first = Databases.BulkAdd.query Initialization.defaultDbProperties this.DbName models
                do first |> should be (ofCase <@ Databases.BulkAdd.Result.Created @>)
                
                let! second = Databases.BulkAdd.query Initialization.defaultDbProperties this.DbName models
                do second |> should be (ofCase <@ Databases.BulkAdd.Result.Created @>)
                match second with
                | Databases.BulkAdd.Result.Created c ->
                    c |> should haveLength 3
                    c |> List.iter (fun x -> x |> should be (ofCase <@ Databases.BulkAdd.InsertResult.Failure @>))
                | _ -> failwith "Adding the documents returned a Result that is not Created."
            }
            
        [<Fact>]
        member this.``Adding documents to a non-existing db returns NotFound`` () =
            async {
                let nonExistingDbName = this.DbName + "_non_existing"
                let models = [model1; model2; model3]
                let! result = Databases.BulkAdd.query Initialization.defaultDbProperties nonExistingDbName models
                do result |> should be (ofCase <@ Databases.BulkAdd.Result.NotFound @>)
            }

