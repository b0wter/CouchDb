namespace b0wter.CouchDb.Lib

module JsonDeserializationError =
    
    /// <summary>
    /// Wraps an error reason and the corresponding json in a record.
    /// </summary>
    type JsonDeserializationError = {
        Json: string
        Reason: string
    }
    
    let create (json, reason) =
        { Json = json; Reason = reason }
    
    let asString (t: JsonDeserializationError) =
        sprintf "Error: %s%sJson:%s%s" t.Reason System.Environment.NewLine System.Environment.NewLine t.Json
