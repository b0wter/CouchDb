namespace b0wter.CouchDb.Tests.Integration.Databases
open b0wter.CouchDb.Tests.Integration.TestModels

module Find =
    
    open FsUnit.Xunit
    open Xunit
    open b0wter.CouchDb.Lib
    open b0wter.CouchDb.Tests.Integration
    open b0wter.CouchDb.Lib.Mango
    open b0wter.FSharp.Collections
    open FsUnit.CustomMatchers
    
    let id1 = System.Guid.Parse("39346820-3700-4d09-b86c-e68653c98ca7")
    let id2 = System.Guid.Parse("c51b3eae-73a5-4e18-9c29-701645cfb91e")
    let id3 = System.Guid.Parse("94429f08-0b16-4076-be3e-bc47d4deea21")
    
    let model1 = TestModels.Default.create (id1, 1, "one",   "eno",   2.5,  System.DateTime(1980, 1, 1, 12, 0, 0))
    let model2 = TestModels.Default.create (id2, 2, "one",   "owt",   2.5,  System.DateTime(1990, 10, 10, 20, 0, 0))
    let model3 = TestModels.Default.create (id3, 3, "three", "eerht", 3.14, System.DateTime(2000, 10, 10, 20, 0, 0))
    
    type Tests() =
        inherit Utilities.PrefilledSingleDatabaseTests("database-find-tests", [ model1; model2; model3 ])

        [<Fact>]
        member this.``Find using Equal String returns matching documents`` () =
            async {
                
                let selector = condition "type" <| Equal (Text "Default")
                let expression = createExpression selector
                let! result = Databases.Find.query<TestModels.Default.T> Initialization.defaultDbProperties this.DbName expression
                match result with
                | Databases.Find.Result.Success testModels ->
                    let m1 = testModels.docs |> List.find (fun x -> x._id = id1) 
                    let m2 = testModels.docs |> List.find (fun x -> x._id = id2) 
                    let m3 = testModels.docs |> List.find (fun x -> x._id = id3)
                    
                    do (m1 |> TestModels.Default.compareWithoutRev model1)
                    do (m2 |> TestModels.Default.compareWithoutRev model2) 
                    do (m3 |> TestModels.Default.compareWithoutRev model3)
                    
                | _ -> failwith <| sprintf "Find query failed, got result: %s" (result.GetType().FullName)
            }
            
        [<Fact>]
        member this.``Find using Equal Integer returns matching documents`` () =
            async {
                let selector = condition "myInt" <| Equal (Integer 1)
                let expression = createExpression selector
                let! result = Databases.Find.query<TestModels.Default.T> Initialization.defaultDbProperties this.DbName expression
                match result with
                | Databases.Find.Result.Success testModels ->
                    do testModels.docs |> should haveLength 1
                    do testModels.docs.Head |> TestModels.Default.compareWithoutRev model1
                | _ -> failwith <| sprintf "Find query failed, got result: %s" (result.GetType().FullName)
            }
            
        [<Fact>]
        member this.``Find using Equal Float returns matching documents`` () =
            async {
                let selector = condition "myFloat" <| Equal (Float 3.14)
                let expression = createExpression selector
                let! result = Databases.Find.query<TestModels.Default.T> Initialization.defaultDbProperties this.DbName expression
                match result with
                | Databases.Find.Result.Success testModels ->
                    do testModels.docs |> should haveLength 1
                    do testModels.docs.Head |> TestModels.Default.compareWithoutRev model3
                | _ -> failwith <| sprintf "Find query failed, got result: %s" (result.GetType().FullName)
            }
            
        [<Fact>]
        member this.``Find using two equal conditionals returns matching documents`` () =
            async {
                
                let myIntEquals1 = condition "myInt" (Equal <| Integer 1)
                let myIntEquals2 = condition "myInt" (Equal <| Integer 2)
                let myInt1Or2 = myIntEquals1 |> ``or`` myIntEquals2
                let expression = createExpression myInt1Or2
                let! result = Databases.Find.query<TestModels.Default.T> Initialization.defaultDbProperties this.DbName expression
                match result with
                | Databases.Find.Result.Success testModels ->
                    testModels.docs |> should haveLength 2
                    let m1 = testModels.docs |> List.find (fun x -> x._id = id1) 
                    let m2 = testModels.docs |> List.find (fun x -> x._id = id2) 
                    
                    do (m1 |> TestModels.Default.compareWithoutRev model1)
                    do (m2 |> TestModels.Default.compareWithoutRev model2)
                                       
                | _ -> failwith <| sprintf "Find query failed, got result: %s" (result.GetType().FullName)
            }
            
        [<Fact>]
        member this.``Find using an invalid db name returns InvalidDbName`` () =
            async {
                let nonExistingDbName = this.DbName + "_non-existing"
                let selector = condition "myInt" <| Equal (Integer 1)
                let expression = createExpression selector
                let! result = Databases.Find.query<TestModels.Default.T> Initialization.defaultDbProperties nonExistingDbName expression
                do result |> should be (ofCase <@ Databases.Find.Result<TestModels.Default.T>.NotFound @>)
            }

        [<Fact>]
        member this.``getFirst on a successful query returns the first element of a query`` () =
            async {
                let selector = condition "_id" <| Equal (Id id1)
                let expression = createExpression selector
                let! result = Databases.Find.query<TestModels.Default.T> Initialization.defaultDbProperties this.DbName expression
                do result |> should be (ofCase <@ Databases.Find.Result<TestModels.Default.T>.Success @>)
                
                match result |> Databases.Find.getFirst with
                | Ok o -> o |> Default.compareWithoutRev model1
                | Error e -> failwith e
            }