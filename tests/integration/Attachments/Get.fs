namespace b0wter.CouchDb.Tests.Integration.Attachments
open b0wter.CouchDb.Tests.Integration

module Get =
    
    open FsUnit.Xunit
    open Xunit
    open b0wter.CouchDb.Lib
    open FsUnit.CustomMatchers
    open b0wter.CouchDb.Tests.Integration.DocumentTestModels
    
    let defaultAttachment = [ 01uy; 02uy; 03uy; 04uy; 05uy; 06uy; 07uy; 08uy; 09uy; 10uy; 11uy; 12uy; 13uy; 14uy; 15uy; 16uy; 17uy ] |> Array.ofList
    
    type Tests() =
        inherit Utilities.PrefilledSingleDatabaseTests("test-db", [ Default.defaultInstance ])
        
        [<Fact>]
        member this.``Retrieving a newly-added attachment returns DocumentExists result`` () =
            async {
                let! doc = this.GetSingleDocument<Default.T> ()
                
                match! Attachments.PutBinary.queryAsResult Initialization.defaultDbProperties this.DbName doc._id doc._rev.Value "foo" defaultAttachment with
                | Ok attachment ->
                    let! result = Attachments.GetBinary.queryAsResult Initialization.defaultDbProperties this.DbName doc._id "foo"
                    match result with
                    | Ok a ->
                        a |> should equal defaultAttachment
                    | Error e ->
                        failwith (e |> ErrorRequestResult.binaryAsString)
                    //result |> should be (ofCase <@ Attachments.GetBinary.Success @>)
                | Error e ->
                    failwith (e |> ErrorRequestResult.textAsString)
            }
            
        [<Fact>]
        member this.``Retrieving a non-existing attachment returns NotFound`` () =
            async {
                let! doc = this.GetSingleDocument<Default.T> ()
                let! result = Attachments.GetBinary.query Initialization.defaultDbProperties this.DbName doc._id "foo"
                result |> should be (ofCase <@ Attachments.GetBinary.NotFound @>)
            }
            
        [<Fact>]
        member this.``Retrieving an attachment for a non-existing document returns NotFound`` () =
            async {
                let! result = Attachments.GetBinary.query Initialization.defaultDbProperties this.DbName "bogus id" "foo"
                result |> should be (ofCase <@ Attachments.GetBinary.NotFound @>)
            }
    
        [<Fact>]
        member this.``Retrieving an attachment without specifying a document id returns DocumentIdMissing`` () =
            async {
                let! result = Attachments.GetBinary.query Initialization.defaultDbProperties this.DbName System.String.Empty "foo"
                result |> should be (ofCase<@ Attachments.GetBinary.DocumentIdMissing @>)
            }
                
        [<Fact>]
        member this.``Retrieving a document without specifying a db name returns DbNameMissing`` () =
            async {
                let! result = Attachments.GetBinary.query Initialization.defaultDbProperties this.DbName "foo" System.String.Empty
                result |> should be (ofCase<@ Attachments.GetBinary.AttachmentNameMissing @>)
            }
