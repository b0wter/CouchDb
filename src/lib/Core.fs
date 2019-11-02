namespace b0wter.CouchDb.Lib
open QueryParameters

module Core =
    
    open System
    open FSharp.Data
    open System.Net
    open Utilities

    type HttpPath = string
    
    let DefaultCookieContainer = CookieContainer()
    
    
    /// <summary>
    /// Addes the path to the base url and adds a slash if necessary.
    /// </summary>
    let private combineUrls (``base``: string) (path: string) =
        match ``base``, path with
        | a, b when a.EndsWith("/") && b.StartsWith("/") -> a + b.TrimStart('/')
        | a, b when a.EndsWith("/") || b.StartsWith("/") -> a + b                     
        | a, b                                           -> a + "/" + b

    /// <summary>
    /// Sends a pre-made request and performs basic error handling.
    /// </summary>
    let sendRequest (request: unit -> Async<HttpResponse>) : Async<RequestResult.T> =
        // TODO: Maybe add the `silentHttpErrors` flag to all outgoing request. This will make it so that error status codes do not generate exceptions.
        async {
            try 
                let! response = request ()
                let status = response.StatusCode
                do printfn "Received HTTP response with status code: %i" status
                return match response.Body with
                       | Binary _ ->
                           {
                               statusCode = Some status
                               content = "Binary payloads are not supported at the moment."
                               headers = response.Headers
                           }
                       | Text t ->
                           {
                               statusCode = Some status
                               content = t
                               headers = response.Headers
                           }
            with
            | :? System.NullReferenceException as ex ->
                do printfn "Encountered a NullReferenceException! %s" ex.Message
                return RequestResult.create(None, ex.Message)
            | :? Http.HttpRequestException as ex ->
                do printfn "Encountered a HttpRequestException! %s" ex.Message
                return RequestResult.create(None, ex.Message)
            | :? WebException as ex ->
                do printfn "Encountered a WebException! %s" ex.Message
                if ex.Status = WebExceptionStatus.ProtocolError then
                    try
                        let response = ex.Response :?> HttpWebResponse
                        let headers = seq { for i in [0..response.Headers.Count - 1] do yield (response.Headers.Keys.[i], response.Headers.Get(i)) } |> Map.ofSeq
                        do printfn "WebException contained a HttpWebResponse with status code %i. Will continue evaluation." (response.StatusCode |> int)
                        let! content = b0wter.FSharp.Streams.readToEndAsync (System.Text.Encoding.UTF8) (response.GetResponseStream()) 
                        return RequestResult.createWithHeaders (response.StatusCode |> int |> Some, content, headers)
                    with
                    | :? InvalidCastException as e ->
                        do printfn "WebException could not be cast into a HttpWebResponse. Details: %s | Parent: %s" e.Message ex.Message
                        return RequestResult.create(None, sprintf "Internal error with casting WebException. Details: %s | Parent: %s" e.Message ex.Message)
                else
                    do printfn "Exception indicates a non-protocol error (e.g. connection refused). Continue evaluation with status code 0!"
                    return RequestResult.create (None, ex.Message)
        }

    /// <summary>
    /// Wraps a status code and string contents. This may represent a success as well as an error.
    /// </summary>
    type QueryResult = {
        statusCode: int option
        content: string
    }

    /// <summary>
    /// Takes a string and deserialises its content.
    /// Returns an error result in case the deserialisation fails.
    /// </summary>
    let deserializeJsonWith<'TResult> (customConverters: Newtonsoft.Json.JsonConverter list) (content: string) : Result<'TResult, JsonDeserializationError.T> =
        do printfn "Deserializing to type: %s" typeof<'TResult>.FullName
        try
            let result = Ok <| match customConverters with
                               | [] ->
                                 Newtonsoft.Json.JsonConvert.DeserializeObject<'TResult>(content, Json.settings ())
                               | converters ->
                                 Newtonsoft.Json.JsonConvert.DeserializeObject<'TResult>(content, converters |> Json.settingsWithCustomConverter)
            do printfn "Deserialization successful."
            result
        with
        | :? Newtonsoft.Json.JsonException as ex ->
            do printfn "Deserialization failed."
            Error { json = content; reason = ex.Message }

    /// <summary>
    /// Takes a SuccessRequestResult and deserialises its content.
    /// Returns an error result in case the deserialisation fails.
    /// </summary>
    let deserializeJson<'TResult> (content: string) : Result<'TResult, JsonDeserializationError.T> =
        deserializeJsonWith<'TResult> [] content

    /// <summary>
    /// Stores the query parameters. ToString() will be called on all values.
    /// </summary>
    type QueryParameters = BaseQueryParameter list
    
    /// <summary>
    /// Maps the list of query parameters (whose values are of type object) to strings by calling to string on them.
    /// </summary>
    let private formatQueryParameters (parameters: QueryParameters) : (string * string) list =
        parameters |> List.map (fun x -> (x.Key, x.AsString))
    
    /// Serializes an object and returns a string representation as well as a binary (UTF8) representation.
    /// Allows the user to define additional `JsonConverter`.
    let private serializeAsBinaryJson (customConverters: Newtonsoft.Json.JsonConverter list) (content: obj) =
        let json = match customConverters with
                    | [] ->         Newtonsoft.Json.JsonConvert.SerializeObject(content, Json.settings ())
                    | converters -> Newtonsoft.Json.JsonConvert.SerializeObject(content, converters |> Json.settingsWithCustomConverter)
                    |> Json.postProcessing
        (json, System.Text.Encoding.UTF8.GetBytes(json))

    /// <summary>
    /// Creates a post request with the given form values.
    /// </summary>
    let createFormPost (p: DbProperties.T) (path: HttpPath) (formValues: seq<string * string>) =
        fun () ->
            let url = combineUrls (p |> DbProperties.baseEndpoint) path
            Http.AsyncRequest(url, body = FormValues formValues, cookieContainer = DefaultCookieContainer, silentHttpErrors = true)

    /// Creates a POST request containing a json serialized payload. Allows to define additional `JsonConverter`.
    let createCustomJsonPost (p: DbProperties.T) (path: HttpPath) (customConverters: Newtonsoft.Json.JsonConverter list) (content: obj) (queryParameters: QueryParameters) =
        fun () ->
            let queryParamters = queryParameters |> formatQueryParameters
            let url = combineUrls (p |> DbProperties.baseEndpoint) path
            let json, binary = serializeAsBinaryJson customConverters content
            do printfn "Serialized object:"
            do printfn "%s" json
            Http.AsyncRequest(url, 
                              httpMethod = "POST",
                              body = BinaryUpload binary, 
                              cookieContainer = DefaultCookieContainer, 
                              headers = [ HttpRequestHeaders.ContentType HttpContentTypes.Json ],
                              query = queryParamters,
                              silentHttpErrors = true
                            )

    /// <summary>
    /// Creates a POST request containing a json serialized payload.
    /// </summary>
    let createJsonPost (p: DbProperties.T) (path: HttpPath) (content: obj) (queryParameters: QueryParameters) =
        createCustomJsonPost p path [] content queryParameters
        
    /// <summary>
    /// Creates a COPY request without a body (this is a custom HTTP method defined by CouchDb).
    /// </summary>
    let createCopy (p: DbProperties.T) (path: HttpPath) (queryParameters: QueryParameters) (headers: (string * string) list) =
        fun () ->
            let queryParamters = queryParameters |> formatQueryParameters
            let url = combineUrls (p |> DbProperties.baseEndpoint) path
            Http.AsyncRequest(url, cookieContainer = DefaultCookieContainer, httpMethod = "COPY", query = queryParamters, silentHttpErrors = true, headers = headers)

    /// <summary>
    /// Creates a PUT request without a body.
    /// </summary>
    let createPut (p: DbProperties.T) (path: HttpPath) (queryParameters: QueryParameters) =
        fun () ->
            let queryParamters = queryParameters |> formatQueryParameters
            let url = combineUrls (p |> DbProperties.baseEndpoint) path
            Http.AsyncRequest(url, cookieContainer = DefaultCookieContainer, httpMethod = "PUT", query = queryParamters, silentHttpErrors = true)

    /// Creates a PUT request with a json payload.
    let createCustomJsonPut (p: DbProperties.T) (path: HttpPath) (customConverters: Newtonsoft.Json.JsonConverter list) (content: obj) (queryParameters: QueryParameters) =
        fun () ->
            let queryParameters = queryParameters |> formatQueryParameters
            let url = combineUrls (p |> DbProperties.baseEndpoint) path
            let json, binary = serializeAsBinaryJson customConverters content
            do printfn "Serialized object:"
            do printfn "%s" json
            Http.AsyncRequest(url,
                              httpMethod = "PUT",
                              body = BinaryUpload binary,
                              cookieContainer = DefaultCookieContainer,
                              headers = [ HttpRequestHeaders.ContentType HttpContentTypes.Json ],
                              query = queryParameters,
                              silentHttpErrors = true
                            )

    /// Creates a simple PUT request without a body.
    let createJsonPut (p: DbProperties.T) (path: HttpPath) (content: obj) (queryParameters: QueryParameters) =
        createCustomJsonPut p path [] content queryParameters

    /// Creates a simple GET request.
    let createGet (p: DbProperties.T) (path: HttpPath) (queryParameters: QueryParameters) =
        fun () ->
            let queryParameters = queryParameters |> formatQueryParameters
            let url = combineUrls (p |> DbProperties.baseEndpoint) path
            Http.AsyncRequest(url, httpMethod = "GET", cookieContainer = DefaultCookieContainer, query = queryParameters, silentHttpErrors = true)

    /// Creates a simple HEAD request.
    let createHead (p: DbProperties.T) (path: HttpPath) (queryParameters: QueryParameters) =
        fun () ->
            let queryParameters = queryParameters |> formatQueryParameters
            let url = combineUrls (p |> DbProperties.baseEndpoint) path
            Http.AsyncRequest(url, cookieContainer = DefaultCookieContainer, httpMethod = "HEAD", query = queryParameters, silentHttpErrors = true)

    /// <summary>
    /// Creates a simple DELETE request.
    /// </summary>
    let createDelete (p: DbProperties.T) (path: HttpPath) (queryParameters: QueryParameters) =
        fun () ->
            let queryParameters = queryParameters |> formatQueryParameters
            let url = combineUrls (p |> DbProperties.baseEndpoint) path
            Http.AsyncRequest(url, cookieContainer = DefaultCookieContainer, httpMethod = "DELETE", query = queryParameters, silentHttpErrors = true)

