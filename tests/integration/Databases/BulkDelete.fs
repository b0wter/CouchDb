namespace b0wter.CouchDb.Tests.Integration.Databases

module BulkDelete =
    
    open FsUnit.Xunit
    open Xunit
    open b0wter.CouchDb.Lib
    open b0wter.CouchDb.Tests.Integration
    open FsUnit.CustomMatchers
    
    let id1 = "39346820-3700-4d09-b86c-e68653c98ca7"
    let id2 = "c51b3eae-73a5-4e18-9c29-701645cfb91e"
    let id3 = "94429f08-0b16-4076-be3e-bc47d4deea21"
    
    let model1 = DocumentTestModels.Default.create (id1, 1, "one",   "eno",   2.5,  System.DateTime(1980, 1, 1, 12, 0, 0))
    let model2 = DocumentTestModels.Default.create (id2, 2, "one",   "owt",   2.5,  System.DateTime(1990, 10, 10, 20, 0, 0))
    let model3 = DocumentTestModels.Default.create (id3, 3, "three", "eerht", 3.14, System.DateTime(2000, 10, 10, 20, 0, 0))

    type Tests() =
        inherit Utilities.PrefilledSingleDatabaseTests("bulk-delete-tests", [model1; model2; model3])
        
        [<Fact>]
        member this.``Trying to delete three documents at once deletes three documents`` () =
            async {
                let! allDocs = Databases.AllDocuments.queryAllAsResult Initialization.defaultDbProperties this.DbName
                match allDocs with
                | Ok o ->
                    do o.Rows |> should haveLength 3
                    let idsAndRevs = [id1; id2; id3] |> List.map (fun id -> (id, (o.Rows |> List.find (fun row -> row.Id = id)).Value.Value.Rev))
                    
                    let! deleteMany = Databases.BulkDelete.query Initialization.defaultDbProperties this.DbName (System.String.IsNullOrWhiteSpace >> not) (System.String.IsNullOrWhiteSpace >> not) idsAndRevs
                    do deleteMany |> should be (ofCase <@ Databases.BulkDelete.Result.Created @>)
                    
                    let! documentCountCheck = Server.DbsInfo.queryAsResult Initialization.defaultDbProperties [this.DbName]

                    match documentCountCheck with
                    | Ok o ->
                        do o.[0].Info.Value.DocCount |> should equal 0
                        do o.[0].Info.Value.DocDelCount |> should equal 3
                    | Error e ->
                        failwith (e |> ErrorRequestResult.textAsString)
                | Error e -> failwith (e |> ErrorRequestResult.textAsString)
            }
