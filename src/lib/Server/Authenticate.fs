namespace b0wter.CouchDb.Lib.Server

//
// Queries: /_all_dbs
//

open b0wter.CouchDb.Lib.Core
open b0wter.CouchDb.Lib
open b0wter.FSharp

module Authenticate =
    type Vendor = {
        Version: string
        Name: string
    }
    
    type Response = {
        CouchDb: string
        Uuid: string
        Version: string
        Vendor: Vendor    
    }
    
    type Result
        /// Successfully authenticated (200)
        = Success of Response
        /// Redirect after successful authentication (302)
        | Found of Response
        /// Username or password wasn’t recognized (401)
        | Unauthorized of RequestResult.TString
        /// Deserialization of the recieved response failed.
        | JsonDeserialisationError of RequestResult.TString
        /// Response could not be interpreted.
        | Unknown of RequestResult.TString
        
    /// <summary>
    /// Sends an authentication request to the database.
    /// The result is stored in the default cookie container so that each subsequent
    /// request is automatically authenticated.
    /// </summary>
    let query (props: DbProperties.T) =
        async {
            let formData = [("name", props.Credentials.Username :> obj); ("password", props.Credentials.Password :> obj)] |> Map.ofList
            let request = createFormPost props "_session" formData []
            let! result = sendTextRequest request 
            return match result.StatusCode with
                    | Some 200 | Some 302->
                        match deserializeJson<Response> result.Content with
                        | Ok r -> Success r
                        | Error e -> JsonDeserialisationError <| RequestResult.createForJson(e, result.StatusCode, result.Headers)
                    | Some 401 -> Unauthorized result
                    | _ -> Unknown result
        }
        
    /// Returns the result from the query as a generic `FSharp.Core.Result`.
    let asResult (r: Result) =
        match r with
        | Success response | Found response -> Ok response
        | Unauthorized e | JsonDeserialisationError e | Unknown e -> Error <| ErrorRequestResult.fromRequestResultAndCase(e, r)
        
    /// Runs query followed by asResult.
    let queryAsResult = query >> Async.map asResult
    
