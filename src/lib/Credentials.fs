namespace b0wter.CouchDb.Lib

module Credentials =
    open Newtonsoft.Json

    /// Credentials for the CouchDb login.
    type Credentials = {
        [<JsonProperty("name")>]
        Username: string
        Password: string
    }

    /// Creates a credentials instance.
    let create (username , password) =
        { Username = username; Password = password }

    /// Represents all possible states the credentials might have.
    type CredentialStatus
        /// Credentials are valid, meaning they contain a username and a password.
        = Valid of Credentials
        /// The username is empty or missing.
        | MissingUsername
        /// The password is empty or missing.
        | MissingPassword
        /// Username and password are both either empty or missing.
        | MissingUsernameAndPassword

    /// Takes credentials and checks their validity.
    let validate (t: Credentials) : CredentialStatus =
        match (t.Username |> System.String.IsNullOrWhiteSpace, t.Password |> System.String.IsNullOrWhiteSpace) with
        | true, true -> MissingUsernameAndPassword
        | true, false -> MissingPassword
        | false, true -> MissingUsername
        | false, false -> Valid t
