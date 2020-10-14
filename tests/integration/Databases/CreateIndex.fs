namespace b0wter.CouchDb.Tests.Integration.Databases

module CreateIndex =
    
    open FsUnit.Xunit
    open Xunit
    open b0wter.CouchDb.Lib
    open b0wter.CouchDb.Tests.Integration
    open FsUnit.CustomMatchers
    open Newtonsoft.Json.Linq
    
    type Tests() =
        inherit Utilities.EmptySingleDatabaseTests("db-tests")
        

        [<Fact>]
        member this.``Creating an index with fields returns Success`` () =
            async {
                let index = Databases.CreateIndex.createFieldsIndex [ "field" ]
                let queryParameters = { Databases.CreateIndex.EmptyQueryParameters with Index = index }
                let! result = Databases.CreateIndex.query Initialization.defaultDbProperties this.DbName queryParameters

                result |> should be (ofCase <@ Databases.CreateIndex.Success @>)
            }

        [<Fact>]
        member this.``Creating an index with a selector retuns Success`` () =
            async {
                let selector = Mango.condition "_id" <| Mango.Equal (Mango.Text "text") |> Mango.createExpressionWithLimit 10
                let index = Databases.CreateIndex.createSelectorIndex selector
                let queryParameters = { Databases.CreateIndex.EmptyQueryParameters with Index = index }
                let! result = Databases.CreateIndex.query Initialization.defaultDbProperties this.DbName queryParameters
                result |> should be (ofCase <@ Databases.CreateIndex.Success @>)
            }

        [<Fact>]
        member this.``Creating an index with a fields and a selector returns Success`` () =
            async {
                let selector = Mango.condition "_id" <| Mango.Equal (Mango.Text "text") |> Mango.createExpression
                let index = Databases.CreateIndex.createFieldsAndSelectorIndex [ "field" ] selector
                let queryParameters = { Databases.CreateIndex.EmptyQueryParameters with Index = index }
                let! result = Databases.CreateIndex.query Initialization.defaultDbProperties this.DbName queryParameters
                result |> should be (ofCase <@ Databases.CreateIndex.Success @>)
            }

        [<Fact>]
        member this.``Querying a non-existing database returns NotFound`` () =
            async {
                let index = Databases.CreateIndex.createFieldsIndex [ "field" ]
                let queryParameters = { Databases.CreateIndex.EmptyQueryParameters with Index = index }
                let dbName = this.DbName + "__UNDEFINED_DATABASE"
                let! result = Databases.CreateIndex.query Initialization.defaultDbProperties dbName queryParameters

                result |> should be (ofCase <@ Databases.CreateIndex.NotFound @>)
            }
