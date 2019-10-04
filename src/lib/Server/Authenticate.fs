namespace b0wter.CouchDb.Lib.Server

//
// Queries: /_all_dbs
//

open b0wter.CouchDb.Lib.Core
open b0wter.CouchDb.Lib
open b0wter.FSharp

module Authenticate =
    type Vendor = {
        version: string
        name: string
    }
    
    type Response = {
        couchdb: string
        uuid: string
        version: string
        vendor: Vendor    
    }
    
    type Result
        /// Successfully authenticated (200)
        = Success of Response
        /// Redirect after successful authentication (302)
        | Found of Response
        /// Username or password wasn’t recognized (401)
        | Unauthorized of RequestResult.T
        /// Deserialization of the recieved response failed.
        | JsonDeserialisationError of RequestResult.T
        /// Response could not be interpreted.
        | Unknown of RequestResult.T
        
    /// <summary>
    /// Sends an authentication request to the database.
    /// The result is stored in the default cookie container so that each subsequent
    /// request is automatically authenticated.
    /// </summary>
    let query (props: DbProperties.T) =
        async {
            let credentials = [ ("username", props.credentials.username); ("password", props.credentials.password) ] |> Seq.ofList
            let request = createFormPost props "_session" credentials 
            let! result = sendRequest request
            return match result.statusCode with
                    | Some 200 | Some 302->
                        match deserializeJson<Response> result.content with
                        | Ok r -> Success r
                        | Error e -> JsonDeserialisationError <| RequestResult.createForJson(e, result.statusCode, result.headers)
                    | Some 401 -> Unauthorized result
                    | _ -> Unknown result
        }
        
    /// Returns the result from the query as a generic `FSharp.Core.Result`.
    let asResult (r: Result) =
        match r with
        | Success response | Found response -> Ok response
        | Unauthorized e | JsonDeserialisationError e | Unknown e -> Error <| ErrorRequestResult.fromRequestResultAndCase(e, r)
        
    let queryAsResult = query >> Async.map asResult
    
