namespace b0wter.CouchDb.Lib

module QueryParameters =
    
    [<AbstractClass>]
    type BaseQueryParameter(key: string) =
        member this.Key = key
        abstract member AsString: string with get
        
    [<AbstractClass>]
    type TypedQueryParameter<'a>(key: string, value: 'a) =
        inherit BaseQueryParameter(key)
        member this.Value = value
        
    type BoolQueryParameter(key, value: bool) =
        inherit TypedQueryParameter<bool>(key, value)
        override this.AsString = (this.Value |> string).ToLower()
        
    type TrueQueryParameter(key) =
        inherit BoolQueryParameter(key, true)
        
    type FalseQueryParameter(key) =
        inherit BoolQueryParameter(key, false)
        
    type StringQueryParameter(key, value: string) =
        inherit TypedQueryParameter<string>(key, value)
        override this.AsString = this.Value
        
    type StringListQueryParameter(key, value: string list) =
        inherit TypedQueryParameter<string list>(key, value)
        override this.AsString = System.String.Join(",", this.Value)
        
    type IntQueryParameter(key, value: int) =
        inherit TypedQueryParameter<int>(key, value)
        override this.AsString = (this.Value |> string)
