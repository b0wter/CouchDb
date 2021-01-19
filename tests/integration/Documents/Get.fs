namespace b0wter.CouchDb.Tests.Integration.Documents
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
                    do x.Ok |> should be True
                    do x.Id |> should equal (Default.defaultInstance._id.ToString())
                    do x.Rev |> should not' (be EmptyString)
                    
                    match! Documents.Get.query<Default.T> Initialization.defaultDbProperties this.DbName Default.defaultInstance._id [] with
                    | Documents.Get.Result.DocumentExists x ->
                        // You cannot check the complete object for equality since the returned result has a revision.
                        x.Content._id |> should equal Default.defaultInstance._id
                        x.Content.myInt |> should equal Default.defaultInstance.myInt
                        x.Content.myFirstString |> should equal Default.defaultInstance.myFirstString
                        x.Content.mySecondString |> should equal Default.defaultInstance.mySecondString
                    | x -> failwith <| sprintf "Expected NotModified but got %s" (x.GetType().FullName)
                    
                | _ -> failwith <| sprintf "Database preparation failed, could not add document to db."
                
            }
        
        [<Fact>]
        member this.``Retrieving a newly-added document as JObject returns DocumentExists result`` () =
            async {
                match! Databases.AddDocument.query Initialization.defaultDbProperties this.DbName Default.defaultInstance with
                | Databases.AddDocument.Result.Created x ->
                    do x.Ok |> should be True
                    do x.Id |> should equal (Default.defaultInstance._id.ToString())
                    do x.Rev |> should not' (be EmptyString)
                    
                    match! Documents.Get.queryJObject Initialization.defaultDbProperties this.DbName Default.defaultInstance._id [] with
                    | Documents.Get.Result.DocumentExists x ->
                        x.Content.Value<string>("_id") |> should equal Default.defaultInstance._id
                        x.Content.Value<int>("myInt") |> should equal Default.defaultInstance.myInt
                        x.Content.Value<string>("myFirstString") |> should equal Default.defaultInstance.myFirstString
                        x.Content.Value<string>("mySecondString") |> should equal Default.defaultInstance.mySecondString
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
    

