namespace b0wter.CouchDb.Lib

module Exception =
    open b0wter.FSharp

    /// Creates a string of the format:
    /// [$ExceptionType] $ExceptionMessage
    let formatForLog (ex: System.Exception) =
        sprintf "[%s] %s" (ex.GetType().Name) ex.Message

    /// Aggregates the messages of the exception and all inner exceptions.
    let foldMessages (ex: System.Exception) =
        let rec run acc (e: System.Exception) =
            match e.InnerException with
            | null -> (e |> formatForLog) :: acc
            | _ -> run ((e |> formatForLog) :: acc) e.InnerException
        ex |> run [] |> List.rev |> String.join System.Environment.NewLine