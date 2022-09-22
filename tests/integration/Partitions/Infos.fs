namespace b0wter.CouchDb.Tests.Integration.Partitions

module Infos =
    
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
        member this.``Querying for details of an existing partition in an existing database returns Success-result`` () =
            async {
                let! result = Partitions.Infos.query Initialization.defaultDbProperties this.DbName "foo"
                
                result |> should be (ofCase <@ Partitions.Infos.Result.Success @>)
                
                match result with
                | Partitions.Infos.Result.Success response ->
                    response.Partition |> should equal "foo"
                    response.DbName |> should equal this.DbName
                    response.DocumentCount |> should equal 3
                    response.DeletedDocumentCount |> should equal 0
                | _ ->
                    failwith "Despite a prior assertion the `result` is not in the expected case"
                   
            }
        
        [<Fact>]
        member this.``Querying for details of a non-existing partition in an existing database returns empty Success-result`` () =
            async {
                let! result = Partitions.Infos.query Initialization.defaultDbProperties this.DbName "non-existing-partition"
                
                result |> should be (ofCase <@ Partitions.Infos.Result.Success  @>)
                
                match result with
                | Partitions.Infos.Result.Success response ->
                    response.Partition |> should equal "non-existing-partition"
                    response.DbName |> should equal this.DbName
                    response.DocumentCount |> should equal 0
                    response.DeletedDocumentCount |> should equal 0
                | _ ->
                    failwith "Despite a prior assertion the `result` is not in the expected case"
            }
        
        [<Fact>]
        member this.``Querying for details of a non-existing partition in a non-existing database returns NotFound-result`` () =
            async {
                let! result = Partitions.Infos.query Initialization.defaultDbProperties "non-existing-database" "non-existing-partition"
                
                result |> should be (ofCase <@ Partitions.Infos.Result.NotFound  @>)
            }
        
        [<Theory>]
        [<InlineData(null)>]
        [<InlineData("")>]
        [<InlineData(" ")>]
        [<InlineData("  ")>]
        member this.``Querying for partition details with missing partition name return PartitionNameMissing-result`` partitionName =
            async {
                let! result = Partitions.Infos.query Initialization.defaultDbProperties this.DbName partitionName
                
                result |> should be (ofCase <@ Partitions.Infos.Result.PartitionNameMissing  @>)
            }
        
        [<Theory>]
        [<InlineData(null)>]
        [<InlineData("")>]
        [<InlineData(" ")>]
        [<InlineData("  ")>]
        member this.``Querying for partition details with missing/empty database name return DbNameMissing-result`` dbName =
            async {
                let! result = Partitions.Infos.query Initialization.defaultDbProperties dbName "non-existing-partition"
                
                result |> should be (ofCase <@ Partitions.Infos.Result.DbNameMissing  @>)
            }
