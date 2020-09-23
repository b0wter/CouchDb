namespace b0wter.CouchDb.Tests.Integration.DocumentTestModels

module HierarchicalSimpel =
    open FsUnit.Xunit   
    
    type T = {
        _id: string
        _rev: string option
        ``type``: string
        myString: string
        myInt: int
        myFloat: float
        mySubs: int list
    }
    
    let create (id, myInt, myString, myFloat, subs: int list) =
        {
            _id = id
            _rev = None
            ``type`` = "Hierarchical.T"
            myString = myString
            myInt = myInt
            myFloat = myFloat
            mySubs = subs
        }
    
    let defaultInstance = create ("c56b6f65-8e4c-43dc-bea1-562d30eab205", 42, "string", 3.14, [1; 2; 3; 4])
    
    let compareWithoutRev (a: T) (b: T) =
        a._id |> should equal b._id
        a.``type`` |> should equal b.``type``
        a.myInt |> should equal b.myInt
        a.myString |> should equal b.myString
        a.myFloat |> should equal b.myFloat
        do a.mySubs |> should equal b.mySubs

