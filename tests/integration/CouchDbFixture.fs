namespace b0wter.CouchDb.Tests.Integration

module CouchDbFixture =

    // TODO: Use this library: https://github.com/microsoft/Docker.DotNet

    type T() =
        do
            Initialization.authenticateCouchDbClient () |> Async.RunSynchronously |> ignore
            Initialization.deleteAllDatabases () |> Async.RunSynchronously |> ignore
        interface System.IDisposable with
            member this.Dispose () =
                do printfn "Disposing database."
