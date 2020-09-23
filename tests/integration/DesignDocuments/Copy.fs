namespace b0wter.CouchDb.Tests.Integration.DesignDocuments

module Copy =
    
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
        
        [<Fact>]
        member this.``Copying an existing document without supplying a doc rev returns Created result`` () =
            async {
                let targetId = "466dbe23-98e8-4d39-be9c-d44c56e2d15a"
                
                let! addDocument = Documents.Put.query<Default.T> Initialization.defaultDbProperties this.DbName getTestDocumentId getTestDocumentRev Default.defaultInstance
                do addDocument |> should be (ofCase <@ Documents.Put.Result.Created @>)
                
                let! countDocuments = Server.DbsInfo.query Initialization.defaultDbProperties [this.DbName]
                match countDocuments with
                | Server.DbsInfo.Result.Success infos ->
                    do infos.[0].info.Value.doc_count |> should equal 1
                    
                    let! copyDocument = Documents.Copy.query Initialization.defaultDbProperties this.DbName Default.defaultInstance._id None targetId None
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
                let targetId = "466dbe23-98e8-4d39-be9c-d44c56e2d15a"
                let! copyDocument = Documents.Copy.query Initialization.defaultDbProperties this.DbName Default.defaultInstance._id None targetId None
                copyDocument |> should be (ofCase <@ Documents.Copy.Result.NotFound @>)
            }
            
        [<Fact>]
        member this.``Copying an existing document to a doc id that already exists returns a Conflict result`` () =
            async {
                let! addDocument = Documents.Put.query Initialization.defaultDbProperties this.DbName getTestDocumentId getTestDocumentRev Default.defaultInstance
                match addDocument with
                | Documents.Put.Result.Created _ ->
                    
                    let! copyDocument = Documents.Copy.query Initialization.defaultDbProperties this.DbName Default.defaultInstance._id None Default.defaultInstance._id None
                    do copyDocument |> should be (ofCase <@ Documents.Copy.Result.Conflict @>)
                    
                | _ -> failwith "The test preparation failed, could not add the initial document."
            }
            
        [<Fact>]
        member this.``Copying from a non-existing database returns a NotFound result`` () =
            async {
                let targetId = "466dbe23-98e8-4d39-be9c-d44c56e2d15a"
                let! copyDocument = Documents.Copy.query Initialization.defaultDbProperties "non-existing-db" Default.defaultInstance._id None targetId None
                copyDocument |> should be (ofCase <@ Documents.Copy.Result.NotFound @>)
            }
