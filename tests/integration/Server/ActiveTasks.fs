namespace b0wter.CouchDb.Tests.Integration.Server

module ActiveTasks =
    
    open FsUnit.Xunit
    open Xunit
    open b0wter.CouchDb.Lib
    open FsUnit.CustomMatchers
    open b0wter.CouchDb.Tests.Integration

    type Tests() =
        inherit Utilities.CleanDatabaseTests()

        [<Fact>]
        member this.``Requesting active tasks returns the active tasks`` () =
            async {
                let! result = Server.ActiveTasks.query Initialization.defaultDbProperties
                result |> should be (ofCase <@ Server.ActiveTasks.Result.Success @>)
            }