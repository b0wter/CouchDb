namespace b0wter.CouchDb.Lib

module Credentials =

    type T = {
        username: string
        password: string
    }

    type U
        = Set of T
        | Unset
    
    let create (username , password) =
        { username = username; password = password }

    type CredentialStatus 
        = Valid of T
        | MissingUsername
        | MissingPassword
        | MissingUsernameAndPassword

    let validate (t: T) : CredentialStatus =
        match (t.username |> System.String.IsNullOrWhiteSpace, t.password |> System.String.IsNullOrWhiteSpace) with
        | true, true -> MissingUsernameAndPassword
        | true, false -> MissingPassword
        | false, true -> MissingUsername
        | false, false -> Valid t
