namespace b0wter.CouchDb.Tests.Integration.TestModels

module HierarchicalArray =
    open FsUnit.Xunit
    open System
    
    type SubField = {
        subInt: int
        subString: string
        subFloat: float
    }
    
    type T = {
        _id: System.Guid
        _rev: string option
        ``type``: string
        myString: string
        myInt: int
        myFloat: float
        mySubs: SubField list
    }
    
    let createSubField (subInt, subString, subFloat) =
        { subInt = subInt
          subString = subString
          subFloat = subFloat } 
    
    let create (id, myInt, myString, myFloat, subs: SubField list) =
        {
            _id = id
            _rev = None
            ``type`` = "Hierarchical.T"
            myString = myString
            myInt = myInt
            myFloat = myFloat
            mySubs = subs
        }
    
    let defaultInstance = create ((System.Guid.Parse("c56b6f65-8e4c-43dc-bea1-562d30eab205")), 42, "string", 3.14, [
        (-42, "substring", -3.14) |> createSubField; (-21, "substring", -90.0) |> createSubField ])
    
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
        do List.iter2 compareSubField a.mySubs b.mySubs

    

