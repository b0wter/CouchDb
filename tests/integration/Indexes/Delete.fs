namespace b0wter.CouchDb.Tests.Integration.Indexes

module DeleteIndex =
    
    open FsUnit.Xunit
    open Xunit
    open b0wter.CouchDb.Lib
    open b0wter.CouchDb.Tests.Integration
    open FsUnit.CustomMatchers
    open Newtonsoft.Json.Linq
    open b0wter.FSharp
    open b0wter.FSharp.Operators
    
    type Tests() =
        inherit Utilities.EmptySingleDatabaseTests("db-tests")

        let indexName = "myIndex"
        let dDocName = "myDDoc"

        let createFieldIndex dbName : Async<Result<Indexes.Create.Response, string>> = 
            async {
                let errorAsString (r: RequestResult.TString) = sprintf "Could not initialise a index deletion test because the respons was: %i - %s" (r.StatusCode |?| 0)  r.Content 

                let index = Indexes.Create.createFieldsIndex [ "myField" ]
                let queryParameters = { Indexes.Create.EmptyQueryParameters with Index = index; Name = Some indexName; DDoc = Some dDocName }
                return! Indexes.Create.queryAsResult Initialization.defaultDbProperties dbName queryParameters |> AsyncResult.mapError errorAsString
            }
        
        [<Fact>]
        member this.``Deleting an existing index returns Deleted`` () =
            async {
                match! createFieldIndex this.DbName with
                | Ok response -> 
                    // CouchDb automatically adds the '_design/' prefix when returning the design document id.
                    // The prefix needs to be removed if you want to query CouchDb for the index name.
                    let! result = Indexes.Delete.query Initialization.defaultDbProperties this.DbName (response.Id.Replace("_design/", "")) response.Name

                    result |> should be (ofCase <@ Indexes.Delete.Result.Deleted @>)
                | Error e -> failwith <| sprintf "Could not add an index to perform the 'delete index' tests because: %s" (e.ToString())
            }


        [<Fact>]
        member this.``Deleting an index on a non-existing database returns NotFound`` () =
            async {
                let! result = Indexes.Delete.query Initialization.defaultDbProperties this.DbName dDocName indexName

                result |> should be (ofCase <@ Indexes.Delete.Result.NotFound @>)
            }

        [<Fact>]
        member this.``Deleting using a design document whose name stats with '_design/' returns InvalidDesignDocName`` () =
            async {
                let! result = Indexes.Delete.query Initialization.defaultDbProperties this.DbName "_design/my-design-doc" "index-name"

                result |> should be (ofCase <@ Indexes.Delete.Result.InvalidDesignDocName @>)
            }

        [<Fact>]
        member this.``Deleting a non-existing index on an existing database and design document returns NotFound`` () =
            async {
                let designDoc = DesignDocumentTestModels.Default.defaultDoc
                
                match! DesignDocuments.Put.queryAsResult Initialization.defaultDbProperties this.DbName designDoc with
                | Ok _ ->
                    let! result = Indexes.Delete.query Initialization.defaultDbProperties this.DbName designDoc.Id indexName
                    result |> should be (ofCase <@ Indexes.Delete.Result.NotFound @>)
                | Error e ->
                    failwith <| sprintf "Could not add design document to test deletion of indexes on non-indexed design documents."
            }

        [<Fact>]
        member this.``Deleting an index on a non-existing design document returns NotFound`` () =
            async {
                let! result = Indexes.Delete.query Initialization.defaultDbProperties this.DbName "my-non-existing-design-doc" indexName

                result |> should be (ofCase <@ Indexes.Delete.Result.NotFound @>)
            }

        [<Fact>]
        member this.``Deleting with an empty database name returns MissingDbName`` () =
            async {
                let! result = Indexes.Delete.query Initialization.defaultDbProperties System.String.Empty "design-doc" "index"
                result |> should be (ofCase <@ Indexes.Delete.Result.MissingDbName @>)
            }

        [<Fact>]
        member this.``Deleting with an empty design doc name returns MissingDesignDocName`` () =
            async {
                let! result = Indexes.Delete.query Initialization.defaultDbProperties this.DbName System.String.Empty "index"
                result |> should be (ofCase <@ Indexes.Delete.Result.MissingDesignDocName @>)
            }

        [<Fact>]
        member this.``Deleting with an empty index name returns MissingIndexName`` () =
            async {
                let! result = Indexes.Delete.query Initialization.defaultDbProperties this.DbName "design-doc" System.String.Empty
                result |> should be (ofCase <@ Indexes.Delete.Result.MissingIndexName @>)
            }
