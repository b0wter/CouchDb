namespace b0wter.CouchDb.Lib

module Credentials =
    open Newtonsoft.Json

    /// Credentials for the CouchDb login.
    type T = {
        [<JsonProperty("name")>]
        username: string
        password: string
    }

    /// Creates a credentials instance.
    let create (username , password) =
        { username = username; password = password }

    /// Represents all possible states the credentials might have.
    type CredentialStatus
        /// Credentials are valid, meaning they contain a username and a password.
        = Valid of T
        /// The username is empty or missing.
        | MissingUsername
        /// The password is empty or missing.
        | MissingPassword
        /// Username and password are both either empty or missing.
        | MissingUsernameAndPassword

    /// Takes credentials and checks their validity.
    let validate (t: T) : CredentialStatus =
        match (t.username |> System.String.IsNullOrWhiteSpace, t.password |> System.String.IsNullOrWhiteSpace) with
        | true, true -> MissingUsernameAndPassword
        | true, false -> MissingPassword
        | false, true -> MissingUsername
        | false, false -> Valid t
