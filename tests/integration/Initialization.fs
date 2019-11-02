namespace b0wter.CouchDb.Tests.Integration

open System.Collections.Generic
open b0wter.CouchDb.Lib
open b0wter.FSharp
open b0wter.FSharp.Operators
open Microsoft.Extensions.Configuration

module Initialization =
    
    /// <summary>
    /// Default db properties (localhost and default port).
    /// </summary>
    let defaultDbProperties =
        let configurationRoot = Configuration.getConfigurationRoot ()
        let host = configurationRoot.GetValue<string>("couchdb_host")
        let port = configurationRoot.GetValue<int>("couchdb_port")
        let user = configurationRoot.GetValue<string>("couchdb_user")
        let password = configurationRoot.GetValue<string>("couchdb_password")
        let credentials = Credentials.create(user, password)
        do printfn "Created default connection: %s:%i" host port
        match DbProperties.create (host, port, credentials, DbProperties.ConnectionType.Http) with
        | DbProperties.DbPropertiesCreateResult.Valid properties -> properties
        | DbProperties.DbPropertiesCreateResult.HostIsEmpty -> failwith "Host name is emty."
        | DbProperties.DbPropertiesCreateResult.PortIsInvalid -> failwith "Invalid port"
        
    
    /// <summary>
    /// Runs a login query using the default db properties and the default credentials.
    /// </summary>
    let authenticateCouchDbClient () =
        async {
            let! result = Server.Authenticate.query defaultDbProperties 
            match result with
            | Server.Authenticate.Result.Success _ -> return true
            | Server.Authenticate.Result.Found _ -> return true 
            | Server.Authenticate.Result.Unauthorized _ -> return failwith "Unknown username/password"
            | Server.Authenticate.Result.JsonDeserialisationError _ -> return failwith "JsonDeserialization of the servers response failed."
            | Server.Authenticate.Result.Unknown x -> return failwith <| sprintf "Unkown error occured: %s" x.content
        }
        
    /// <summary>
    /// Deletes all databases (using the default db properties).
    /// </summary>
    let deleteAllDatabases () =
        async {
            match! Server.AllDbs.query defaultDbProperties with
            | Server.AllDbs.Result.Success dbNames ->
                do printfn "Found the following databases: %A" dbNames
                if dbNames.IsEmpty then return true else
                let deleteResult = dbNames |> List.map (fun name ->
                    async {
                        match! Databases.Delete.query defaultDbProperties name with
                        | Databases.Delete.Result.Deleted deleted -> return deleted.ok
                        | Databases.Delete.Result.Accepted deleted -> return deleted.ok
                        | Databases.Delete.Result.Unauthorized x -> return failwith <| sprintf "Could not delete database, authorization missing. Details: %s" x.content
                        | Databases.Delete.Result.Unknown x -> return failwith <| sprintf "Could not delete database, encountered an unknown error. Details: %s" x.content
                        | Databases.Delete.Result.NotFound x -> return failwith <| sprintf "Could not delete database because of an NotFound error. Details: %s" x.content
                        | Databases.Delete.Result.BadRequest x -> return failwith <| sprintf "Could not delete database because of an BadRequest error. Details: %s" x.content
                    })
                let! deleteResult = Async.Parallel deleteResult
                return deleteResult |> Array.forall ((=) true)
            | Server.AllDbs.Result.JsonDeserialisationError f ->
                return failwith <| sprintf "Database deletion was probably successfull but the response could not be parsed: %s%s" System.Environment.NewLine f.content
            | Server.AllDbs.Result.Unknown f ->
                return failwith <| sprintf "Could not prepare the database because the database names could not be retrieved. Reason: %s" f.content
        }
        
    /// <summary>
    /// Creates a database using the default db properties.
    /// </summary>
    let createDatabases (names: string list) : Async<Result<bool, string>> =
        async {
            let queries = names |> List.map (fun name ->
                async {
                    match! Databases.Create.query defaultDbProperties name [] with
                    | Databases.Create.Result.Unauthorized x ->  return Error (sprintf "[%s] %s: %s" name "unauthorized" x.content)
                    | Databases.Create.Result.AlreadyExists x -> return Error (sprintf "[%s] %s: %s" name "unknown" x.content)
                    | Databases.Create.Result.InvalidDbName x -> return Error (sprintf "[%s] %s: %s" name "unknown" x.content)
                    | Databases.Create.Result.Unknown x ->       return Error (sprintf "[%s] %s: %s" name "unknown" x.content)
                    | Databases.Create.Result.Accepted _ ->      return Ok true
                    | Databases.Create.Result.Created _ ->       return Ok true
                })
            let! queryResults = Async.Parallel queries
            
            let resultAccumulator (isSuccess, errorMessage) (r: Result<bool, string>) = match r with
                                                                                        | Ok s    -> (s && isSuccess, errorMessage)
                                                                                        | Error e -> (false, sprintf "%s; %s" errorMessage e)
            let (success, errors) = queryResults |> Array.fold resultAccumulator (true, sprintf "Initialization.createDatabases failed because:%s" System.Environment.NewLine)
            
            return if success then Ok true else Error errors 
        }