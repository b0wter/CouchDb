namespace b0wter.CouchDb.Tests.Integration.DocumentTestModels

module Default =
    open FsUnit.Xunit
    open Newtonsoft.Json
    
    type T = {
        _id: string
        _rev: string option
        ``type``: string
        myInt: int
        myFirstString: string
        mySecondString: string
        myFloat: float
        myDate: System.DateTime
    }
    
    let createWithRev (id, rev, myInt, myFirstString, mySecondString, myFloat, myDate) =
        {
            _id = id
            _rev = rev
            ``type`` = "Default"
            myInt = myInt
            myFirstString = myFirstString
            mySecondString = mySecondString
            myFloat = myFloat
            myDate = myDate
        }
        
    let create(id, myInt, myFirstString, mySecondString, myFloat, myDate) =
        createWithRev(id, None, myInt, myFirstString, mySecondString, myFloat, myDate)

    let defaultInstanceId = "c8cb91dc-1121-43de-a858-0742327ff158"
    let defaultInstance = create (defaultInstanceId, 42, "foo", "bar", 1.38064852, System.DateTime(2000, 8, 1, 12, 0, 0))
    
    let compareWithoutRev (a: T) (b: T) =
        a._id |> should equal b._id
        a.``type`` |> should equal b.``type``
        a.myInt |> should equal b.myInt
        a.myFirstString |> should equal b.myFirstString
        a.mySecondString |> should equal b.mySecondString
        a.myFloat |> should equal b.myFloat
        a.myDate |> should equal b.myDate