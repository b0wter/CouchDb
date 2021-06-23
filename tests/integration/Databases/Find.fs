namespace b0wter.CouchDb.Tests.Integration.Databases
open b0wter.CouchDb.Tests.Integration.DocumentTestModels

module Find =
    
    open FsUnit.Xunit
    open Xunit
    open b0wter.CouchDb.Lib
    open b0wter.CouchDb.Tests.Integration
    open b0wter.CouchDb.Lib.Mango
    open b0wter.FSharp.Collections
    open FsUnit.CustomMatchers
    
    let id1 = ("39346820-3700-4d09-b86c-e68653c98ca7")
    let id2 = ("c51b3eae-73a5-4e18-9c29-701645cfb91e")
    let id3 = ("94429f08-0b16-4076-be3e-bc47d4deea21")
    
    let model1 = DocumentTestModels.Default.create (id1, 1, "one",   "eno",   2.5,  System.DateTime(1980, 1, 1, 12, 0, 0))
    let model2 = DocumentTestModels.Default.create (id2, 2, "one",   "owt",   2.5,  System.DateTime(1990, 10, 10, 20, 0, 0))
    let model3 = DocumentTestModels.Default.create (id3, 3, "three", "eerht", 3.14, System.DateTime(2000, 10, 10, 20, 0, 0))
    
    let sub1_1 = HierarchicalArray.createSubField (101, "substring", -3.14)
    let sub1_2 = HierarchicalArray.createSubField (102, "substring", -6.28)
    let sub2_1 = HierarchicalArray.createSubField (201, "substring", -9.42)
    let sub2_2 = HierarchicalArray.createSubField (202, "substring", -9.42)
    let sub3_1 = HierarchicalArray.createSubField (301, "substring", -9.42)
    let sub3_2 = HierarchicalArray.createSubField (302, "substring", -12.56)
    
    let hAModel1 = HierarchicalArray.create(id1, 42, "one",   11.1, [ sub1_1; sub1_2 ])
    let hAModel2 = HierarchicalArray.create(id2, 42, "two",   22.2, [ sub2_1; sub2_2 ])
    let hAModel3 = HierarchicalArray.create(id3, 42, "three", 33.3, [ sub3_1; sub3_2 ])
    
    let hModel1 = Hierarchical.create (id1, 42, "one",   11.1, "sub-one", -21, -11.1)
    let hModel2 = Hierarchical.create (id2, 42, "one",   22.2, "sub-two", -42, -22.2)
    let hModel3 = Hierarchical.create (id3, 42, "three", 33.3, "sub-two", -42, -33.3)
    
    let hSModel1 = HierarchicalSimpel.create (id1, 42, "one", 11.1, [1;2;3;4])
    let hSModel2 = HierarchicalSimpel.create (id2, 43, "one", 22.2, [1;2;7;9])
    let hSModel3 = HierarchicalSimpel.create (id3, 44, "one", 33.3, [1;5;6;9])
    
    type GenericTests() =
        inherit Utilities.PrefilledSingleDatabaseTests("database-find-tests", [ model1; model2; model3 ])
        
        [<Fact>]
        member this.``getFirst on a successful query returns the first element of a query`` () =
            async {
                let selector = condition "_id" <| Equal (Text id1)
                let expression = createExpression selector
                let! result = Databases.Find.query<DocumentTestModels.Default.T> Initialization.defaultDbProperties this.DbName expression
                do result |> should be (ofCase <@ Databases.Find.Result<DocumentTestModels.Default.T>.Success @>)
                
                match result |> Databases.Find.getFirst with
                | Ok o -> o |> Default.compareWithoutRev model1
                | Error e -> failwith e
            }
    
    type EqualsTests() =
        inherit Utilities.PrefilledSingleDatabaseTests("database-find-tests", [ model1; model2; model3 ])

        [<Fact>]
        member this.``Find using Equal String returns matching documents`` () =
            async {
                let selector = condition "type" <| Equal (Text "Default")
                let expression = createExpression selector
                let! result = Databases.Find.query<DocumentTestModels.Default.T> Initialization.defaultDbProperties this.DbName expression
                match result with
                | Databases.Find.Result.Success testModels ->
                    let m1 = testModels.Docs |> List.find (fun x -> x._id = id1) 
                    let m2 = testModels.Docs |> List.find (fun x -> x._id = id2) 
                    let m3 = testModels.Docs |> List.find (fun x -> x._id = id3)
                    
                    do (m1 |> DocumentTestModels.Default.compareWithoutRev model1)
                    do (m2 |> DocumentTestModels.Default.compareWithoutRev model2) 
                    do (m3 |> DocumentTestModels.Default.compareWithoutRev model3)
                    
                | _ -> failwith <| sprintf "Find query failed, got result: %s" (result.GetType().FullName)
            }
            
        [<Fact>]
        member this.``Find using Equal Integer returns matching documents`` () =
            async {
                let selector = condition "myInt" <| Equal (Integer 1)
                let expression = createExpression selector
                let! result = Databases.Find.query<DocumentTestModels.Default.T> Initialization.defaultDbProperties this.DbName expression
                match result with
                | Databases.Find.Result.Success testModels ->
                    do testModels.Docs |> should haveLength 1
                    do testModels.Docs.Head |> DocumentTestModels.Default.compareWithoutRev model1
                | _ -> failwith <| sprintf "Find query failed, got result: %s" (result.GetType().FullName)
            }
            
        [<Fact>]
        member this.``Find using Equal Float returns matching documents`` () =
            async {
                let selector = condition "myFloat" <| Equal (Float 3.14)
                let expression = createExpression selector
                let! result = Databases.Find.query<DocumentTestModels.Default.T> Initialization.defaultDbProperties this.DbName expression
                match result with
                | Databases.Find.Result.Success testModels ->
                    do testModels.Docs |> should haveLength 1
                    do testModels.Docs.Head |> DocumentTestModels.Default.compareWithoutRev model3
                | _ -> failwith <| sprintf "Find query failed, got result: %s" (result.GetType().FullName)
            }
            
        [<Fact>]
        member this.``Find using two equal conditionals returns matching documents`` () =
            async {
                
                let myIntEquals1 = condition "myInt" (Equal <| Integer 1)
                let myIntEquals2 = condition "myInt" (Equal <| Integer 2)
                let myInt1Or2 = myIntEquals1 |> ``or`` myIntEquals2
                let expression = createExpression myInt1Or2
                let! result = Databases.Find.query<DocumentTestModels.Default.T> Initialization.defaultDbProperties this.DbName expression
                match result with
                | Databases.Find.Result.Success testModels ->
                    testModels.Docs |> should haveLength 2
                    let m1 = testModels.Docs |> List.find (fun x -> x._id = id1) 
                    let m2 = testModels.Docs |> List.find (fun x -> x._id = id2) 
                    
                    do (m1 |> DocumentTestModels.Default.compareWithoutRev model1)
                    do (m2 |> DocumentTestModels.Default.compareWithoutRev model2)
                                       
                | _ -> failwith <| sprintf "Find query failed, got result: %s" (result.GetType().FullName)
            }
            
        [<Fact>]
        member this.``Find using an invalid db name returns InvalidDbName`` () =
            async {
                let nonExistingDbName = this.DbName + "_non-existing"
                let selector = condition "myInt" <| Equal (Integer 1)
                let expression = createExpression selector
                let! result = Databases.Find.query<DocumentTestModels.Default.T> Initialization.defaultDbProperties nonExistingDbName expression
                do result |> should be (ofCase <@ Databases.Find.Result<DocumentTestModels.Default.T>.NotFound @>)
            }

    type ElemComplexMatchTests() =
        inherit Utilities.PrefilledSingleDatabaseTests("database-find-tests", [ hAModel1; hAModel2; hAModel3 ])

        [<Fact>]
        member this.``Find using an ElementMatch on a valid db returns Success result`` () =
            async {
                let elementSelector = condition "subFloat" (Equal <| Float -9.42)
                let selector = combination <| ElementMatch (elementSelector, "mySubs")
                let expression = createExpression selector
                let! result = Databases.Find.query<HierarchicalArray.T> Initialization.defaultDbProperties this.DbName expression
                do result |> should be (ofCase <@ Databases.Find.Result<HierarchicalArray.T>.Success @>)
                
                match result with
                | Databases.Find.Result.Success s ->
                    do s.Docs |> should haveLength 2
                    let s2 = s.Docs |> List.find (fun x -> x._id = id2)
                    let s3 = s.Docs |> List.find (fun x -> x._id = id3)
                    
                    do (s2 |> HierarchicalArray.compareWithoutRev hAModel2)
                    do (s3 |> HierarchicalArray.compareWithoutRev hAModel3)
                | _ -> failwith "This non-matching union case should have been caught earlier! Please fix the test!"
            }
            
    type ElemSimpleMatchTests() =
        inherit Utilities.PrefilledSingleDatabaseTests("database-find-tests", [ hSModel1; hSModel2; hSModel3 ])

        [<Fact>]
        member this.``Find using an ElementMatch on a valid db returns Success result`` () =
            async {
                let elementSelector = condition "" (Equal <| Integer 9)
                let selector = combination <| ElementMatch (elementSelector, "mySubs")
                let expression = createExpression selector
                let! result = Databases.Find.query<HierarchicalSimpel.T> Initialization.defaultDbProperties this.DbName expression
                do result |> should be (ofCase <@ Databases.Find.Result<HierarchicalSimpel.T>.Success @>)
                
                match result with
                | Databases.Find.Result.Success s ->
                    do s.Docs |> should haveLength 2
                    let s2 = s.Docs |> List.find (fun x -> x._id = id2)
                    let s3 = s.Docs |> List.find (fun x -> x._id = id3)
                    
                    do (s2 |> HierarchicalSimpel.compareWithoutRev hSModel2)
                    do (s3 |> HierarchicalSimpel.compareWithoutRev hSModel3)
                | _ -> failwith "This non-matching union case should have been caught earlier! Please fix the test!"
            }
            
    type AllMatchTests() =
        inherit Utilities.PrefilledSingleDatabaseTests("database-find-tests", [ hAModel1; hAModel2; hAModel3 ])

        [<Fact>]
        member this.``Find using an AllMatch on a valid db returns Success result`` () =
            async {
                let elementSelector = condition "subFloat" (Equal <| Float -9.42)
                let selector = combination <| AllMatch (elementSelector, "mySubs")
                let expression = createExpression selector
                let! result = Databases.Find.query<HierarchicalArray.T> Initialization.defaultDbProperties this.DbName expression
                do result |> should be (ofCase <@ Databases.Find.Result<HierarchicalArray.T>.Success @>)
                
                match result with
                | Databases.Find.Result.Success s ->
                    do s.Docs |> should haveLength 1
                    let s2 = s.Docs |> List.find (fun x -> x._id = id2)
                    
                    do (s2 |> HierarchicalArray.compareWithoutRev hAModel2)
                | _ -> failwith "This non-matching union case should have been caught earlier! Please fix the test!"
            }
            
    type JObjectTests() =
        inherit Utilities.PrefilledSingleDatabaseTests("database-find-tests", [ hModel1; hModel2; hModel3 ])

        [<Fact>]
        member this.``Running a selector matching all elements return three JObjects.`` () =
            async {
                let selector = condition "myInt" (Equal <| Integer 42)
                let expression = createExpression selector
                let! result = Databases.Find.queryObjects Initialization.defaultDbProperties this.DbName expression
                
                match result with
                | Databases.Find.Result.Success s ->
                    do s.Docs |> should haveLength 3
                | _ -> failwith "This non-matching union case should have been caught earlier! Please fix the test!"
            }

        [<Fact>]
        member this.``Running a selector matching two elements returns two JOjects`` () =
            async {
                let selector = condition "myString" (Equal <| Text "one")
                let expression = createExpression selector
                let! result = Databases.Find.queryObjects Initialization.defaultDbProperties this.DbName expression
                
                match result with
                | Databases.Find.Result.Success s ->
                    do s.Docs |> should haveLength 2
                | _ -> failwith "This non-matching union case should have been caught earlier! Please fix the test!"
            }
        
        [<Fact>]
        member this.``Running a selector matching a single element returns a single JObject``() =
            async {
                let selector = condition "_id" (Equal <| Text id1)
                let expression = createExpression selector
                let! result = Databases.Find.queryObjects Initialization.defaultDbProperties this.DbName expression
                
                match result with
                | Databases.Find.Result.Success s ->
                    do s.Docs |> should haveLength 1
                | _ -> failwith "This non-matching union case should have been caught earlier! Please fix the test!"
            }