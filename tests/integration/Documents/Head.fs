namespace b0wter.CouchDb.Tests.Integration.Documents

module Head =
    
    open FsUnit.Xunit
    open Xunit
    open b0wter.CouchDb.Lib
    open FsUnit.CustomMatchers
    open b0wter.CouchDb.Tests.Integration
    open b0wter.CouchDb.Tests.Integration.TestModels
    
    type Tests() =
        inherit Utilities.EmptySingleDatabaseTests("test-db")
        
        [<Fact>]
        member this.``Retrieving a newly-added document returns DocumentExists result`` () =
            async {
                match! Databases.AddDocument.query Initialization.defaultDbProperties this.DbName Default.defaultInstance with
                | Databases.AddDocument.Result.Created x ->
                    do x.ok |> should be True
                    do x.id |> should equal (Default.defaultInstance._id.ToString())
                    do x.rev |> should not' (be EmptyString)
                    
                    match! Documents.Head.query Initialization.defaultDbProperties this.DbName Default.defaultInstance._id with
                    | Documents.Head.Result.DocumentExists y ->
                        y.ETag |> should equal x.rev
                    | x -> failwith <| sprintf "Expected NotModified but got %s" (x.GetType().FullName)
                    
                | _ -> failwith <| sprintf "Database preparation failed, could not add document to db."
                
            }
    
        [<Fact>]
        member this.``Retrieving a non-existing document returns NotFound`` () =
            async {
                let id = System.Guid.Parse("3f4ae7a0-f4f3-489b-a3b8-eba22450fae4")
                let! result = Documents.Head.query Initialization.defaultDbProperties this.DbName id
                result |> should be (ofCase<@ Documents.Head.Result.NotFound @>)
            }
                
        [<Fact>]
        member this.``Retrieving a document without specifying an id returns DocumentIdMissing`` () =
            async {
                let! result = Documents.Head.query Initialization.defaultDbProperties this.DbName System.Guid.Empty
                result |> should be (ofCase<@ Documents.Head.Result.DocumentIdMissing @>)
            }
                
        [<Fact>]
        member this.``Retrieving a document without specifying a db name returns DbNameMissing`` () =
            async {
                let id = System.Guid.Parse("3f4ae7a0-f4f3-489b-a3b8-eba22450fae4")
                let! result = Documents.Head.query Initialization.defaultDbProperties "" id
                result |> should be (ofCase<@ Documents.Head.Result.DbNameMissing @>)
            }
    
    

