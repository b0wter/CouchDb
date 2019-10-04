namespace b0wter.CouchDb.Lib.Server

    //
    // Queries: /_all_dbs
    //
    
    open Newtonsoft.Json
    open b0wter.CouchDb.Lib.Core
    open b0wter.CouchDb.Lib

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
            | Unauthorized
            /// Deserialization of the recieved response failed.
            | JsonDeserialisationError of JsonDeserialisationError
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
                            | Error e -> JsonDeserialisationError e
                        | Some 401 -> Unauthorized
                        | _ -> Unknown result
            }
