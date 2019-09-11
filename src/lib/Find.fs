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

    /// <summary>
    /// Combination operators are used to combine selectors. 
    /// In addition to the common boolean operators found in most programming languages, 
    /// there are three combination operators ($all, $elemMatch, and $allMatch) that help you work with JSON arrays.
    /// A combination operator takes a single argument. The argument is either another selector, or an array of selectors.
    /// 
    /// _Source_: https://docs.couchdb.org/en/stable/api/database/find.html#combination-operators
    /// </summary>
    type CombinationOperator<'a>
        /// <summary>
        /// Matches if all the selectors in the array match.
        /// </summary>
        = And of 'a list
        /// <summary>
        /// Matches if any of the selectors in the array match. All selectors must use the same index.
        /// </summary>
        | Or of 'a list
        /// <summary>
        /// Matches if the given selector does not match.
        /// <summary>
        | Not of 'a
        /// <summary>
        /// Matches if none of the selectors in the array match.
        /// </summary>
        | Nor of 'a list
        /// <summary>
        /// Matches an array value if it contains all the elements of the argument array.
        /// </summary>
        | All of 'a list
        /// <summary>
        /// Matches and returns all documents that contain an array field with at least one element that matches all the specified query criteria.
        /// </summary>
        | ElementMatch of 'a
        /// <summary>
        /// Matches and returns all documents that contain an array field with all its elements matching all the specified query criteria.
        /// </summary>
        | AllMatch of 'a

    /// <summary>
    /// Abstraction for an argument. Since conditional operators allow different types of arguments:
    /// int, float, string, dates, ...
    /// they cannot be put into a single list (that would require all operators to work on the same argument type).
    /// Since we require json in the end all boils down to a serialized string.
    /// </summary>
    type IConditionArgument = interface end

    /// <summary>
    /// Condition operators are specific to a field, and are used to evaluate the value stored in that field.
    /// For instance, the basic $eq operator matches when the specified field contains a value that is equal to the supplied argument.
    /// The basic equality and inequality operators common to most programming languages are supported. 
    /// In addition, some ‘meta’ condition operators are available. 
    /// Some condition operators accept any valid JSON content as the argument. 
    /// Other condition operators require the argument to be in a specific JSON format.
    /// 
    /// _Source_: https://docs.couchdb.org/en/stable/api/database/find.html#operators
    /// </summary>
    type ConditionOperator
        /// <summary>
        /// The field is less than the argument.
        /// </summary>
        = Less of IConditionArgument
        /// <summary>
        /// The field is less than or equal to the argument.
        /// </summary>
        | LessOrEqual of IConditionArgument
        /// <summary>
        /// The field is equal to the argument
        /// </summary>
        | Equal of IConditionArgument
        /// <summary>
        /// The field is not equal to the argument.
        /// </summary>
        | NotEqual of IConditionArgument
        /// <summary>
        /// The field is greater than or equal to the argument.
        /// </summary>
        | GreaterOrEqual of IConditionArgument
        /// <summary>
        /// The field is greater than the to the argument.
        /// </summary>
        | Greater of IConditionArgument
        /// <summary>
        /// Check whether the field exists or not, regardless of its value.
        /// </summary>
        | Exists of bool
        /// <summary>
        /// Check the document field’s type. See <see cref="TypeField"/> for possible field types.
        /// </summary>
        | Type of TypeField
        /// <summary>
        /// The document field must exist in the list provided.
        /// </summary>
        | In of IConditionArgument []
        /// <summary>
        /// The document field not must exist in the list provided.
        /// </summary>
        | NotIn of IConditionArgument []
        /// <summary>
        /// Special condition to match the length of an array field in a document. Non-array fields cannot match this condition.
        /// </summary>
        | Size of int
        /// <summary>
        /// Divisor and Remainder are both positive or negative integers. Non-integer values result in a 404. 
        /// Matches documents where field % Divisor == Remainder is true, and only when the document field is an integer.
        /// </summary>
        | Mod of ModArgument
        /// <summary>
        /// A regular expression pattern to match against the document field.
        /// Only matches when the field is a string value and matches the supplied regular expression.
        /// The matching algorithms are based on the Perl Compatible Regular Expression (PCRE) library.
        /// For more information about what is implemented, see the see the Erlang Regular Expression
        /// </summary>
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


    /// <summary>
    /// Base class for all selectors.
    /// </summary>
    [<AbstractClass>]
    type BaseSelector () = class end

    /// <summary>
    /// Base class for selectors based on a single field.
    /// </summary>
    [<AbstractClass>]
    type Selector() =
        inherit BaseSelector()
        abstract member Name: string
        abstract member TranslatedValue: string

    /// <summary>
    /// A simple selector that checks for simple equality.
    /// </summary>
    type TypedSelector<'a>(name, value: 'a, translator: 'a -> string) = 
        inherit Selector()
        override this.Name = name
        member this.Value = value
        member this.Translator = translator
        override this.TranslatedValue =
            sprintf "%s" (this.Value |> this.Translator)

    /// <summary>
    /// A selector that acts on subfields. This allows you to query for child properties.
    /// The resulting selector is built in this fashion:
    /// $parents.[0] . $parents.[1] . [...] . $parents.[n] . $name
    /// </summary>
    type TypedSubFieldSelector<'a>(name, parents: string list, value: 'a, translator: 'a -> string) =
        inherit TypedSelector<'a>(name, value, translator)
        override this.Name = System.String.Join(".", parents |> Seq.ofList) + "." + name
        override this.TranslatedValue =
            sprintf "%s" (this.Value |> this.Translator)

    /// <summary>
    /// A selector that contains multiple other selectors.
    /// </summary>
    type MultiSelector(selectors: Selector list) =
        inherit BaseSelector ()
        member this.Selectors = selectors

    /// <summary>
    /// Contains all the information required for a query. Contains a single selector.
    /// If you need multiple selectors use a <see cref="MultiSelector"/>.
    /// </summary>
    type Expression = {
        selector: BaseSelector
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

    type Combination = {
        combinators: Combination list
        conditionals: ConditionOperator list
    }


    /// <summary>
    /// Creates an <see cref="Expression"> with default settings for all fields but the selector.
    /// </summary>
    let createExpression (selector: BaseSelector) =
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