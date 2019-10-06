namespace b0wter.CouchDb.Tests.Integration.TestModels

module Default =
    open FsUnit.Xunit
    
    type T = {
        _id: System.Guid
        _rev: string option
        ``type``: string
        myInt: int
        myFirstString: string
        mySecondString: string
        myFloat: float
        myDate: System.DateTime
    }
    
    let create (id, myInt, myFirstString, mySecondString, myFloat, myDate) =
        {
            _id = id
            _rev = None
            ``type`` = "Default"
            myInt = myInt
            myFirstString = myFirstString
            mySecondString = mySecondString
            myFloat = myFloat
            myDate = myDate
        }
    
    let defaultInstance = create (System.Guid.Parse("c8cb91dc-1121-43de-a858-0742327ff158"), 42, "foo", "bar", 1.38064852, System.DateTime(2000, 8, 1, 12, 0, 0))
    
    let compareWithoutRev (a: T) (b: T) =
        a._id |> should equal b._id
        a.``type`` |> should equal b.``type``
        a.myInt |> should equal b.myInt
        a.myFirstString |> should equal b.myFirstString
        a.mySecondString |> should equal b.mySecondString
        a.myFloat |> should equal b.myFloat
        a.myDate |> should equal b.myDate