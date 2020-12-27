namespace b0wter.CouchDb.Lib
open System.Text
open QueryParameters

module Core =
    
    open System
    open System.Net
    open System.Net.Http
    open Utilities

    type HttpPath = string
    
    let DefaultCookieContainer = CookieContainer()
    let DefaultHandler = new HttpClientHandler(CookieContainer=DefaultCookieContainer)
    let DefaultClient = new HttpClient(DefaultHandler)
    
    
    /// <summary>
    /// Adds the path to the base url and adds a slash if necessary.
    /// </summary>
    let private combineUrls (``base``: string) (path: string) =
        match ``base``, path with
        | a, b when a.EndsWith("/") && b.StartsWith("/") -> a + b.TrimStart('/')
        | a, b when a.EndsWith("/") || b.StartsWith("/") -> a + b                     
        | a, b                                           -> a + "/" + b

    /// <summary>
    /// Sends a pre-made request and performs basic error handling.
    /// </summary>
    let sendTextRequest (request: Async<HttpResponseMessage>) : Async<RequestResult.StringRequestResult> =
        async {
            try 
                let! response = request
                let status = response.StatusCode |> int
                let! content = try response.Content.ReadAsStringAsync() |> Async.AwaitTask with ex -> async { return sprintf "Reading the body threw an exception: %s" ex.Message }
                let headers = response.Headers |> Seq.map (fun x -> (x.Key, String.Join(",", x.Value))) |> Map.ofSeq
                return {
                    StatusCode = Some status
                    Content = content
                    Headers = headers
                }
            with
            | ex ->
                let message = ex |> Exception.foldMessages
                return RequestResult.createText(None, message)
        }
        
    /// <summary>
    /// Sends a pre-made request and performs basic error handling.
    /// </summary>
    let sendBinaryRequest (request: Async<HttpResponseMessage>) : Async<RequestResult.BinaryRequestResult> =
        async {
            try 
                let! response = request
                let status = response.StatusCode |> int
                let! content = try response.Content.ReadAsByteArrayAsync() |> Async.AwaitTask with ex -> async { return (sprintf "Reading the body threw an exception: %s" ex.Message) |> Encoding.UTF8.GetBytes }
                let headers = response.Headers |> Seq.map (fun x -> (x.Key, String.Join(",", x.Value))) |> Map.ofSeq
                return {
                    StatusCode = Some status
                    Content = content
                    Headers = headers
                }
            with
            | ex ->
                let message = ex |> Exception.foldMessages |> Encoding.UTF8.GetBytes 
                return RequestResult.createBinary(None, message)
        }

    /// <summary>
    /// Wraps a status code and string contents. This may represent a success as well as an error.
    /// </summary>
    type QueryResult = {
        StatusCode: int option
        Content: string
    }

    /// <summary>
    /// Takes a string and deserialises its content.
    /// Returns an error result in case the deserialisation fails.
    /// </summary>
    let deserializeJsonWith<'TResult> (customConverters: Newtonsoft.Json.JsonConverter list) (content: string) : Result<'TResult, JsonDeserializationError.JsonDeserializationError> =
        try
            let result = Ok <| match customConverters with
                               | [] ->
                                 Newtonsoft.Json.JsonConvert.DeserializeObject<'TResult>(content, Json.settings ())
                               | converters ->
                                 Newtonsoft.Json.JsonConvert.DeserializeObject<'TResult>(content, converters |> Json.settingsWithCustomConverter)
            result
        with
        | :? Newtonsoft.Json.JsonException as ex ->
            Error { Json = content; Reason = ex.Message }

    /// <summary>
    /// Takes a SuccessRequestResult and deserialises its content.
    /// Returns an error result in case the deserialisation fails.
    /// </summary>
    let deserializeJson<'TResult> (content: string) : Result<'TResult, JsonDeserializationError.JsonDeserializationError> =
        deserializeJsonWith<'TResult> [] content

    /// <summary>
    /// Stores the query parameters. ToString() will be called on all values.
    /// </summary>
    type QueryParameters = BaseQueryParameter list
    
    /// <summary>
    /// Maps the list of query parameters (whose values are of type object) to strings by calling to string on them.
    /// </summary>
    let private formatQueryParameters (parameters: QueryParameters) =
        parameters |> List.map (fun x -> (x.Key, x.AsString))
                   |> List.fold (fun acc (key, value) -> sprintf "%s%s=%s" (if String.IsNullOrWhiteSpace(acc) then "?" else "&") key value) ""
                   |> Uri.EscapeUriString
    
    /// Serializes an object.
    /// Allows the user to define additional `JsonConverter`.
    let serializeAsJson (customConverters: Newtonsoft.Json.JsonConverter list) (content: 'a) =
        match customConverters with
        | [] ->         Newtonsoft.Json.JsonConvert.SerializeObject(content, Json.settings ())
        | converters -> Newtonsoft.Json.JsonConvert.SerializeObject(content, converters |> Json.settingsWithCustomConverter)
        |> Json.postProcessing

    /// Creates a POST request with a form encoded payload. Allows the usage of additional `JsonConverter`s.
    let createCustomFormPost (p: DbProperties.DbProperties) (path: HttpPath) (customConverters: Newtonsoft.Json.JsonConverter list) (formData: Map<string, obj>) (queryParameters: QueryParameters) =
        let data = formData |> Map.toSeq |> Seq.map (fun (key, value) -> Collections.Generic.KeyValuePair<string, string>(key, value |> string))
        let queryParameters = queryParameters |> formatQueryParameters 
        let url = combineUrls (p |> DbProperties.baseEndpoint) path + queryParameters
        let request = new HttpRequestMessage(HttpMethod.Post, url)
        do request.Content <- new FormUrlEncodedContent(data)
        DefaultClient.SendAsync(request) |> Async.AwaitTask

    /// Creates a POST request with a form encoded payload.
    let createFormPost (p: DbProperties.DbProperties) (path: HttpPath) (formData: Map<string, obj>) (queryParameters: QueryParameters) =
        createCustomFormPost p path [] formData queryParameters

    /// Creates a POST request containing a json serialized payload. Allows to define additional `JsonConverter`.
    let createCustomJsonPost (p: DbProperties.DbProperties) (path: HttpPath) (customConverters: Newtonsoft.Json.JsonConverter list) (content: 'a) (queryParameters: QueryParameters) =
        let queryParameters = queryParameters |> formatQueryParameters 
        let url = combineUrls (p |> DbProperties.baseEndpoint) path + queryParameters
        let serialized = serializeAsJson customConverters content
        let request = new HttpRequestMessage(HttpMethod.Post, url)
        do request.Content <- new StringContent(serialized, Text.Encoding.UTF8, "application/json")
        DefaultClient.SendAsync(request) |> Async.AwaitTask

    /// <summary>
    /// Creates a POST request containing a json serialized payload.
    /// </summary>
    let createJsonPost (p: DbProperties.DbProperties) (path: HttpPath) (content: obj) (queryParameters: QueryParameters) =
        createCustomJsonPost p path [] content queryParameters
        
    /// <summary>
    /// Creates a POST request containing a binary payload.
    /// </summary>
    let createBinaryPut (p: DbProperties.DbProperties) (path: HttpPath) (content: byte array) (queryParameters: QueryParameters) =
        let queryParameters = queryParameters |> formatQueryParameters 
        let url = combineUrls (p |> DbProperties.baseEndpoint) path + queryParameters
        let request = new HttpRequestMessage(HttpMethod.Put, url)
        do request.Content <- new ByteArrayContent(content)
        DefaultClient.SendAsync(request) |> Async.AwaitTask

    /// <summary>
    /// Creates a COPY request without a body (this is a custom HTTP method defined by CouchDb).
    /// </summary>
    let createCopy (p: DbProperties.DbProperties) (path: HttpPath) (queryParameters: QueryParameters) (headers: (string * string) list) =
        let queryParameters = queryParameters |> formatQueryParameters
        let url = (combineUrls (p |> DbProperties.baseEndpoint) path) + queryParameters
        let method = HttpMethod("COPY")
        let request = new HttpRequestMessage(method, url)
        do headers |> List.iter (fun (key, value) -> request.Headers.Add(key, value)) 
        DefaultClient.SendAsync(request) |> Async.AwaitTask

    /// Creates a PUT request without a body.
    let createPut (p: DbProperties.DbProperties) (path: HttpPath) (queryParameters: QueryParameters) =
        let queryParameters = queryParameters |> formatQueryParameters
        let url = (combineUrls (p |> DbProperties.baseEndpoint) path) + queryParameters
        let request = new HttpRequestMessage(HttpMethod.Put, url)
        DefaultClient.SendAsync(request) |> Async.AwaitTask

    /// Creates a PUT request with a json payload.
    let createCustomJsonPut (p: DbProperties.DbProperties) (path: HttpPath) (customConverters: Newtonsoft.Json.JsonConverter list) (content: 'a) (queryParameters: QueryParameters) =
        let queryParameters = queryParameters |> formatQueryParameters
        let url = combineUrls (p |> DbProperties.baseEndpoint) path + queryParameters
        let json = serializeAsJson customConverters content
        let request = new HttpRequestMessage(HttpMethod.Put, url)
        do request.Content <- new StringContent(json, Text.Encoding.UTF8, "application/json")
        DefaultClient.SendAsync(request) |> Async.AwaitTask
        // Binary body?

    /// Creates a simple PUT request without a body.
    let createJsonPut (p: DbProperties.DbProperties) (path: HttpPath) (content: obj) (queryParameters: QueryParameters) =
        createCustomJsonPut p path [] content queryParameters

    /// Creates a simple GET request.
    let createGet (p: DbProperties.DbProperties) (path: HttpPath) (queryParameters: QueryParameters) =
        let queryParameters = queryParameters |> formatQueryParameters
        let url = combineUrls (p |> DbProperties.baseEndpoint) path + queryParameters
        let request = new HttpRequestMessage(HttpMethod.Get, url)
        DefaultClient.SendAsync(request) |> Async.AwaitTask

    /// Creates a simple HEAD request.
    let createHead (p: DbProperties.DbProperties) (path: HttpPath) (queryParameters: QueryParameters) =
        let queryParameters = queryParameters |> formatQueryParameters
        let url = combineUrls (p |> DbProperties.baseEndpoint) path + queryParameters
        let request = new HttpRequestMessage(HttpMethod.Head, url)
        DefaultClient.SendAsync(request) |> Async.AwaitTask

    /// <summary>
    /// Creates a simple DELETE request.
    /// </summary>
    let createDelete (p: DbProperties.DbProperties) (path: HttpPath) (queryParameters: QueryParameters) =
        let queryParameters = queryParameters |> formatQueryParameters
        let url = combineUrls (p |> DbProperties.baseEndpoint) path + queryParameters
        let request = new HttpRequestMessage(HttpMethod.Delete, url)
        DefaultClient.SendAsync(request) |> Async.AwaitTask

