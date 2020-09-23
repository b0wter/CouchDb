namespace b0wter.CouchDb.Tests.Integration.DesignDocumentTestModels

module Default =

    open b0wter.CouchDb.Lib.DesignDocuments.DesignDocument

    let defaultId = "b1fbe634-a0b6-40c2-b898-c5c5e4e54f01"
    let defaultView1 = createView "all" Map "function(doc) { emit(doc._id, doc); }"
    let defaultView2 = createView "myIntEq1" Map "function(doc) { if (doc.myInt && doc.myInt === 1) { emit(1, doc); } }"
    let defaultDoc = createDocWithId defaultId [ defaultView1; defaultView2 ]