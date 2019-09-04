namespace b0wter.CouchDb.Lib

module DbProperties =

    type ConnectionType
        = Https
        | Http

    type T = {
        credentials: Credentials.T
        host: string
        port: int
        connectionType: ConnectionType
    }

    type DbPropertiesCreateResult
        = Valid of T
        | HostIsEmpty
        | PortIsInvalid

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

    let baseEndpoint (t: T) =
        let protocol = match t.connectionType with | Http -> "http" | Https -> "https"
        sprintf "%s://%s:%i/" protocol t.host t.port
