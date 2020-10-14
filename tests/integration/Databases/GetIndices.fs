namespace b0wter.CouchDb.Tests.Integration.Databases

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
                let index = Databases.CreateIndex.createFieldsIndex [ "myField" ]
                let queryParameters = { Databases.CreateIndex.EmptyQueryParameters with Index = index }
                return! Databases.CreateIndex.queryAsResult Initialization.defaultDbProperties dbName queryParameters
            }

        let createSelectorIndex dbName = 
            async {
                let selector = Mango.condition "_id" <| Mango.Equal (Mango.Text "text") |> Mango.createExpression
                let index = Databases.CreateIndex.createSelectorIndex selector
                let queryParameters = { Databases.CreateIndex.EmptyQueryParameters with Index = index }
                return! Databases.CreateIndex.queryAsResult Initialization.defaultDbProperties dbName queryParameters
            }
        
        [<Fact>]
        member this.GetAFieldIndex() =
            async {
                match! createFieldIndex this.DbName with
                | Ok _ -> 
                    let! result = Databases.GetIndices.query Initialization.defaultDbProperties this.DbName
                    
                    result |> should be (ofCase <@ Databases.GetIndices.Result.Success @>)
                | Error e -> failwith <| sprintf "Could not add an index to perform the 'get index' tests because: %s" (e.ToString())
            }

        [<Fact>]
        member this.GetASelectorIndex() =
            async {
                match! createSelectorIndex this.DbName with
                | Ok _ -> 
                    let! result = Databases.GetIndices.query Initialization.defaultDbProperties this.DbName
                    
                    result |> should be (ofCase <@ Databases.GetIndices.Result.Success @>)
                | Error e -> failwith <| sprintf "Could not add an index to perform the 'get index' tests because: %s" (e.ToString())
            }