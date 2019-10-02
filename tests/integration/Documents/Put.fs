namespace b0wter.CouchDb.Tests.Integration.Documents

module Put =
    
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

    let getTestDocumentId (doc: TestDocument) = doc._id
    let getTestDocumentRev (doc: TestDocument) = doc._rev

    type Tests() =
        inherit Utilities.PrefilledDatabaseTests("test-db")
        let dbName = "test-db"

        [<Fact>]
        member this.``Putting a non-existing document returns Created`` () =
            async {
                let! result = Documents.Put.query Initialization.defaultDbProperties dbName getTestDocumentId getTestDocumentRev testDocumentWithId
                result |> should be (ofCase <@ Documents.Put.Result.Created @>)
            }

        [<Fact>]
        member this.``Putting an existing document returns Created and keeps number of documents`` () =
            async {
                let! first = Documents.Put.query Initialization.defaultDbProperties dbName getTestDocumentId getTestDocumentRev testDocumentWithId
                match first with
                | Documents.Put.Result.Created x ->
                    
                    // add the document for the first time
                    //
                    let newDocument = { testDocumentWithId with _rev = Some x.rev; myInt = 1337 }
                    let! second = Documents.Put.query Initialization.defaultDbProperties dbName getTestDocumentId getTestDocumentRev newDocument
                    second |> should be (ofCase <@ Documents.Put.Result.Created @>)

                    // update the document using the rev from the previous step
                    //
                    let! check = Documents.Get.query<TestDocument> Initialization.defaultDbProperties dbName testDocumentWithId._id []
                    match check with
                    | Documents.Get.Result.DocumentExists x -> x.content.myInt |> should equal 1337
                    | _ -> failwith "The retrieval of the document (using Documents.Info) failed!"
                    
                    // check that there is only a single document in the database
                    //
                    let! count = Server.DbsInfo.query Initialization.defaultDbProperties [ dbName ]
                    match count with
                    | Server.DbsInfo.Result.Success s -> s.[0].info.Value.doc_count |> should equal 1
                    | _ -> failwith "Putting the updated document resulted in the creation of a new document!"

                | _ -> failwith "Adding the initial document failed."

            }

        [<Fact>]
        member this.``Putting a document with an id that already exists returns Conflict`` () =
            async {
                let! first = Documents.Put.query Initialization.defaultDbProperties dbName getTestDocumentId getTestDocumentRev testDocumentWithId 
                match first with
                | Documents.Put.Result.Created x -> 
                    let! second = Documents.Put.query Initialization.defaultDbProperties dbName getTestDocumentId getTestDocumentRev testDocumentWithId
                    second |> should be (ofCase <@ Documents.Put.Result.Conflict @>)
                | _ -> failwith "Adding the initial document failed."
            }

        [<Fact>]
        member this.``Putting a document with a non-existing database name returns NotFound`` () =
            async {
                let! first = Documents.Put.query Initialization.defaultDbProperties "non-existing-db" getTestDocumentId getTestDocumentRev testDocumentWithId
                first |> should be (ofCase <@ Documents.Put.Result.NotFound @>)
            }

        [<Fact>]
        member this.``Putting a document with an empty database name returns DbNameMissing`` () =
            async {
                let! first = Documents.Put.query Initialization.defaultDbProperties "" getTestDocumentId getTestDocumentRev testDocumentWithId
                first |> should be (ofCase <@ Documents.Put.Result.DbNameMissing @>)
            }
            
        [<Fact>]
        member this.``Putting a document with an empty id returns DocumentIdMissing`` () =
            async {
                let! first = Documents.Put.query Initialization.defaultDbProperties dbName (fun _ -> System.Guid.Empty) getTestDocumentRev testDocumentWithId
                first |> should be (ofCase <@ Documents.Put.Result.DocumentIdMissing @>)
            }
