namespace b0wter.CouchDb.Tests.Integration.DesignDocuments

module Put =
    
    open FsUnit.Xunit
    open Xunit
    open b0wter.CouchDb.Lib
    open FsUnit.CustomMatchers
    open b0wter.CouchDb.Tests.Integration
    open b0wter.CouchDb.Tests.Integration.DesignDocumentTestModels
    
    type Tests() =
        inherit Utilities.EmptySingleDatabaseTests("test-db")
        let dbName = "test-db"

        [<Fact>]
        member this.``Putting a non-existing design document returns Created`` () =
            async {
                let! result = DesignDocuments.Put.query Initialization.defaultDbProperties dbName Default.defaultDoc
                result |> should be (ofCase <@ DesignDocuments.Put.Result.Created @>)
            }

        [<Fact>]
        member this.``Putting an existing design document returns Created and keeps number of documents`` () =
            async {
                // add the document for the first time
                //
                let! first = DesignDocuments.Put.query Initialization.defaultDbProperties dbName Default.defaultDoc
                match first with
                | DesignDocuments.Put.Result.Created x ->
                    // add the document for the second time (with an updated rev)
                    //
                    let newViewName = "ThisIsANewView"
                    let newViews = { Default.defaultDoc.views.Head with name = newViewName } :: (Default.defaultDoc.views |> List.tail)
                    let newDocument = { Default.defaultDoc with rev = Some x.rev; views = newViews }
                    let! second = DesignDocuments.Put.query Initialization.defaultDbProperties dbName newDocument
                    second |> should be (ofCase <@ DesignDocuments.Put.Result.Created @>)

                    // retrieve the document and check that is has the new content
                    //
                    let! check = DesignDocuments.Get.query Initialization.defaultDbProperties dbName Default.defaultDoc.id []
                    match check with
                    | DesignDocuments.Get.Result.DocumentExists x -> x.content.views.Head.name |> should equal newViewName
                    | _ -> 
                        let internalError = check |> DesignDocuments.Get.asResult |> function Ok _ -> "Response indicates success altough the test failed. Look into this!" | Error e -> e |> ErrorRequestResult.asString
                        failwith (sprintf "The retrieval of the document (using DesignDocuments.Info) failed: %s%s" System.Environment.NewLine internalError)
                    
                    // check that there is only a single document in the database
                    //
                    let! count = Server.DbsInfo.query Initialization.defaultDbProperties [ dbName ]
                    match count with
                    | Server.DbsInfo.Result.Success s -> s.[0].info.Value.doc_count |> should equal 1
                    | _ -> failwith "Putting the updated document resulted in the creation of a new document!"

                | _ -> failwith "Adding the initial document failed."

            }

        [<Fact>]
        member this.``Putting a design document twice without setting the rev returns Conflict result`` () =
            async {
                let! first = DesignDocuments.Put.query Initialization.defaultDbProperties dbName Default.defaultDoc
                match first with
                | DesignDocuments.Put.Result.Created x ->
                    
                    // add the document for the first time
                    //
                    let newViewName = "ThisIsANewView"
                    let newViews = { Default.defaultDoc.views.Head with name = newViewName } :: (Default.defaultDoc.views |> List.tail)
                    let newDocument = { Default.defaultDoc with views = newViews }
                    let! second = DesignDocuments.Put.query Initialization.defaultDbProperties dbName newDocument
                    second |> should be (ofCase <@ DesignDocuments.Put.Result.Conflict @>)

                | _ -> 
                    let asResult = match DesignDocuments.Put.asResult first with Ok _ -> "Response indicates success?" | Error e -> e |> ErrorRequestResult.asString
                    failwith (sprintf "Adding the initial document failed: %s%s%A" asResult System.Environment.NewLine Default.defaultDoc)
            }

        [<Fact>]
        member this.``Putting a design document with an id that already exists returns Conflict`` () =
            async {
                let! first = DesignDocuments.Put.query Initialization.defaultDbProperties dbName Default.defaultDoc
                match first with
                | DesignDocuments.Put.Result.Created x -> 
                    let! second = DesignDocuments.Put.query Initialization.defaultDbProperties dbName Default.defaultDoc
                    second |> should be (ofCase <@ DesignDocuments.Put.Result.Conflict @>)
                | _ -> failwith "Adding the initial document failed."
            }

        [<Fact>]
        member this.``Putting a design document with a non-existing database name returns NotFound`` () =
            async {
                let! first = DesignDocuments.Put.query Initialization.defaultDbProperties "non-existing-db" Default.defaultDoc
                first |> should be (ofCase <@ DesignDocuments.Put.Result.NotFound @>)
            }

        [<Fact>]
        member this.``Putting a design document with an empty database name returns DbNameMissing`` () =
            async {
                let! first = DesignDocuments.Put.query Initialization.defaultDbProperties "" Default.defaultDoc
                first |> should be (ofCase <@ DesignDocuments.Put.Result.DbNameMissing @>)
            }
            
        [<Fact>]
        member this.``Putting a design document with an empty id returns DesignDocumentIdMissing`` () =
            async {
                let emptyGuidDocument = { Default.defaultDoc with id = System.String.Empty }
                let! first = DesignDocuments.Put.query Initialization.defaultDbProperties dbName emptyGuidDocument
                first |> should be (ofCase <@ DesignDocuments.Put.Result.DocumentIdMissing @>)
            }
