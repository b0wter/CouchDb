namespace b0wter.CouchDb.Lib

module Mango =

    /// <summary>
    /// Defines the possible directions to sort items: ascending and descending.
    /// </summary>    
    type SortDirection 
        = Ascending
        | Descending

    /// <summary>
    /// Defines the key to sort on and the direction to sort in.
    /// </summary>
    type DirectionalSort = {
        Property: string
        Direction: SortDirection
    }

    /// <summary>
    /// Defines the default sort order and allows the definition of a custom sort order.
    /// Note that the default order is not serialized into json as it should default to the database defaults.
    /// </summary>
    type Sorting
        = Default
        | Directional of DirectionalSort

    /// <summary>
    /// Datatypes for use in conditional operators.
    /// These types can be used for Mango queries. All other datatypes must be translated into these.
    /// They serve as predefined translations into "valid" json and make sure all dates and guids
    /// are serialised in the same way.
    /// </summary>
    type DataType
        = Bool of bool
        | Integer of int
        | Float of float
        | Text of string
        | Date of System.DateTime
        | Id of System.Guid

    /// <summary>
    /// Represents the types CouchDb knows about.
    /// Is only used in conjunction with `Condition.Type`.
    /// </summary>
    type TypeField
        = Null
        | Boolean
        | Number
        | String
        | Array
        | Object

    type Condition
        /// The field is less than the argument.
        = Less of DataType
        /// The field is less than or equal to the argument.
        | LessOrEqual of DataType
        /// The field is equal to the argument
        | Equal of DataType
        /// The field is not equal to the argument.
        | NotEqual of DataType
        /// The field is greater than or equal to the argument.
        | GreaterOrEqual of DataType
        /// The field is greater than the to the argument.
        | Greater of DataType
        /// Check whether the field exists or not, regardless of its value. The bool sets wether to check for existance or non-existance.
        | Exists of bool
        /// Check the document field’s type. See <see cref="TypeField"/> for possible field types.
        | Type of TypeField
        /// The document field must exist in the list provided.
        | In of DataType list
        /// The document field not must exist in the list provided.
        | NotIn of DataType list
        /// Special condition to match the length of an array field in a document. Non-array fields cannot match this condition.
        | Size of int
        /// Divisor and Remainder are both positive or negative integers. 
        /// Matches documents where field % Divisor == Remainder is true, and only when the document field is an integer.
        | Mod of (int * int)
        /// A regular expression pattern to match against the document field.
        /// Only matches when the field is a string value and matches the supplied regular expression.
        /// The matching algorithms are based on the Perl Compatible Regular Expression (PCRE) library.
        /// For more information about what is implemented, see the see the Erlang Regular Expression
        | Regex of System.Text.RegularExpressions.Regex

        
    /// A single condition to check. Contains the name of the key to check, the parameter to use for comparison and the operation.
    type ConditionalOperator = {
        /// Name of the property to run condition on.
        Name: string
        /// Name of the parents. This is translated into a subfield query.
        /// 
        /// See: https://docs.couchdb.org/en/stable/api/database/find.html#subfields
        Parents: string list
        /// Contains the operation and the parameter to perform the actual comoparison.
        Operation: Condition
    }
    /// Combination operators are used to combine selectors. 
    /// In addition to the common boolean operators found in most programming languages, 
    /// there are three combination operators ($all, $elemMatch, and $allMatch) that help you work with JSON arrays.
    /// A combination operator takes a single argument. The argument is either another selector, or an array of selectors.
    /// 
    /// _Source_: https://docs.couchdb.org/en/stable/api/database/find.html#combination-operators
    and CombinationOperator
        /// Matches if all the selectors in the array match.
        = And of Operator list
        /// Matches if any of the selectors in the array match. All selectors must use the same index.
        | Or of Operator list
        /// Matches if the given selector does not match.
        | Not of Operator
        /// Matches if none of the selectors in the array match.
        | Nor of Operator list
        /// Matches an array value if it contains all the elements of the argument array.
        | All of Operator list
        /// Matches and returns all documents that contain an array field with at least one element that matches all the specified query criteria.
        /// The complex version is used to match properties on subdocuments (don't use this on something like `[ 123, 456, 789]`).
        /// The second parameter is the name of the field that the element match query will be run on.
        | ElementMatch of (Operator * string)
        /// Matches and returns all documents that contain an array field with all its elements matching all the specified query criteria.
        /// If you want to match a subfield in an object in the array you need to supply a non-empty string for the enclosed operator.
        /// If you dont supply a name the complete name element will be dropped and allow you to match an array of simple data types (e.g. int).
        /// The second parameter is the name of the field that the element match query will be run on.
        | AllMatch of (Operator * string)
        
    /// <summary>
    /// An operator represents a single operation that is performed. This may be a `Combinator` that contains multiple `Contidionals`.
    /// </summary>
    and Operator
        = Conditional of ConditionalOperator
        | Combinator of CombinationOperator
    /// <summary>
    /// Contains all the information required for a query. Contains a single `Operator`.
    /// If you need multiple operators use a `CombinationOperator`.
    /// </summary>
    and Expression = {
        Selector: Operator
        Limit: int option
        Skip: int option
        Sort: Sorting option
        [<Newtonsoft.Json.JsonProperty("use_index")>]
        UseIndex: string list option
        R: int
        Bookmark: string option
        Update: bool option
        Stable: bool option
        [<Newtonsoft.Json.JsonProperty("execution_stats")>]
        ExecutionStats: bool option
        (* Currently not supported:
            - fields
            - stale
        *)
    }

    /// Creates an <see cref="Expression"/> with default settings for all fields but the selector.
    let createExpression (operator: Operator) =
        {
            Selector = operator
            Limit = None
            Skip = None
            Sort = None
            UseIndex = None
            R = 1
            Bookmark = None
            Update = None
            Stable = None
            ExecutionStats = None
        }

    let createExpressionWithLimit (limit: int) (operator: Operator) =
        { Selector = operator; Limit = Some limit; Skip = None; Sort = None; UseIndex = None; 
          R = 1; Bookmark = None ; Update = None; Stable = None; ExecutionStats = None }
        
    let conditionWithParents parents field operation =
        Conditional {
            ConditionalOperator.Name = field;
            ConditionalOperator.Parents = parents;
            ConditionalOperator.Operation = operation
        }
        
    let condition =
        conditionWithParents []
        
    let combination (combinator: CombinationOperator) =
        Operator.Combinator combinator
        
    let ``or`` (a: Operator) (b: Operator) =
        Combinator <| Or [a; b]
        
    let ``and`` (a: Operator) (b: Operator) =
        Combinator <| And [a; b]

    let nor (a: Operator) (b: Operator) =
        Combinator <| Nor [a; b]
        
    let all ([<System.ParamArray>] operators: Operator array) =
        Combinator (All <| (operators |> List.ofArray))
