namespace b0wter.CouchDb.Lib

module Selector =

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

    type SimpleSelector
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