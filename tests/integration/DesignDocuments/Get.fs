namespace b0wter.CouchDb.Tests.Integration.DesignDocuments
open b0wter.CouchDb.Tests.Integration

module Get =
    
    open FsUnit.Xunit
    open Xunit
    open b0wter.CouchDb.Lib
    open FsUnit.CustomMatchers
    open b0wter.CouchDb.Tests.Integration.DocumentTestModels
    
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
                    
                    match! Documents.Get.query<Default.T> Initialization.defaultDbProperties this.DbName Default.defaultInstance._id [] with
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
                let id = "3f4ae7a0-f4f3-489b-a3b8-eba22450fae4"
                let! result = Documents.Get.query<Default.T> Initialization.defaultDbProperties this.DbName id []
                result |> should be (ofCase<@ Documents.Get.Result<Default.T>.NotFound @>)
            }
                
        [<Fact>]
        member this.``Retrieving a document without specifying an id returns DocumentIdMissing`` () =
            async {
                let! result = Documents.Get.query<Default.T> Initialization.defaultDbProperties this.DbName System.String.Empty []
                result |> should be (ofCase<@ Documents.Get.Result<Default.T>.DocumentIdMissing @>)
            }
                
        [<Fact>]
        member this.``Retrieving a document without specifying a db name returns DbNameMissing`` () =
            async {
                let id = "3f4ae7a0-f4f3-489b-a3b8-eba22450fae4"
                let! result = Documents.Get.query<Default.T> Initialization.defaultDbProperties "" id []
                result |> should be (ofCase<@ Documents.Get.Result<Default.T>.DbNameMissing @>)
            }
    
