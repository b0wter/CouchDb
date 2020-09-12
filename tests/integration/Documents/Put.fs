namespace b0wter.CouchDb.Tests.Integration.Documents

module Put =
    
    open FsUnit.Xunit
    open Xunit
    open b0wter.CouchDb.Lib
    open FsUnit.CustomMatchers
    open b0wter.CouchDb.Tests.Integration
    open b0wter.CouchDb.Tests.Integration.DocumentTestModels
    
    let getTestDocumentId (doc: Default.T) = doc._id
    let getTestDocumentRev (doc: Default.T) = doc._rev

    type Tests() =
        inherit Utilities.EmptySingleDatabaseTests("test-db")
        let dbName = "test-db"

        [<Fact>]
        member this.``Putting a non-existing document returns Created`` () =
            async {
                let! result = Documents.Put.query Initialization.defaultDbProperties dbName getTestDocumentId getTestDocumentRev Default.defaultInstance
                result |> should be (ofCase <@ Documents.Put.Result.Created @>)
            }

        [<Fact>]
        member this.``Putting an existing document returns Created and keeps number of documents`` () =
            async {
                // add the document for the first time
                //
                let! first = Documents.Put.query Initialization.defaultDbProperties dbName getTestDocumentId getTestDocumentRev Default.defaultInstance
                match first with
                | Documents.Put.Result.Created x ->
                    
                    // add the document for the second time (with an updated rev)
                    //
                    let newDocument = { Default.defaultInstance with _rev = Some x.rev; myInt = 1337 }
                    let! second = Documents.Put.query Initialization.defaultDbProperties dbName getTestDocumentId getTestDocumentRev newDocument
                    second |> should be (ofCase <@ Documents.Put.Result.Created @>)

                    // retrieve the document and check that is has the new content
                    //
                    let! check = Documents.Get.query<Default.T> Initialization.defaultDbProperties dbName Default.defaultInstance._id []
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
        member this.``Putting a document twice without setting the rev returns Conflict result`` () =
            async {
                let! first = Documents.Put.query Initialization.defaultDbProperties dbName getTestDocumentId getTestDocumentRev Default.defaultInstance
                match first with
                | Documents.Put.Result.Created x ->
                    
                    // add the document for the first time
                    //
                    let newDocument = { Default.defaultInstance with myInt = 1337 }
                    let! second = Documents.Put.query Initialization.defaultDbProperties dbName getTestDocumentId getTestDocumentRev newDocument
                    second |> should be (ofCase <@ Documents.Put.Result.Conflict @>)

                | _ -> failwith "Adding the initial document failed."

            }

        [<Fact>]
        member this.``Putting a document with an id that already exists returns Conflict`` () =
            async {
                let! first = Documents.Put.query Initialization.defaultDbProperties dbName getTestDocumentId getTestDocumentRev Default.defaultInstance 
                match first with
                | Documents.Put.Result.Created x -> 
                    let! second = Documents.Put.query Initialization.defaultDbProperties dbName getTestDocumentId getTestDocumentRev Default.defaultInstance
                    second |> should be (ofCase <@ Documents.Put.Result.Conflict @>)
                | _ -> failwith "Adding the initial document failed."
            }

        [<Fact>]
        member this.``Putting a document with a non-existing database name returns NotFound`` () =
            async {
                let! first = Documents.Put.query Initialization.defaultDbProperties "non-existing-db" getTestDocumentId getTestDocumentRev Default.defaultInstance
                first |> should be (ofCase <@ Documents.Put.Result.NotFound @>)
            }

        [<Fact>]
        member this.``Putting a document with an empty database name returns DbNameMissing`` () =
            async {
                let! first = Documents.Put.query Initialization.defaultDbProperties "" getTestDocumentId getTestDocumentRev Default.defaultInstance
                first |> should be (ofCase <@ Documents.Put.Result.DbNameMissing @>)
            }
            
        [<Fact>]
        member this.``Putting a document with an empty id returns DocumentIdMissing`` () =
            async {
                let! first = Documents.Put.query Initialization.defaultDbProperties dbName (fun _ -> System.Guid.Empty) getTestDocumentRev Default.defaultInstance
                first |> should be (ofCase <@ Documents.Put.Result.DocumentIdMissing @>)
            }
