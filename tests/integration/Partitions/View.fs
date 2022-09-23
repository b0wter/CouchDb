namespace b0wter.CouchDb.Tests.Integration.Partitions.View

module View =
    
    open FsUnit.Xunit
    open System
    open Xunit
    open b0wter.CouchDb.Lib
    open b0wter.CouchDb.Tests.Integration
    open b0wter.FSharp.Collections
    open FsUnit.CustomMatchers
    open b0wter.CouchDb.Tests.Integration.DocumentTestModels

    let partition1Name = "foo"
    let partition2Name = "bar"
    
    let id1 = sprintf "%s:doc1" partition1Name
    let id2 = sprintf "%s:doc2" partition1Name
    let id3 = sprintf "%s:doc3" partition1Name
    let id4 = sprintf "%s:doc4" partition2Name
    let id5 = sprintf "%s:doc5" partition2Name
    let id6 = sprintf "%s:doc6" partition2Name

    let model1 = Default.create (id1, 1, "one",   "eno",   2.5, DateTime(1980, 1, 1, 12, 0, 0))
    let model2 = Default.create (id2, 2, "two",   "owt",   2.5, DateTime(1990, 10, 10, 20, 0, 0))
    let model3 = Default.create (id3, 3, "three", "eerht", 2.5, DateTime(1990, 10, 10, 20, 0, 0))
    let model4 = Default.create (id4, 1, "one",   "eno",   2.5, DateTime(2000, 10, 10, 20, 0, 0))
    let model5 = Default.create (id5, 2, "two",   "owt",   2.5, DateTime(2000, 10, 10, 20, 0, 0))
    let model6 = Default.create (id6, 3, "three", "eerht", 2.5, DateTime(2000, 10, 10, 20, 0, 0))

    let designDocId = sprintf "%s:b1fbe634-a0b6-40c2-b898-c5c5e4e54f01" partition1Name
    let view1Name = "all"
    let view2Name = "myIntEq3"
    let defaultView1 = DesignDocuments.DesignDocument.createView view1Name DesignDocuments.DesignDocument.Map "function(doc) { emit(doc._id, doc); }"
    let defaultView2 = DesignDocuments.DesignDocument.createView view2Name DesignDocuments.DesignDocument.Map "function(doc) { if (doc.myInt && doc.myInt === 3) { emit(doc._id, doc); } }"
    let designDoc = DesignDocuments.DesignDocument.createDocWithId designDocId [ defaultView1; defaultView2 ]

    type Tests() =
        inherit Utilities.PrefilledSingleDatabaseTests("database-view-tests", [ model1; model2; model3; model4; model5; model6 ], true)

        [<Fact>]
        member this.``Querying 'all' view returns all documents.`` () =
            async {
                match! DesignDocuments.Put.queryAsResult Initialization.defaultDbProperties this.DbName designDoc with
                | Ok _ ->
                    let! result = Partitions.View.queryAsResult<string, Default.T> Initialization.defaultDbProperties this.DbName partition1Name designDocId view1Name
                    match result with
                    | Ok r ->
                        let responses = r |> Partitions.View.responseAsSingleResponses |> List.exactlyOne
                        do responses.TotalRows |> should equal 3
                        do responses.Rows.Length |> should equal 3
                    | Error e ->
                        failwith (sprintf "Could not retrieve the design document view because:%s%s" Environment.NewLine (ErrorRequestResult.textAsString e))
                | Error e ->
                    failwith (sprintf "Could not add the design document because:%s%s" Environment.NewLine (ErrorRequestResult.textAsString e))
            }

        [<Fact>]
        member this.``Querying 'all' two times with 268435456 limit returns all elements twice.`` () =
            async {
                match! DesignDocuments.Put.queryAsResult Initialization.defaultDbProperties this.DbName designDoc with
                | Ok _ ->
                    let singleQueryParameter = { Partitions.View.EmptyQueryParameters with Limit = Some 268435456}
                    let queryParameters = (Partitions.View.QueryParameters.Multi {Partitions.View.MultiQueryParameters.Queries = [ singleQueryParameter; singleQueryParameter ] })
                    let! result = Partitions.View.queryWithAsResult<string, Default.T> Initialization.defaultDbProperties this.DbName partition1Name designDocId view1Name queryParameters
                    match result with
                    | Ok r ->
                        let responses = r |> Partitions.View.responseAsSingleResponses 
                        do responses.Length |> should equal 2
                        do responses |> List.iter (fun response -> 
                            do response.TotalRows |> should equal 3
                            do response.Rows.Length |> should equal 3)
                    | Error e ->
                        failwith (sprintf "Could not retrieve the design document view because:%s%s" Environment.NewLine (ErrorRequestResult.textAsString e))
                | Error e ->
                    failwith (sprintf "Could not add the design document because:%s%s" Environment.NewLine (ErrorRequestResult.textAsString e))
            }

        [<Fact>]
        member this.``Querying with a limit of 2 returns 2 elements.`` () =
            async {
                match! DesignDocuments.Put.queryAsResult Initialization.defaultDbProperties this.DbName designDoc with
                | Ok _ ->
                    let singleQueryParameter = { Partitions.View.EmptyQueryParameters with Limit = Some 2 }
                    let queryParameters = (Partitions.View.QueryParameters.Single singleQueryParameter)
                    let! result = Partitions.View.queryWithAsResult<string, Default.T> Initialization.defaultDbProperties this.DbName partition1Name designDocId view1Name queryParameters
                    match result with
                    | Ok r ->
                        let response = r |> Partitions.View.responseAsSingleResponses |> List.exactlyOne
                        do response.Offset |> should equal 0
                        // Total rows returns the total number of rows for the view,
                        // not the number of rows returned (if limited by e.g. `limit`).
                        do response.TotalRows |> should equal 3 
                        do response.Rows.Length |> should equal 2
                    | Error e ->
                        failwith (sprintf "Could not retrieve the design document view because:%s%s" Environment.NewLine (ErrorRequestResult.textAsString e))
                | Error e ->
                    failwith (sprintf "Could not add the design document because:%s%s" Environment.NewLine (ErrorRequestResult.textAsString e))
            }
    
