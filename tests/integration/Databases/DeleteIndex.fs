namespace b0wter.CouchDb.Tests.Integration.Databases

module DeleteIndex =
    
    open FsUnit.Xunit
    open Xunit
    open b0wter.CouchDb.Lib
    open b0wter.CouchDb.Tests.Integration
    open FsUnit.CustomMatchers
    open Newtonsoft.Json.Linq
    
    type Tests() =
        inherit Utilities.EmptySingleDatabaseTests("db-tests")
        