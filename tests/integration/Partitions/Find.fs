namespace b0wter.CouchDb.Tests.Integration.Partitions

open Newtonsoft.Json.Linq

module Find =
    
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
    let model2 = Default.create (id2, 2, "two",   "owt",   2.5,  DateTime(1990, 10, 10, 20, 0, 0))
    let model3 = Default.create (id3, 3, "three", "eerht", 3.14, DateTime(2000, 10, 10, 20, 0, 0))
    let model4 = Default.create (id4, 4, "one", "eno", 6.28, DateTime(2010, 10, 10, 20, 0, 0))
    let model5 = Default.create (id5, 5, "two", "owt", 42.0, DateTime(2020, 10, 10, 20, 0, 0))
    let model6 = Default.create (id6, 6, "three", "eerht", 1024.0, DateTime(2030, 10, 10, 20, 0, 0))
    
    let documents = [
        model1 :> obj; model2; model3; model4; model5; model6
    ]
    
    type Tests() =
        inherit Utilities.PrefilledSingleDatabaseTests("dummydb", documents, true)

        [<Fact>]
        member this.``Searching for valid data in an existing database with partitions only returns documents from a matching partition`` () =
            async {
                let! result =
                    Partitions.Find.queryWithOutput<Default.T>
                        Initialization.defaultDbProperties
                        this.DbName
                        partition1Name
                        ((Mango.condition "myFirstString" (Mango.Equal <| Mango.Text "one")) |> Mango.createExpression)
                
                match result with
                | Databases.Find.Result.Success r ->
                    r.Docs |> should haveLength 1
                    let doc = r.Docs.Head
                    doc.myFloat |> should equal 2.5
                    doc.myInt |> should equal 1
                    doc.myFirstString |> should equal "one"
                    doc.mySecondString |> should equal "eno"
                | _ ->
                    failwith "The query should return a success response but did not in this case"
            }
