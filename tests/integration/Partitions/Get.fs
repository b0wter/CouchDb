namespace b0wter.CouchDb.Tests.Integration.Partitions

module Get =
    
    open FsUnit.Xunit
    open Xunit
    open b0wter.CouchDb.Lib
    open b0wter.CouchDb.Tests.Integration
    open FsUnit.CustomMatchers
    
    let documents = [
        {| _id = "foo:document1"; name = "Document 1" |} :> obj
        {| _id = "foo:document2"; name = "Document 2" |} :> obj
        {| _id = "foo:document3"; name = "Document 3" |} :> obj
        {| _id = "bar:document1"; name = "Document 1" |} :> obj
        {| _id = "bar:document2"; name = "Document 2" |} :> obj
        {| _id = "bar:document3"; name = "Document 3" |} :> obj
    ]
    
    type Tests() =
        inherit Utilities.PrefilledSingleDatabaseTests("dummydb", documents, true)
        
        [<Fact>]
        member this.``Querying an existing partitioned database for partition details returns Success-result`` () =
            async {
                let! result = Partitions.Get.query Initialization.defaultDbProperties this.DbName "foo"
                
                result |> should be (ofCase <@ Partitions.Get.Result.Success @>)
                
                match result with
                | Partitions.Get.Result.Success response ->
                    response.Partition |> should equal "foo"
                    response.DbName |> should equal this.DbName
                    response.DocumentCount |> should equal 3
                    response.DeletedDocumentCount |> should equal 0
                | _ ->
                    failwith "Despite a prior assertion the `result` is not in the expected case"
                   
            }
        
        

