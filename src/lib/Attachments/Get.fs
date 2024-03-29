namespace b0wter.CouchDb.Lib.Attachments

module GetText =
    open b0wter.CouchDb.Lib
    open Utilities

    type Result<'a> = HttpVerbs.Get.Result<'a>

    type Response<'a> = HttpVerbs.Get.Response<'a>

    let queryWith<'a> dbProps dbName docId attachmentName queryParameters customConverters =
        async {
            if System.String.IsNullOrWhiteSpace(dbName) then
                return Result<'a>.DbNameMissing <| RequestResult.createText (None, "The database name is empty. The query has not been sent to the server.")
            else
                let url = (sprintf "%s/%s/%s" dbName (id |> string)) attachmentName
                return! HttpVerbs.Get.query<'a> dbProps url docId queryParameters customConverters
        }

    let query<'a> dbProps dbName id queryParameters = queryWith<'a> dbProps dbName id queryParameters []

    let queryAsResultWith dbProps dbName docId attachmentName queryParameters customConverters =
        queryWith dbProps dbName docId attachmentName queryParameters customConverters |> Async.map HttpVerbs.Get.asResult
    
    let queryAsResult dbProps dbName docId attachmentName queryParameters =
        query dbProps dbName attachmentName docId queryParameters |> Async.map HttpVerbs.Get.asResult

    let asResult = HttpVerbs.Get.asResult
    
    
module GetBinary =
    open b0wter.CouchDb.Lib
    
    type Response = byte []
    
    type Result
        = Success of Response
        | NotFound of RequestResult.BinaryRequestResult
        | Unauthorized of RequestResult.BinaryRequestResult
        | Unknown of RequestResult.BinaryRequestResult
        | DocumentIdMissing of RequestResult.BinaryRequestResult
        | AttachmentNameMissing of RequestResult.BinaryRequestResult
    
    let queryWith dbProps dbName docId attachmentName queryParameters =
        async {
            if docId |> Utilities.String.isNullOrWhiteSpace then return DocumentIdMissing (RequestResult.createBinary (None, [||]))
            else if attachmentName |> Utilities.String.isNullOrWhiteSpace then return AttachmentNameMissing (RequestResult.createBinary (None, [||]))
            else
                let url = (sprintf "%s/%s/%s" dbName (docId |> string)) attachmentName
                let request = Core.createGet dbProps url queryParameters
                let! result = Core.sendBinaryRequest request
                return match result.StatusCode with
                       | Some 200 | Some 304 ->
                           Success result.Content
                       | Some 404 ->
                           NotFound result
                       | Some 401 ->
                           Unauthorized result
                       | _ ->
                           Unknown result
        }
        
    let query dbProps dbName docId attachmentName = queryWith dbProps dbName docId attachmentName []
        
    let asResult (r: Result) : FSharp.Core.Result<Response, ErrorRequestResult.BinaryErrorRequestResult> =
        match r with
        | Success s -> Ok s
        | NotFound e | AttachmentNameMissing e | DocumentIdMissing e | Unauthorized e
        | Unknown e -> Error <| ErrorRequestResult.fromBinaryRequestResultAndCase(e, r)
        
    let queryWithAsResult dbProps dbName docId attachmentName queryParameters =
        queryWith dbProps dbName docId attachmentName queryParameters |> Utilities.Async.map asResult
    
    let queryAsResult dbProps dbName docId attachmentName = query dbProps dbName docId attachmentName |> Utilities.Async.map asResult