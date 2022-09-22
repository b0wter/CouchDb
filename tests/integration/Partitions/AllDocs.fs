namespace b0wter.CouchDb.Tests.Integration.Partitions

open Newtonsoft.Json.Linq

module AllDocs =
    
    open FsUnit.Xunit
    open Xunit
    open b0wter.CouchDb.Lib
    open b0wter.CouchDb.Tests.Integration
    open FsUnit.CustomMatchers
    open b0wter.CouchDb.Tests.Integration.DocumentTestModels
    open System
    
    let partition1Name = "foo"
    let partition2Name = "bar"
    
    let id1 = sprintf "%s:document1" partition1Name
    let id2 = sprintf "%s:document2" partition1Name
    let id3 = sprintf "%s:document3" partition1Name
    let id4 = sprintf "%s:document1" partition2Name
    let id5 = sprintf "%s:document2" partition2Name
    let id6 = sprintf "%s:document3" partition2Name

    let model1 = Default.create (id1, 1, "one",   "eno",   2.5,  DateTime(1980, 1, 1, 12, 0, 0))
    let model2 = Default.create (id2, 2, "one",   "owt",   2.5,  DateTime(1990, 10, 10, 20, 0, 0))
    let model3 = Default.create (id3, 3, "three", "eerht", 3.14, DateTime(2000, 10, 10, 20, 0, 0))
    let model4 = Default.create (id4, 3, "four", "rouf", 6.28, DateTime(2010, 10, 10, 20, 0, 0))
    let model5 = Default.create (id5, 5, "five", "evif", 42.0, DateTime(2020, 10, 10, 20, 0, 0))
    let model6 = Default.create (id6, 6, "six", "xis", 1024.0, DateTime(2030, 10, 10, 20, 0, 0))
    
    let documents = [
        model1 :> obj; model2; model3; model4; model5; model6
    ]
    
    type Tests() =
        inherit Utilities.PrefilledSingleDatabaseTests("dummydb", documents, true)

        [<Fact>]
        member this.``Querying all documents in partition returns all matching documents`` () =
            async {
                let! result = Partitions.AllDocs.query<string, JObject>  Initialization.defaultDbProperties this.DbName partition1Name
                
                match result with
                | Partitions.AllDocs.Result.Success r ->
                    r.TotalRows |> should equal 3
                | _ ->
                    failwith "The query should return a success response but did not in this case"
            }

        [<Fact>]
        member this.``Querying all documents with the 'include documents' parameter of a partition returns all matching documents`` () =
            async {
                let queryParameters = { Partitions.AllDocs.EmptyQueryParameters with IncludeDocs = Some true }
                let! result = Partitions.AllDocs.queryWith<string, Default.T> Initialization.defaultDbProperties this.DbName partition1Name queryParameters
                
                match result with
                | Partitions.AllDocs.Result.Success r ->
                    r.TotalRows |> should equal 3
                    let doc = r.Rows.Head.Doc.Value
                    doc.myFloat |> should equal 2.5
                    doc.myInt |> should equal 1
                    doc.myFirstString |> should equal "one"
                    doc.mySecondString |> should equal "eno"
                | _ ->
                    failwith "The query should return a success response but did not in this case"
            }
