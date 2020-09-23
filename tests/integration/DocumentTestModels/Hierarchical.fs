namespace b0wter.CouchDb.Tests.Integration.DocumentTestModels

module Hierarchical =
    open FsUnit.Xunit
    
    type SubField = {
        subInt: int
        subString: string
        subFloat: float
        
    }
    
    type T = {
        _id: string
        _rev: string option
        ``type``: string
        myString: string
        myInt: int
        myFloat: float
        mySub: SubField
    }
    
    let create (id, myInt, myString, myFloat, subString, subInt, subFloat) =
        {
            _id = id
            _rev = None
            ``type`` = "Hierarchical.T"
            myString = myString
            myInt = myInt
            myFloat = myFloat
            mySub = {
                subString = subString
                subInt = subInt
                subFloat = subFloat
            }
        }
    
    let defaultInstance = create ("c56b6f65-8e4c-43dc-bea1-562d30eab205", 42, "string", 3.14, "substring", -42, -3.14)
    
    let compareSubField (a: SubField) (b: SubField) =
        a.subFloat |> should equal b.subFloat
        a.subInt |> should equal b.subInt
        a.subString |> should equal b.subString
    
    let compareWithoutRev (a: T) (b: T) =
        a._id |> should equal b._id
        a.``type`` |> should equal b.``type``
        a.myInt |> should equal b.myInt
        a.myString |> should equal b.myString
        a.myFloat |> should equal b.myFloat
        do compareSubField a.mySub b.mySub

