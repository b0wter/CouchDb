namespace b0wter.CouchDb.Lib

module Find =

    type TypeField
        = Null
        | Boolean
        | Number
        | String
        | Array
        | Object

    type ModArgument = {
        divisor: int
        remainder: int
    }

    type Comparator
        = Lessof of obj
        | LessOrEqual of obj
        | Equal of obj
        | NotEqual of obj
        | GreaterOrEqual of obj
        | Greater of obj
        | Exists of bool
        | Type of TypeField
        | In of obj []
        | NotIn of obj []
        | Size of int
        | Mod of ModArgument
        | Regex of System.Text.RegularExpressions.Regex

    type DefaultSort = string
    
    type SortDirection 
        = Ascending
        | Descending

    type DirectionalSort = {
        property: string
        direction: SortDirection
    }

    type Sorting
        = Default of DefaultSort
        | Directional of DirectionalSort


    // TODO: Make a class out of Selector to use inheritance to be able to cast to a base class in the custom converter! 
    //       Make an optional converter method parameter ('a -> string)

    [<AbstractClass>]
    type Selector() =
        abstract member Name: string
        abstract member TranslatedValue: string

    type TypedSelector<'a>(name, value: 'a, translator: 'a -> string) = 
        inherit Selector()
        override this.Name = name
        member this.Value = value
        member this.Translator = translator
        override this.TranslatedValue =
            sprintf "%s" (this.Value |> this.Translator)

    type TypedSubFieldSelector<'a>(name, parents: string list, value: 'a, translator: 'a -> string) =
        inherit Selector()
        override this.Name = System.String.Join(".", parents |> Seq.ofList) + "." + name
        member this.Value = value
        member this.Translator = translator
        override this.TranslatedValue =
            sprintf "%s" (this.Value |> this.Translator)

    type Expression = {
        selector: Selector
        limit: int option
        skip: int option
        sort: Sorting option
        use_index: string list option
        r: int
        bookmark: string option
        update: bool option
        stable: bool option
        execution_stats: bool option
        (* Currently not supported:
            - fields
            - stale
        *)
    }

    let createExpression (selector: Selector) =
        {
            selector = selector
            limit = None
            skip = None
            sort = None
            use_index = None
            r = 1
            bookmark = None
            update = None
            stable = None
            execution_stats = None
        }