namespace b0wter.CouchDb.Tests.Integration
open Docker.DotNet.Models
open System.Collections.Generic
open b0wter.CouchDb.Lib
open b0wter.FSharp
open b0wter.FSharp.Operators

module Initialization =

    /// <summary>
    /// Default CouchDb credentials.
    /// </summary>
    let defaultCredentials = Credentials.create("admin", "password")
    
    /// <summary>
    /// Default db properties (localhost and default port).
    /// </summary>
    let defaultDbProperties = 
        match DbProperties.create ("localhost", 5984, defaultCredentials, DbProperties.ConnectionType.Http) with
        | DbProperties.DbPropertiesCreateResult.Valid properties -> properties
        | DbProperties.DbPropertiesCreateResult.HostIsEmpty -> failwith "Host name is emty."
        | DbProperties.DbPropertiesCreateResult.PortIsInvalid -> failwith "Invalid port"
        
    /// <summary>
    /// Docker client using the default settings (local machine).
    /// </summary>
    let dockerClient = (new Docker.DotNet.DockerClientConfiguration(System.Uri("unix:///var/run/docker.sock"))).CreateClient()
    
    /// <summary>
    /// Runs a login query using the default db properties and the default credentials.
    /// </summary>
    let authenticateCouchDbClient () =
        async {
            let! result = Core.authenticate defaultDbProperties 
            match result with
            | Ok _ -> return true
            | Error e -> return failwith <| e.reason
        }
    
    /// <summary>
    /// Default port mapping to expose the default CouchDb port.
    /// </summary>
    let private defaultPortMapping =
        let binding = PortBinding()
        do binding.HostPort <- "5984"
        System.Collections.Generic.List<PortBinding>([binding])
        
    /// <summary>
    /// Collection wrapping the defaultPortMapping.
    /// </summary>
    let private defaultPortMappings : IDictionary<string, IList<PortBinding>> =
        let dict = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.IList<PortBinding>>()
        do dict.Add("5984/tcp", defaultPortMapping)
        dict :> IDictionary<string, IList<PortBinding>>
    
    /// <summary>
    /// Returns image creation parameters using the given imageName and tag.
    /// </summary>
    let private defaultImageCreationParameters (imageName: string, tag: string) =
        let imageCreationParameters = Docker.DotNet.Models.ImagesCreateParameters()
        do imageCreationParameters.Repo <- imageName
        do imageCreationParameters.Tag <- tag
        imageCreationParameters
    
    /// <summary>
    /// Default host configuration with the default port mapping.
    /// </summary>
    let private defaultHostConfiguration =
        let hostConfiguration = Docker.DotNet.Models.HostConfig()
        do hostConfiguration.PortBindings <- defaultPortMappings //<- defaultPortMapping
        hostConfiguration
    
    /// <summary>
    /// Returns the default container creation parameters for the given image, container name and host configuration.
    /// Uses the 'latest' tag.
    /// </summary>
    let private defaultContainerCreationParameters (imageName: string, tag: string) (containerName: string) hostConfiguration = 
        let containerCreationParameters = Docker.DotNet.Models.CreateContainerParameters()
        do containerCreationParameters.Image <- sprintf "%s:%s" imageName tag
        do containerCreationParameters.Name <- containerName
        do containerCreationParameters.HostConfig <- hostConfiguration
        containerCreationParameters
        
    /// <summary>
    /// Default parameters to start containers.
    /// </summary>
    let private defaultStartParameters =
        ContainerStartParameters()
        
    /// <summary>
    /// Runs a docker image as a container using the default parameters.
    /// </summary>
    let runDockerImage (client: Docker.DotNet.DockerClient) (imageName: string) (containerName: string) (tag: string option) =
        async {
            let containerCreationParameters = defaultContainerCreationParameters (imageName, tag |?| "latest") containerName defaultHostConfiguration
            let exposedPorts = new Dictionary<string, Docker.DotNet.Models.EmptyStruct>()
            do exposedPorts.Add("5984/tcp", EmptyStruct())
            do containerCreationParameters.ExposedPorts <- exposedPorts
            
            let! creationResult = client.Containers.CreateContainerAsync(containerCreationParameters) |> Async.AwaitTask
            return! client.Containers.StartContainerAsync(creationResult.ID, defaultStartParameters) |> Async.AwaitTask
        }
        
    /// <summary>
    /// Deletes all databases (using the default db properties).
    /// </summary>
    let deleteAllDatabases () =
        async {
            match! Database.All.query defaultDbProperties with
            | Database.All.Result.Success dbNames ->
                if dbNames.IsEmpty then return true else
                let deleteResult = dbNames |> List.map (fun name ->
                    async {
                        match! Database.Delete.query defaultDbProperties name with
                        | Database.Delete.Result.Deleted deleted -> return deleted.ok
                        | Database.Delete.Result.Accepted deleted -> return deleted.ok
                        | Database.Delete.Result.Unauthorized x -> return failwith <| sprintf "Could not delete database, authorization missing. Details: %s" x.reason
                        | Database.Delete.Result.Unknown x -> return failwith <| sprintf "Could not delete database, encountered an unknown error. Details: %s" x.reason
                        | Database.Delete.Result.InvalidDatabase x -> return failwith <| sprintf "Could not delete database because of an InvalidDatabase error. Details: %s" x.reason
                    })
                let! deleteResult = Async.Parallel deleteResult
                return deleteResult |> Array.forall ((=) true)
            | Database.All.Result.Failure f ->
                return failwith <| sprintf "Could not prepare the database because the database names could not be retrieved. Reason: %s" f.reason
                
        }
        
    /// <summary>
    /// Creates a database using the default db properties.
    /// </summary>
    let createDatabases (names: string list) =
        async {
            let queries = names |> List.map (fun name ->
                async {
                    match! Database.Create.query defaultDbProperties name None None with
                    | Database.Create.Result.HttpError _ -> return false
                    | Database.Create.Result.Unauthorized _ -> return false
                    | Database.Create.Result.AlreadyExists _ -> return false
                    | Database.Create.Result.InvalidDbName _ -> return false
                    | Database.Create.Result.Unknown _ -> return false
                    | Database.Create.Result.Accepted _ -> return true
                    | Database.Create.Result.Created _ -> return true
                })
            let! queryResults = Async.Parallel queries
            return queryResults |> Array.forall ((=) true)
        }