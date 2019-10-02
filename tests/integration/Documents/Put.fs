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

    type Tests() =
        inherit Utilities.PrefilledDatabaseTests("test-db")
        let dbName = "test-db"

        [<Fact>]
        member this.``Putting a non-existing document returns Created`` () =
            async {
                let! result = Documents.Put.query Initialization.defaultDbProperties dbName getTestDocumentId testDocumentWithId
                result |> should be (ofCase <@ Documents.Put.Result.Created @>)
            }

        [<Fact>]
        member this.``Putting an existing document returns Created`` () =
            async {
                let! first = Documents.Put.query Initialization.defaultDbProperties dbName getTestDocumentId testDocumentWithId
                match first with
                | Documents.Put.Result.Created x -> 
                    let newDocument = { testDocumentWithId with _rev = Some x.rev; myInt = 1337 }
                    let! second = Documents.Put.query Initialization.defaultDbProperties dbName getTestDocumentId newDocument
                    second |> should be (ofCase <@ Documents.Put.Result.Created @>)

                    let! check = Documents.Info.query<TestDocument> Initialization.defaultDbProperties dbName testDocumentWithId._id []
                    match check with
                    | Documents.Info.Result.DocumentExists x -> x.content.myInt |> should equal 1337
                    | _ -> failwith "The retrieval of the document (using Documents.Info) failed!"

                | _ -> failwith "Adding the initial document failed."

            }

        [<Fact>]
        member this.``Putting a document with an id that already exists returns Conflict`` () =
            async {
                let! first = Documents.Put.query Initialization.defaultDbProperties dbName getTestDocumentId testDocumentWithId
                match first with
                | Documents.Put.Result.Created x -> 
                    let! second = Documents.Put.query Initialization.defaultDbProperties dbName getTestDocumentId testDocumentWithId
                    second |> should be (ofCase <@ Documents.Put.Result.Conflict @>)
                | _ -> failwith "Adding the initial document failed."
            }

        [<Fact>]
        member this.``Putting a document with a non-existing database name returns NotFound`` () =
            async {
                let! first = Documents.Put.query Initialization.defaultDbProperties "non-existing-db" getTestDocumentId testDocumentWithId
                first |> should be (ofCase <@ Documents.Put.Result.NotFound @>)
            }

        [<Fact>]
        member this.``Putting a document with an empty database name returns DbNameMissing`` () =
            async {
                let! first = Documents.Put.query Initialization.defaultDbProperties "" getTestDocumentId testDocumentWithId
                first |> should be (ofCase <@ Documents.Put.Result.DbNameMissing @>)
            }
            
        [<Fact>]
        member this.``Putting a document with an empty id returns DocumentIdMissing`` () =
            async {
                let! first = Documents.Put.query Initialization.defaultDbProperties dbName (fun _ -> System.Guid.Empty) testDocumentWithId
                first |> should be (ofCase <@ Documents.Put.Result.DocumentIdMissing @>)
            }
