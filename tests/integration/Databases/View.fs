namespace b0wter.CouchDb.Tests.Integration.Databases
open b0wter.CouchDb.Tests.Integration.DocumentTestModels

module View =
    
    open FsUnit.Xunit
    open System
    open Xunit
    open b0wter.CouchDb.Lib
    open b0wter.CouchDb.Tests.Integration
    open b0wter.CouchDb.Lib.Mango
    open b0wter.FSharp.Collections
    open FsUnit.CustomMatchers

    let id1 = "39346820-3700-4d09-b86c-e68653c98ca7"
    let id2 = "c51b3eae-73a5-4e18-9c29-701645cfb91e"
    let id3 = "94429f08-0b16-4076-be3e-bc47d4deea21"
    let id4 = "9f5f60ca-1225-4a02-9273-ff0984f2a2f2"

    let model1 = DocumentTestModels.Default.create (id1, 1, "one",   "eno",   2.5,  System.DateTime(1980, 1, 1, 12, 0, 0))
    let model2 = DocumentTestModels.Default.create (id2, 2, "one",   "owt",   2.5,  System.DateTime(1990, 10, 10, 20, 0, 0))
    let model3 = DocumentTestModels.Default.create (id3, 3, "three", "eerht", 3.14, System.DateTime(2000, 10, 10, 20, 0, 0))
    let model4 = DocumentTestModels.Default.create (id4, 3, "four", "rouf", 6.28, System.DateTime(2000, 10, 10, 20, 0, 0))

    let designDocId = "b1fbe634-a0b6-40c2-b898-c5c5e4e54f01"
    let view1Name = "all"
    let view2Name = "myIntEq3"
    let defaultView1 = DesignDocuments.DesignDocument.createView view1Name DesignDocuments.DesignDocument.Map "function(doc) { emit(doc._id, doc); }"
    let defaultView2 = DesignDocuments.DesignDocument.createView view2Name DesignDocuments.DesignDocument.Map "function(doc) { if (doc.myInt && doc.myInt === 3) { emit(doc._id, doc); } }"
    let designDoc = DesignDocuments.DesignDocument.createDocWithId designDocId [ defaultView1; defaultView2 ]

    type Tests() =
        inherit Utilities.PrefilledSingleDatabaseTests("database-view-tests", [ model1; model2; model3 ])

        [<Fact>]
        member this.``Querying 'all' view returns all documents.`` () =
            async {
                match! DesignDocuments.Put.queryAsResult Initialization.defaultDbProperties this.DbName designDoc with
                | Ok _ ->
                    let! result = Databases.View.queryAsResult<string, Default.T> Initialization.defaultDbProperties this.DbName designDocId view1Name
                    match result with
                    | Ok r ->
                        let responses = r |> Databases.View.responseAsSingleResponses |> List.exactlyOne
                        do responses.TotalRows |> should equal 3
                        do responses.Rows.Length |> should equal 3
                    | Error e ->
                        failwith (sprintf "Could not retrieve the design document view because:%s%s" Environment.NewLine (ErrorRequestResult.asString e))
                | Error e ->
                    failwith (sprintf "Could not add the design document because:%s%s" Environment.NewLine (ErrorRequestResult.asString e))
            }

        [<Fact>]
        member this.``Querying 'all' two times with 268435456 limit returns all elements twice.`` () =
            async {
                match! DesignDocuments.Put.queryAsResult Initialization.defaultDbProperties this.DbName designDoc with
                | Ok _ ->
                    let singleQueryParameter = { Databases.View.EmptyQueryParameters with Limit = Some 268435456}
                    let queryParameters = (Databases.View.QueryParameters.Multi {Databases.View.MultiQueryParameters.Queries = [ singleQueryParameter; singleQueryParameter ] })
                    let! result = Databases.View.queryWithAsResult<string, Default.T> Initialization.defaultDbProperties this.DbName designDocId view1Name queryParameters
                    match result with
                    | Ok r ->
                        let responses = r |> Databases.View.responseAsSingleResponses 
                        do responses.Length |> should equal 2
                        do responses |> List.iter (fun response -> 
                            do response.TotalRows |> should equal 3
                            do response.Rows.Length |> should equal 3)
                    | Error e ->
                        failwith (sprintf "Could not retrieve the design document view because:%s%s" Environment.NewLine (ErrorRequestResult.asString e))
                | Error e ->
                    failwith (sprintf "Could not add the design document because:%s%s" Environment.NewLine (ErrorRequestResult.asString e))
            }

        [<Fact>]
        member this.``Querying with a limit of 2 returns 2 elements.`` () =
            async {
                match! DesignDocuments.Put.queryAsResult Initialization.defaultDbProperties this.DbName designDoc with
                | Ok _ ->
                    let singleQueryParameter = { Databases.View.EmptyQueryParameters with Limit = Some 2 }
                    let queryParameters = (Databases.View.QueryParameters.Single singleQueryParameter)
                    let! result = Databases.View.queryWithAsResult<string, Default.T> Initialization.defaultDbProperties this.DbName designDocId view1Name queryParameters
                    match result with
                    | Ok r ->
                        let response = r |> Databases.View.responseAsSingleResponses |> List.exactlyOne
                        do response.Offset |> should equal 0
                        // Total rows returns the total number of rows for the view,
                        // not the number of rows returned (if limited by e.g. `limit`).
                        do response.TotalRows |> should equal 3 
                        do response.Rows.Length |> should equal 2
                    | Error e ->
                        failwith (sprintf "Could not retrieve the design document view because:%s%s" Environment.NewLine (ErrorRequestResult.asString e))
                | Error e ->
                    failwith (sprintf "Could not add the design document because:%s%s" Environment.NewLine (ErrorRequestResult.asString e))
            }
