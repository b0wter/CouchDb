namespace b0wter.CouchDb.Lib

module JsonDeserializationError =
    
    /// <summary>
    /// Wraps an error reason and the corresponding json in a record.
    /// </summary>
    type T = {
        json: string
        reason: string
    }
    
    let create (json, reason) =
        { json = json; reason = reason }
    

