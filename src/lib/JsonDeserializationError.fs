namespace b0wter.CouchDb.Lib

module JsonDeserializationError =
    
    /// <summary>
    /// Wraps an error reason and the corresponding json in a record.
    /// </summary>
    type T = {
        Json: string
        Reason: string
    }
    
    let create (json, reason) =
        { Json = json; Reason = reason }
    
    let asString (t: T) =
        sprintf "Error: %s%sJson:%s%s" t.Reason System.Environment.NewLine System.Environment.NewLine t.Json
