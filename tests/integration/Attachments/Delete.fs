namespace b0wter.CouchDb.Tests.Integration.Attachments

open System

module Delete =
    
    open FsUnit.Xunit
    open Xunit
    open b0wter.CouchDb.Lib
    open FsUnit.CustomMatchers
    open b0wter.CouchDb.Tests.Integration
    open b0wter.CouchDb.Tests.Integration.DocumentTestModels
    
    let getTestDocumentId (doc: Default.T) = doc._id
    let getTestDocumentRev (doc: Default.T) = doc._rev

    let defaultAttachment = [ 01uy; 02uy; 03uy; 04uy; 05uy; 06uy; 07uy; 08uy; 09uy; 10uy; 11uy; 12uy ] |> Array.ofList
    
    type Tests() =
        inherit Utilities.PrefilledSingleDatabaseTests("test-db", [ Default.defaultInstance ])

        [<Fact>]
        member this.``Deleting an existing attachment returns Ok result`` () =
            async {
                let! doc = this.GetSingleDocument<Default.T> ()
                match! Attachments.PutBinary.queryAsResult Initialization.defaultDbProperties this.DbName doc._id doc._rev "foo" defaultAttachment with
                | Ok attachment ->
                    let! result = Attachments.Delete.query Initialization.defaultDbProperties this.DbName attachment.Id attachment.Rev "foo"
                    result |> should be (ofCase <@ Attachments.Delete.Result.Ok @>)
                | Error e -> failwith ("Adding the initial attachment failed: " + (e |> ErrorRequestResult.textAsString))
            }

        [<Fact>]
        member this.``Deleting a non-existing attachment returns Ok/NotFound result`` () =
            async {
                // The response should be NotFound according to the documentation but is Ok.
                // See this issue:
                // https://github.com/apache/couchdb/issues/2146
                let! doc = this.GetSingleDocument<Default.T> ()
                let! result = Attachments.Delete.query Initialization.defaultDbProperties this.DbName doc._id doc._rev.Value "non-existing attachment"
                result |> should be (ofCase<@ Attachments.Delete.Result.NotFound @>)
            }
            
        [<Fact>]
        member this.``Deleting with a non-existing database returns NotFound result`` () =
            async {
                let! result = Attachments.Delete.query Initialization.defaultDbProperties "non-existing-db" Default.defaultInstance._id "some rev" "some attachment name"
                result |> should be (ofCase<@ Attachments.Delete.Result.NotFound @>)
            }
            
        [<Fact>]
        member this.``Deleting an attachment with an empty database name returns DbNameMissing result`` () =
            async {
                let! result = Attachments.Delete.query Initialization.defaultDbProperties String.Empty "some id" "some rev" "some attachment name"
                result |> should be (ofCase<@ Attachments.Delete.Result.DbNameMissing @>)
            }
            
        [<Fact>]
        member this.``Deleting an attachment with an empty document id returns DocumentIdEmpty result`` () =
            async {
                let! result = Attachments.Delete.query Initialization.defaultDbProperties this.DbName String.Empty "some rev" "some attachment name"
                result |> should be (ofCase<@ Attachments.Delete.Result.DocumentIdEmpty @>)
            }
            
        [<Fact>]
        member this.``Deleting an attachment with an empty document rev returns DocumentRevEmpty result`` () =
            async {
                let! result = Attachments.Delete.query Initialization.defaultDbProperties this.DbName "some document id" String.Empty "some attachment name"
                result |> should be (ofCase<@ Attachments.Delete.Result.DocumentRevEmpty @>)
            }
            
        [<Fact>]
        member this.``Deleting an attachment with an empty attachment name returns BadRequest result`` () =
            async {
                let! result = Attachments.Delete.query Initialization.defaultDbProperties this.DbName "some document id" "some rev" "some attachment name"
                result |> should be (ofCase<@ Attachments.Delete.Result.BadRequest @>)
            }
            
        member this.``Deleting an attachment with an old rev returns Conflict result`` () =
            async {
                let! doc = this.GetSingleDocument<Default.T> ()
                match! Attachments.PutBinary.queryAsResult Initialization.defaultDbProperties this.DbName doc._id doc._rev "foo" defaultAttachment with
                | Ok _ ->
                    let! result = Attachments.Delete.query Initialization.defaultDbProperties this.DbName doc._id doc._rev.Value "foo"
                    result |> should be (ofCase <@ Attachments.Delete.Result.Conflict @>)
                | Error e -> failwith ("Adding the initial attachment failed: " + (e |> ErrorRequestResult.textAsString))
            }
