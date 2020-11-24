namespace b0wter.CouchDb.Tests.Integration.Attachments

module Put =
    
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
        inherit Utilities.PrefilledSingleDatabaseTests ("test-db", [ Default.defaultInstance ])

        [<Fact>]
        member this.``Putting a valid attachment for a valid document returns Accepted`` () =
            async {
                let! doc = this.GetSingleDocument<Default.T> ()
                let! result = Attachments.PutBinary.query Initialization.defaultDbProperties this.DbName doc._id doc._rev.Value "foo" defaultAttachment
                
                result |> should be (ofCase <@ Attachments.PutBinary.Result.Created @>)
            }
            
        [<Fact>]
        member this.``Putting a valid attachment for a non-existing document returns NotFound`` () =
            async {
                let! doc = this.GetSingleDocument<Default.T> ()
                let! result = Attachments.PutBinary.query Initialization.defaultDbProperties this.DbName "non-existing document id" doc._rev.Value "foo" defaultAttachment
                
                // ATTENTION - The returned case should be NotFound but is actually Conflict!
                //             This needs to be addressed by the CouchDb maintainers.
                result |> should be (ofCase <@ Attachments.PutBinary.Result.Conflict @>)
            }

        [<Fact>]
        member this.``Putting a valid attachment into a non-existing database returns NotFound`` () =
            async {
                let! doc = this.GetSingleDocument<Default.T> ()
                let! result = Attachments.PutBinary.query Initialization.defaultDbProperties (this.DbName + "non-existing") doc._id doc._rev.Value "foo" defaultAttachment
                result |> should be (ofCase <@ Attachments.PutBinary.Result.NotFound @>)
            }
            
        [<Fact>]
        member this.``Putting a valid attachment with an empty database name returns DbNameMissing`` () =
            async {
                let! result = Attachments.PutBinary.query Initialization.defaultDbProperties System.String.Empty "" "" "foo" defaultAttachment
                result |> should be (ofCase <@ Attachments.PutBinary.Result.DbNameMissing @>)
            }
            
        [<Fact>]
        member this.``Putting an attachment with missing id returns DocumentIdMissing`` () =
            async {
                let! result = Attachments.PutBinary.query Initialization.defaultDbProperties this.DbName System.String.Empty "some revision" "foo" defaultAttachment
                result |> should be (ofCase <@ Attachments.PutBinary.Result.DocumentIdMissing @>)
            }
            
        [<Fact>]
        member this.``Putting an attachment with missing revision returns DocumentRevMissing`` () =
            async {
                let! result = Attachments.PutBinary.query Initialization.defaultDbProperties this.DbName "some id" System.String.Empty "foo" defaultAttachment
                result |> should be (ofCase <@ Attachments.PutBinary.Result.DocumentRevMissing @>)
            }
            
        [<Fact>]
        member this.``Putting an attachment with missing name returns AttachmentNameMissing`` () =
            async {
                let! result = Attachments.PutBinary.query Initialization.defaultDbProperties this.DbName "some id" "some revision" System.String.Empty defaultAttachment
                result |> should be (ofCase <@ Attachments.PutBinary.Result.AttachmentNameMissing @>)
            }
            
        [<Fact>]
        member this.``Putting an attachment twice returns Conflict`` () =
            async {
                let! doc = this.GetSingleDocument<Default.T> ()
                let! _ = Attachments.PutBinary.query Initialization.defaultDbProperties this.DbName doc._id doc._rev.Value "foo" defaultAttachment
                let! result = Attachments.PutBinary.query Initialization.defaultDbProperties this.DbName doc._id doc._rev.Value "foo" defaultAttachment
                result |> should be (ofCase <@ Attachments.PutBinary.Result.Conflict @>)
            }
