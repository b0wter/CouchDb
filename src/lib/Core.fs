namespace b0wter.CouchDb.Lib

module Core =
    
    open System
    open FSharp.Data
    open System.Net

    type HttpPath = string

    let DefaultCookieContainer = CookieContainer()

    /// <summary>
    /// Wraps a status code and a response body (string) as a record.
    /// </summary>
    type SuccessRequestResult = {
        statusCode: int
        content: string
    }

    /// <summary>
    /// Wraps a status code and an error reason as a record.
    /// </summary>
    type ErrorRequestResult = {
        statusCode: int
        reason: string
    }

    type IRequestResult = 
        abstract member StatusCode: int

    type RequestResult =
        | SuccessResult of SuccessRequestResult
        | ErrorResult of ErrorRequestResult
        interface IRequestResult with
            member this.StatusCode = 
                match this with
                | SuccessResult s -> s.statusCode
                | ErrorResult e -> e.statusCode

    let statusCodeFromResult (r: Result<SuccessRequestResult, ErrorRequestResult>) : int =
        match r with
        | Ok s    -> s.statusCode
        | Error e -> e.statusCode

    /// <summary>
    /// Addes the path to the base url and adds a slash if necessary.
    /// </summary>
    let private combineUrls (``base``: string) (path: string) =
        match ``base``, path with
        | a, b when a.EndsWith("/") && b.StartsWith("/") -> a + b.TrimStart('/')
        | a, b when a.EndsWith("/") || b.StartsWith("/") -> a + b                     
        | a, b                                           -> a + "/" + b

    /// <summary>
    /// Creates an SuccessRequestResult.
    /// </summary>
    let successResultRequest (statusCode, content) =
        { statusCode = statusCode; content = content }

    /// <summary>
    /// Creates an ErrorRequestResult.
    /// </summary>
    let errorRequestResult (statusCode, reason) =
        { statusCode = statusCode; reason = reason }

    /// <summary>
    /// Sends a pre-made request and performs basic error handling.
    /// </summary>
    let sendRequest (p: DbProperties.T) (request: unit -> Async<HttpResponse>) : Async<Result<SuccessRequestResult, ErrorRequestResult>> =
        async {
            try 
                let! response = request ()
                let status = response.StatusCode
                do printfn "Received HTTP response with status code: %i" status

                return if status < 400 then
                        match response.Body with
                        | Binary _ -> Error (errorRequestResult (status, "Binary payloads are not supported at the moment."))
                        | Text t   -> Ok (successResultRequest (status, t))
                       else
                        match response.Body with
                        | Binary _ -> Error (errorRequestResult (status, "Binary responses are currently not supported."))
                        | Text t   -> Ok (successResultRequest (status, t))
            with
            | :? Http.HttpRequestException as ex ->
                do printfn "Encountered a HttpRequestException! %s" ex.Message
                return Error <| errorRequestResult (0, ex.Message)
            | :? WebException as ex ->
                do printfn "Encountered a WebException! %s" ex.Message
                if ex.Status = WebExceptionStatus.ProtocolError then
                    try
                        let response = ex.Response :?> HttpWebResponse
                        do printfn "WebException contained a HttpWebResponse with status code %i. Will continue evaluation." (response.StatusCode |> int)
                        let! content = b0wter.FSharp.Streams.readToEndAsync (System.Text.Encoding.UTF8) (response.GetResponseStream()) 
                        return Ok <| successResultRequest (response.StatusCode |> int, content)
                    with
                    | :? InvalidCastException ->
                        do printfn "WebException could not be cast into a HttpWebResponse."
                        return Error <| errorRequestResult (0, "Internal error with casting WebException.")
                else
                    do printfn "Exception indicates a non-protocol error (e.g. connection refused). Continue evaluation with status code 0!"
                    return Error <| errorRequestResult (0, ex.Message)
        }

    /// <summary>
    /// Wraps a status code and string contents. This may represent a success as well as an error.
    /// </summary>
    type QueryResult = {
        statusCode: int
        content: string
    }

    /// <summary>
    /// Extracts the status code and the content from a RequestResult.
    /// </summary>
    let statusCodeAndContent (result: Result<SuccessRequestResult, ErrorRequestResult>) =
        let statusCode = result |> statusCodeFromResult
        let content = match result with | Ok o -> o.content | Error e -> e.reason
        { statusCode = statusCode; content = content }

    /// <summary>
    /// Wraps an error reason and the corresponding json in a record.
    /// </summary>
    type JsonDeserialisationError = {
        json: string
        reason: string
    }

    /// <summary>
    /// Takes a string and deserialises its content.
    /// Returns an error result in case the deserialisation fails.
    /// </summary>
    let deserializeJson<'TResult> (customConverters: Newtonsoft.Json.JsonConverter list) (content: string) : Result<'TResult, JsonDeserialisationError> =
        try
            Ok <| match customConverters with
                  | [] ->
                    Newtonsoft.Json.JsonConvert.DeserializeObject<'TResult>(content, Utilities.Json.jsonSettings)
                  | converters ->
                    Newtonsoft.Json.JsonConvert.DeserializeObject<'TResult>(content, converters |> Utilities.Json.jsonSettingsWithCustomConverter)
        with
        | :? Newtonsoft.Json.JsonException as ex ->
            Error { json = content; reason = ex.Message }

    /// <summary>
    /// Takes a SuccessRequestResult and deserialises its content.
    /// Returns an error result in case the deserialisation fails.
    /// </summary>
    let deserializeJsonRequestResult<'TResult> (result: SuccessRequestResult) : Result<'TResult, JsonDeserialisationError> =
        deserializeJson [] result.content

    let analyseRequestResult (result: Result<SuccessRequestResult, ErrorRequestResult>) =
        failwith "Not implemented!"
    
    /// <summary>
    /// Creates a post request with the given form values.
    /// </summary>
    let createFormPost (p: DbProperties.T) (path: HttpPath) (formValues: seq<string * string>) =
        fun () ->
            let url = combineUrls (p |> DbProperties.baseEndpoint) path
            Http.AsyncRequest(url, body = FormValues formValues, cookieContainer = DefaultCookieContainer)

    let createCustomJsonPost (p: DbProperties.T) (path: HttpPath) (customConverter: Newtonsoft.Json.JsonConverter list) (content: obj) =
        fun () ->
            let url = combineUrls (p |> DbProperties.baseEndpoint) path
            let json = match customConverter with
                        | [] -> Newtonsoft.Json.JsonConvert.SerializeObject(content, Utilities.Json.jsonSettings)
                        | converters -> Newtonsoft.Json.JsonConvert.SerializeObject(content, converters |> Utilities.Json.jsonSettingsWithCustomConverter)
            let binary = System.Text.Encoding.UTF8.GetBytes(json)
            do printfn "Serialized object:"
            do printfn "%s" json
            Http.AsyncRequest(url, body = BinaryUpload binary, cookieContainer = DefaultCookieContainer, headers = [ FSharp.Data.HttpRequestHeaders.ContentType HttpContentTypes.Json ] )

    /// <summary>
    /// Creates a post request containing a json serialized payload.
    /// </summary>
    let createJsonPost (p: DbProperties.T) (path: HttpPath) (content: obj) =
        createCustomJsonPost p path [] content

    /// <summary>
    /// Creates a put request without a body.
    /// </summary>
    let createPut (p: DbProperties.T) (path: HttpPath) =
        fun () ->
            let url = combineUrls (p |> DbProperties.baseEndpoint) path
            Http.AsyncRequest(url, cookieContainer = DefaultCookieContainer, httpMethod = "PUT")

    let createGet (p: DbProperties.T) (path: HttpPath) =
        fun () ->
            let url = combineUrls (p |> DbProperties.baseEndpoint) path
            Http.AsyncRequest(url, cookieContainer = DefaultCookieContainer)

    /// <summary>
    /// Creates a simple HEAD request.
    /// </summary>
    let createHead (p: DbProperties.T) (path: HttpPath) =
        fun () ->
            let url = combineUrls (p |> DbProperties.baseEndpoint) path
            Http.AsyncRequest(url, cookieContainer = DefaultCookieContainer, httpMethod = "HEAD")

    /// <summary>
    /// Creates a simple DELETE request.
    /// </summary>
    let createDelete (p: DbProperties.T) (path: HttpPath) =
        fun () ->
            let url = combineUrls (p |> DbProperties.baseEndpoint) path
            Http.AsyncRequest(url, cookieContainer = DefaultCookieContainer, httpMethod = "DELETE")

    /// <summary>
    /// Sends an authentication request to the database.
    /// The result is stored in the default cookie container so that each subsequent
    /// request is automatically authenticated.
    /// </summary>
    let authenticate (p: DbProperties.T) =
        async {
            let credentials = [ ("username", p.credentials.username); ("password", p.credentials.password) ] |> Seq.ofList
            let form = FormValues credentials
            let request = createFormPost p "_session" credentials 
            return! (sendRequest p request)
        }
        
