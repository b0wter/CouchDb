namespace b0wter.CouchDb.Tests.Integration.Documents
open b0wter.CouchDb.Tests.Integration

module Get =
    
    open FsUnit.Xunit
    open Xunit
    open b0wter.CouchDb.Lib
    open b0wter.CouchDb.Tests.Integration.CustomMatchers
    open b0wter.CouchDb.Tests.Integration.TestModels
    
    type Tests() =
        inherit Utilities.EmptySingleDatabaseTests("test-db")
        let dbName = "test-db"
        
        [<Fact>]
        member this.``Retrieving a newly-added document returns DocumentExists result`` () =
            async {
                match! Database.AddDocument.query Initialization.defaultDbProperties dbName Default.defaultInstance with
                | Database.AddDocument.Result.Created x ->
                    do x.ok |> should be True
                    do x.id |> should equal (Default.defaultInstance._id.ToString())
                    do x.rev |> should not' (be EmptyString)
                    
                    match! Documents.Get.query<Default.T> Initialization.defaultDbProperties dbName Default.defaultInstance._id [] with
                    | Documents.Get.Result.DocumentExists x ->
                        // You cannot check the complete object for equality since the returned result has a revision.
                        x.content._id |> should equal Default.defaultInstance._id
                        x.content.myInt |> should equal Default.defaultInstance.myInt
                        x.content.myFirstString |> should equal Default.defaultInstance.myFirstString
                        x.content.mySecondString |> should equal Default.defaultInstance.mySecondString
                    | x -> failwith <| sprintf "Expected NotModified but got %s" (x.GetType().FullName)
                    
                | _ -> failwith <| sprintf "Database preparation failed, could not add document to db."
                
            }
    
        [<Fact>]
        member this.``Retrieving a non-existing document returns NotFound`` () =
            async {
                let id = System.Guid.Parse("3f4ae7a0-f4f3-489b-a3b8-eba22450fae4")
                let! result = Documents.Get.query<Default.T> Initialization.defaultDbProperties dbName id []
                result |> should be (ofCase<@ Documents.Get.Result<Default.T>.NotFound @>)
            }
                
        [<Fact>]
        member this.``Retrieving a document without specifying an id returns DocumentIdMissing`` () =
            async {
                let! result = Documents.Get.query<Default.T> Initialization.defaultDbProperties dbName System.Guid.Empty []
                result |> should be (ofCase<@ Documents.Get.Result<Default.T>.DocumentIdMissing @>)
            }
                
        [<Fact>]
        member this.``Retrieving a document without specifying a db name returns DbNameMissing`` () =
            async {
                let id = System.Guid.Parse("3f4ae7a0-f4f3-489b-a3b8-eba22450fae4")
                let! result = Documents.Get.query<Default.T> Initialization.defaultDbProperties "" id []
                result |> should be (ofCase<@ Documents.Get.Result<Default.T>.DbNameMissing @>)
            }
    

