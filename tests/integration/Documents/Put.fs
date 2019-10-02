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
        member this.``Putting a non-existing document will returns Created`` () =
            async {
                let! result = Documents.Put.query Initialization.defaultDbProperties dbName getTestDocumentId testDocumentWithId
                result |> should be (ofCase <@ Documents.Put.Result.Created @>)
            }