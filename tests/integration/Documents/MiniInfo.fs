namespace b0wter.CouchDb.Tests.Integration.Documents

module MiniInfo =
    
    open FsUnit.Xunit
    open Xunit
    open b0wter.CouchDb.Lib
    open b0wter.FSharp.Operators
    open b0wter.CouchDb.Tests.Integration.CustomMatchers
    open b0wter.CouchDb.Tests.Integration
    
    type TestDocument = {
        _id: System.Guid
        _rev: string option
        myInt: int
        myFirstString: string
        mySecondString: string
    }
    
    let testDocumentWithId = {
        _id = System.Guid.Parse("c8cb91dc-1121-43de-a858-0742327ff158")
        _rev = None
        myInt = 42
        myFirstString = "foo"
        mySecondString = "bar"
    }
    
    type Tests() =
        inherit Utilities.PrefilledDatabaseTests("test-db")
        let dbName = "test-db"
        
        [<Fact>]
        member this.``Retrieving a newly-added document returns DocumentExists result`` () =
            async {
                match! Database.AddDocument.query Initialization.defaultDbProperties dbName testDocumentWithId with
                | Database.AddDocument.Result.Created x ->
                    do x.ok |> should be True
                    do x.id |> should equal (testDocumentWithId._id.ToString())
                    do x.rev |> should not' (be EmptyString)
                    
                    match! Documents.MiniInfo.query Initialization.defaultDbProperties dbName testDocumentWithId._id with
                    | Documents.MiniInfo.Result.DocumentExists y ->
                        y.ETag |> should equal x.rev
                    | x -> failwith <| sprintf "Expected NotModified but got %s" (x.GetType().FullName)
                    
                | _ -> failwith <| sprintf "Database preparation failed, could not add document to db."
                
            }
    
        [<Fact>]
        member this.``Retrieving a non-existing document returns NotFound`` () =
            async {
                let id = System.Guid.Parse("3f4ae7a0-f4f3-489b-a3b8-eba22450fae4")
                let! result = Documents.MiniInfo.query Initialization.defaultDbProperties dbName id
                result |> should be (ofCase<@ Documents.MiniInfo.Result.NotFound @>)
            }
                
        [<Fact>]
        member this.``Retrieving a document without specifying an id returns DocumentIdMissing`` () =
            async {
                let! result = Documents.MiniInfo.query Initialization.defaultDbProperties dbName null
                result |> should be (ofCase<@ Documents.MiniInfo.Result.DocumentIdMissing @>)
            }
                
        [<Fact>]
        member this.``Retrieving a document without specifying a db name returns DbNameMissing`` () =
            async {
                let id = System.Guid.Parse("3f4ae7a0-f4f3-489b-a3b8-eba22450fae4")
                let! result = Documents.MiniInfo.query Initialization.defaultDbProperties "" id
                result |> should be (ofCase<@ Documents.MiniInfo.Result.DbNameMissing @>)
            }
    
    

