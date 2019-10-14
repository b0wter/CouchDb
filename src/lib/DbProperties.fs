namespace b0wter.CouchDb.Lib

module DbProperties =

    /// Defines the http connection type.
    type ConnectionType
        = Https
        | Http

    /// Contains all information necessary to connect to a CouchDb instance.
    type T = {
        credentials: Credentials.T
        host: string
        port: int
        connectionType: ConnectionType
    }

    /// Contains all possible states of a `DbProperties.T` instance.
    type DbPropertiesCreateResult
        = Valid of T
        | HostIsEmpty
        | PortIsInvalid

    /// Creates a `DbProperties.T` instance.
    let create (host, port, credentials, connectionType) =
        match host, port with
        | (_, i) when i <= 0 || i >= 65536 -> PortIsInvalid
        | (h, _) when System.String.IsNullOrWhiteSpace(h) -> HostIsEmpty
        | _ ->
            {
                credentials = credentials
                host = host
                port = port
                connectionType = connectionType
            } |> Valid

    /// Uses the `connectionType`, `host` and `port` fields to create base url for a CouchDb server.
    let baseEndpoint (t: T) =
        let protocol = match t.connectionType with | Http -> "http" | Https -> "https"
        sprintf "%s://%s:%i/" protocol t.host t.port
