namespace b0wter.CouchDb.Lib.Databases

//
// Queries: /{db}/_index [POST]
//

open b0wter.CouchDb.Lib
open b0wter.CouchDb.Lib.Core
open b0wter.FSharp
open Newtonsoft.Json.Linq
open Newtonsoft.Json

module DeleteIndex =
    open System