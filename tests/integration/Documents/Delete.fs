namespace b0wter.CouchDb.Tests.Integration.Documents

module Delete =
    
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
        member this.``Deleting an existing document returns Ok result`` () =
            async {
                let! addResult = Documents.Put.query Initialization.defaultDbProperties dbName getTestDocumentId getTestDocumentRev Default.defaultInstance
                match addResult with
                | Documents.Put.Result.Created x ->
                    let! removeResult = Documents.Delete.query Initialization.defaultDbProperties dbName Default.defaultInstance._id x.rev
                    removeResult |> should be (ofCase <@ Documents.Delete.Result.Ok @>)
                    
                | _ -> failwith "Adding the initial document failed."
            }

        [<Fact>]
        member this.``Deleting a non-existing document returns NotFound result`` () =
            async {
                let bogusRev = "ThisIsABogusRev"
                let! result = Documents.Delete.query Initialization.defaultDbProperties dbName Default.defaultInstance._id bogusRev
                result |> should be (ofCase<@ Documents.Delete.Result.NotFound @>)
            }
            
        [<Fact>]
        member this.``Deleting with an non-existing database returns NotFound result`` () =
            async {
                let bogusRev = "ThisIsABogusRev"
                let! result = Documents.Delete.query Initialization.defaultDbProperties "non-existing-db" Default.defaultInstance._id bogusRev
                result |> should be (ofCase<@ Documents.Delete.Result.NotFound @>)
            }
            
        [<Fact>]
        member this.``Deleting a document with an empty id returns DocumentIdEmpty result`` () =
            async {
                let bogusRev = "ThisIsABogusRev"
                let! result = Documents.Delete.query Initialization.defaultDbProperties dbName System.String.Empty bogusRev
                result |> should be (ofCase<@ Documents.Delete.Result.DocumentIdEmpty @>)
            }
            
        [<Fact>]
        member this.``Deleting a document with an empty rev returns DocumentRevEmpty result`` () =
            async {
                let! result = Documents.Delete.query Initialization.defaultDbProperties dbName Default.defaultInstance._id ""
                result |> should be (ofCase<@ Documents.Delete.Result.DocumentRevEmpty @>)
            }
            
        [<Fact>]
        member this.``Deleting a document using an old rev returns Conflict result`` () =
            async {
                
                // Add the document.
                //
                let! addResult = Documents.Put.query Initialization.defaultDbProperties dbName getTestDocumentId getTestDocumentRev Default.defaultInstance
                match addResult with
                | Documents.Put.Result.Created x ->
                    
                    // Set the revision and update the contents of the document.
                    // Then update it again. This will result in a new revision that we don't care about.
                    //
                    let updatedDocument = { Default.defaultInstance with _rev = Some x.rev; myInt = 1337 }
                    let! update = Documents.Put.query Initialization.defaultDbProperties dbName getTestDocumentId getTestDocumentRev updatedDocument
                    match update with
                    | Documents.Put.Result.Created y ->
                        
                        // Delete using the old revision (from when the document was initially added, not updated).
                        //
                        let! delete = Documents.Delete.query Initialization.defaultDbProperties dbName Default.defaultInstance._id x.rev
                        delete |> should be (ofCase <@ Documents.Delete.Result.Conflict @>)
                        
                    | _ -> failwith "Updating the initial document failed!"
                    
                | _ -> failwith "Adding the initial document failed."
            }
