namespace b0wter.CouchDb.Tests.Integration.Indexes

module GetIndices =
    
    open FsUnit.Xunit
    open Xunit
    open b0wter.CouchDb.Lib
    open b0wter.CouchDb.Tests.Integration
    open FsUnit.CustomMatchers
    open Newtonsoft.Json.Linq
    
    type Tests() =
        inherit Utilities.EmptySingleDatabaseTests("db-tests")

        let createFieldIndex dbName = 
            async {
                let index = Indexes.Create.createFieldsIndex [ "myField" ]
                let queryParameters = { Indexes.Create.EmptyQueryParameters with Index = index }
                return! Indexes.Create.queryAsResult Initialization.defaultDbProperties dbName queryParameters
            }

        let createSelectorIndex dbName = 
            async {
                let selector = Mango.condition "_id" <| Mango.Equal (Mango.Text "text") |> Mango.createExpression
                let index = Indexes.Create.createSelectorIndex selector
                let queryParameters = { Indexes.Create.EmptyQueryParameters with Index = index }
                return! Indexes.Create.queryAsResult Initialization.defaultDbProperties dbName queryParameters
            }
        
        [<Fact>]
        member this.``Getting an existing field-based index returns Success`` () =
            async {
                match! createFieldIndex this.DbName with
                | Ok _ -> 
                    let! result = Indexes.Get.query Initialization.defaultDbProperties this.DbName
                    
                    result |> should be (ofCase <@ Indexes.Get.Result.Success @>)
                | Error e -> failwith <| sprintf "Could not add an index to perform the 'get index' tests because: %s" (e.ToString())
            }

        [<Fact>]
        member this.``Getting an existing selector-based index returns Success`` () =
            async {
                match! createSelectorIndex this.DbName with
                | Ok _ -> 
                    let! result = Indexes.Get.query Initialization.defaultDbProperties this.DbName
                    
                    result |> should be (ofCase <@ Indexes.Get.Result.Success @>)
                | Error e -> failwith <| sprintf "Could not add an index to perform the 'get index' tests because: %s" (e.ToString())
            }

        [<Fact>]
        member this.``Getting a non-existing index returns NotFound`` () =
            async {
                let! result = Indexes.Get.query Initialization.defaultDbProperties "non_existing_index"
                result |> should be (ofCase <@ Indexes.Get.Result.NotFound @>)
            }

        [<Fact>]
        member this.``Getting indexes with an empty database name returns DbNameMissing`` () =
            async {
                let! result = Indexes.Get.query Initialization.defaultDbProperties System.String.Empty
                result |> should be (ofCase <@ Indexes.Get.Result.DbNameMissing @>)
            }
