namespace b0wter.CouchDb.Lib.DesignDocuments

module DesignDocument =
    open System
    open Newtonsoft.Json
    open Newtonsoft.Json.Linq

    type FunctionType
        = Map
        | Reduce
        | Show
        | View

    let functionTypeAsString (t: FunctionType) =
        match t with
        | Map -> "map"
        | Reduce -> "red"
        | Show -> "show"
        | View -> "view"

    type View = {
        Name: string
        ``Type``: FunctionType
        Func: string
    }

    let createView name ``type`` func =
        { Name = name; ``Type`` = ``type``; Func = func }

    let parseFunc (s: string) : FunctionType option =
        match s.ToLower() with
        | "map" -> Some Map
        | "reduce" -> Some Reduce
        | "show" -> Some Show
        | "view" -> Some View
        | _ -> None

    type DesignDocument = 
        {
            [<JsonProperty("_id")>]
            Id: string
            [<JsonProperty("_rev")>]
            Rev: string option
            Views: View list
            Language: string
        }

    let createDoc id rev views language = 
        { Id = id; Rev = rev; Views = views; Language = language }

    let createDocWithIdAndRev id rev views =
        { Id = id; Rev = rev; Views = views; Language = "javascript" }

    let createDocWithId id views =
        { Id = id; Rev = None; Views = views; Language = "javascript" }

    let viewAsJson (v: View) : JProperty =
        let json = JObject()
        let property = JProperty(v.``Type`` |> functionTypeAsString, v.Func)
        do json.Add(property)
        JProperty(v.Name, json)

    let designDocumentAsJObject (d: DesignDocument) : JObject =
        let serializer = JsonSerializer.Create(b0wter.CouchDb.Lib.Json.settings())
        let json = JObject()
        let properties = d.Views |> List.map viewAsJson
        do properties |> List.iter (fun p -> json.Add(p))
        let parent = JObject.FromObject(d, serializer)
        do parent.["views"].Parent.Remove()
        do parent.Add("views", json)
        parent

    let id (d: DesignDocument) = d.Id
    
    let rev (d: DesignDocument) = d.Rev

module Converter =
    open Newtonsoft.Json
    open Newtonsoft.Json.Linq
    open System.Linq

    let jViewToView (j: JToken) : DesignDocument.View =
        let asProp = j :?> JProperty
        let name = asProp.Name
        let prop = asProp.Children<JObject>().First().Children<JProperty>().First()
        let functionType = prop.Name
        let payload = prop.Value.ToString()
        match functionType |> DesignDocument.parseFunc with
        | Some ``type`` -> DesignDocument.createView name ``type`` payload
        | None -> failwith (sprintf "Deserializing the design document failed because '%s' could not be parsed as a function type (map, show, ...)." functionType)

    type DesignDocumentConverter() =
        inherit JsonConverter()

        override this.CanRead = true

        override this.CanConvert(t) =
            typeof<DesignDocument.DesignDocument>.IsAssignableFrom(t)

        override this.ReadJson(reader, objectType, existingValue, serializer) =
            let json = JObject.Load(reader)
            let jsonViews = json.["views"]
            let views = jsonViews.Children() |> Seq.map jViewToView |> List.ofSeq
            let id = json.Value<string>("_id")
            let rev = json.Value<string>("_rev") |> Some
            let language = json.Value<string>("language")
            DesignDocument.createDoc id rev views language :> obj

        override this.WriteJson(writer, value, _) =
            let jObject = (value :?> DesignDocument.DesignDocument) |> DesignDocument.designDocumentAsJObject
            jObject.WriteTo(writer)