namespace b0wter.CouchDb.Tests.Integration.Indexes

module CreateIndex =
    
    open FsUnit.Xunit
    open Xunit
    open b0wter.CouchDb.Lib
    open b0wter.CouchDb.Tests.Integration
    open FsUnit.CustomMatchers
    
    type Tests() =
        inherit Utilities.EmptySingleDatabaseTests("db-tests")
        

        [<Fact>]
        member this.``Creating an index with fields returns Success`` () =
            async {
                let index = Indexes.Create.createFieldsIndex [ "field" ]
                let queryParameters = { Indexes.Create.EmptyQueryParameters with Index = index }
                let! result = Indexes.Create.query Initialization.defaultDbProperties this.DbName queryParameters

                result |> should be (ofCase <@ Indexes.Create.Success @>)
            }

        [<Fact>]
        member this.``Creating an index with a selector retuns Success`` () =
            async {
                let selector = Mango.condition "_id" <| Mango.Equal (Mango.Text "text") |> Mango.createExpressionWithLimit 10
                let index = Indexes.Create.createSelectorIndex selector
                let queryParameters = { Indexes.Create.EmptyQueryParameters with Index = index }
                let! result = Indexes.Create.query Initialization.defaultDbProperties this.DbName queryParameters
                result |> should be (ofCase <@ Indexes.Create.Success @>)
            }

        [<Fact>]
        member this.``Creating an index with a fields and a selector returns Success`` () =
            async {
                let selector = Mango.condition "_id" <| Mango.Equal (Mango.Text "text") |> Mango.createExpression
                let index = Indexes.Create.createFieldsAndSelectorIndex [ "field" ] selector
                let queryParameters = { Indexes.Create.EmptyQueryParameters with Index = index }
                let! result = Indexes.Create.query Initialization.defaultDbProperties this.DbName queryParameters
                result |> should be (ofCase <@ Indexes.Create.Success @>)
            }

        [<Fact>]
        member this.``Creating an index on a non-existing database returns NotFound`` () =
            async {
                let index = Indexes.Create.createFieldsIndex [ "field" ]
                let queryParameters = { Indexes.Create.EmptyQueryParameters with Index = index }
                let dbName = this.DbName + "__UNDEFINED_DATABASE"
                let! result = Indexes.Create.query Initialization.defaultDbProperties dbName queryParameters

                result |> should be (ofCase <@ Indexes.Create.NotFound @>)
            }

        [<Fact>]
        member this.``Creating an index with an empty database name returns DbNameMissing`` () =
            async {
                let index = Indexes.Create.createFieldsIndex [ "field" ]
                let queryParameters = { Indexes.Create.EmptyQueryParameters with Index = index }
                let dbName = System.String.Empty
                let! result = Indexes.Create.query Initialization.defaultDbProperties dbName queryParameters

                result |> should be (ofCase <@ Indexes.Create.DbNameMissing @>)
            }
