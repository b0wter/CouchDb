namespace b0wter.CouchDb.Tests.Integration.Databases

module Infos =
    
    open FsUnit.Xunit
    open Xunit
    open b0wter.CouchDb.Lib
    open b0wter.CouchDb.Tests.Integration
    open FsUnit.CustomMatchers
    
    type Tests() =
        inherit Utilities.EmptySingleDatabaseTests("db-tests")
        

        [<Fact>]
        member this.``Retrieving database info for an existing database returns infos`` () =
            async {
                let! result = Databases.Infos.query Initialization.defaultDbProperties this.DbName
                result |> should be (ofCase <@ Databases.Infos.Success @>)
            }

        [<Fact>]
        member this.``Retrieving database info for an non-existing database returns NotFound`` () =
            async {
                let nonExistingDbName = this.DbName + "__NON_EXISTING"
                let! result = Databases.Infos.query Initialization.defaultDbProperties nonExistingDbName
                result |> should be (ofCase <@ Databases.Infos.NotFound @>)
            }