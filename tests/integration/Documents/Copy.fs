namespace b0wter.CouchDb.Tests.Integration.Documents

module Copy =
    
    open FsUnit.Xunit
    open Xunit
    open b0wter.CouchDb.Lib
    open b0wter.FSharp.Operators
    open b0wter.CouchDb.Tests.Integration.CustomMatchers
    open b0wter.CouchDb.Tests.Integration
    open CustomMatchers
    
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
        inherit Utilities.PrefilledSingleDatabaseTests("test-db")
        
        [<Fact>]
        member this.``Copying an existing document without supplying a doc rev returns Created result`` () =
            async {
                let targetId = System.Guid.Parse("466dbe23-98e8-4d39-be9c-d44c56e2d15a")
                
                let! addDocument = Documents.Put.query Initialization.defaultDbProperties this.DbName getTestDocumentId getTestDocumentRev testDocumentWithId
                do addDocument |> should be (ofCase <@ Documents.Put.Created @>)
                
                let! countDocuments = Server.DbsInfo.query Initialization.defaultDbProperties [this.DbName]
                match countDocuments with
                | Server.DbsInfo.Result.Success infos ->
                    do infos.[0].info.Value.doc_count |> should equal 1
                    
                    let! copyDocument = Documents.Copy.query Initialization.defaultDbProperties this.DbName testDocumentWithId._id targetId None None
                    match copyDocument with
                    | Documents.Copy.Result.Created c ->
                        do c.id |> should equal targetId
                        do c.ok |> should be True
                        do c.rev |> should not' (be EmptyString)
                    | _ -> failwith "Copy query not successful."
                        
                | _ -> failwith "The document to copy was not added successfully. The document count is not 1."
            }
            
        [<Fact>]
        member this.``Copying a non-existing document returns a NotFound result`` () =
            async {
                let targetId = System.Guid.Parse("466dbe23-98e8-4d39-be9c-d44c56e2d15a")
                let! copyDocument = Documents.Copy.query Initialization.defaultDbProperties this.DbName testDocumentWithId._id targetId None None
                copyDocument |> should be (ofCase <@ Documents.Copy.Result.NotFound @>)
            }
            
        [<Fact>]
        member this.``Copying an existing document to a doc id that already exists returns a Conflict result`` () =
            async {
                let! addDocument = Documents.Put.query Initialization.defaultDbProperties this.DbName getTestDocumentId getTestDocumentRev testDocumentWithId
                match addDocument with
                | Documents.Put.Result.Created _ ->
                    
                    let! copyDocument = Documents.Copy.query Initialization.defaultDbProperties this.DbName testDocumentWithId._id testDocumentWithId._id None None
                    do copyDocument |> should be (ofCase <@ Documents.Copy.Result.Conflict @>)
                    
                | _ -> failwith "The test preparation failed, could not add the initial document."
            }
            
        [<Fact>]
        member this.``Copying from a non-existing database returns a NotFound result`` () =
            async {
                let targetId = System.Guid.Parse("466dbe23-98e8-4d39-be9c-d44c56e2d15a")
                let! copyDocument = Documents.Copy.query Initialization.defaultDbProperties "non-existing-db" testDocumentWithId._id targetId None None
                copyDocument |> should be (ofCase <@ Documents.Copy.Result.NotFound @>)
            }
