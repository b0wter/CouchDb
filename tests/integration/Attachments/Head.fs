namespace b0wter.CouchDb.Tests.Integration.Attachments

open b0wter.CouchDb.Tests.Integration.DesignDocumentTestModels

module Head =
    
    open FsUnit.Xunit
    open Xunit
    open System
    open b0wter.CouchDb.Lib
    open FsUnit.CustomMatchers
    open b0wter.CouchDb.Tests.Integration
    open b0wter.CouchDb.Tests.Integration.DocumentTestModels
    
    let defaultAttachment = [ 01uy; 02uy; 03uy; 04uy; 05uy; 06uy; 07uy; 08uy; 09uy; 10uy; 11uy; 12uy ] |> Array.ofList
    
    type Tests() =
        inherit Utilities.PrefilledSingleDatabaseTests("test-db", [ Default.defaultInstance ])
        
        [<Fact>]
        member this.``Retrieving an attachment with a missing database name returns DbNameMissing`` () =
            async {
                let! result = Attachments.Head.query Initialization.defaultDbProperties String.Empty "some id" "some attachment name" None
                result |> should be (ofCase <@ HttpVerbs.Head.DbNameMissing @>)
            }
            
        [<Fact>]
        member this.``Retrieving an attachment with a missing id returns DocumentIdMissing`` () =
            async {
                let! result = Attachments.Head.query Initialization.defaultDbProperties this.DbName String.Empty "some attachment name" None
                result |> should be (ofCase <@ HttpVerbs.Head.DocumentIdMissing @>)
            }
            
        [<Fact>]
        member this.``Retrieving an attachment with a missing name returns ParameterIsMissing`` () =
            async {
                let! result = Attachments.Head.query Initialization.defaultDbProperties this.DbName "some id" String.Empty None
                result |> should be (ofCase <@ HttpVerbs.Head.ParameterIsMissing @>)
            }
            
        [<Fact>]
        member this.``Retrieving a newly-added document returns `` () =
            async {
                let! doc = this.GetSingleDocument<Default.T> ()
                match! Attachments.PutBinary.queryAsResult Initialization.defaultDbProperties this.DbName doc._id doc._rev "foo" defaultAttachment with
                | Ok attachment ->
                    let! result = Attachments.Head.query Initialization.defaultDbProperties this.DbName doc._id "foo" (Some attachment.Rev)
                    result |> should be (ofCase <@ HttpVerbs.Head.DocumentExists @>)
                | Error e ->
                    failwith (e |> ErrorRequestResult.textAsString)
            }
            
        [<Fact>]
        member this.``Retrieving a non-existing attachment returns NotFound`` () =
            async {
                let id = "3f4ae7a0-f4f3-489b-a3b8-eba22450fae4"
                let! result = Attachments.Head.query Initialization.defaultDbProperties this.DbName id "foo" None
                result |> should be (ofCase<@ Documents.Head.Result.NotFound @>)
            }
