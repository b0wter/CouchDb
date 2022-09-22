namespace b0wter.CouchDb.Lib.Databases

open b0wter.CouchDb.Lib

module Find =

    type ExecutionStats = Generic.Find.ExecutionStats
    
    type MetaData = Generic.Find.MetaData
    
    type Response<'a> = Generic.Find.Response<'a>
    
    type Result<'a> = Generic.Find.Result<'a>
    
    let asResult<'a>  = Generic.Find.asResult<'a>
    
    let queryWithOutput<'a> (props: DbProperties.DbProperties) (dbName: string) (expression: Mango.Expression) =
        Generic.Find.queryWithOutput<'a> props (Generic.Find.FindIn.Database dbName) expression
    
    let query<'a> (props: DbProperties.DbProperties) (dbName: string) (expression: Mango.Expression) =
        Generic.Find.query<'a> props (Generic.Find.FindIn.Database dbName) expression
        
    let queryJObjectsWithOutput props dbName =
        Generic.Find.queryJObjectsWithOutput props (Generic.Find.FindIn.Database dbName)

    let queryJObjectsAsResultWithOutput props dbName expression =
        Generic.Find.queryJObjectsAsResultWithOutput props (Generic.Find.FindIn.Database dbName) expression

    let queryObjects props dbName = Generic.Find.queryObjects props (Generic.Find.FindIn.Database dbName)
    
    let queryAsResult<'a> props dbName = Generic.Find.queryAsResult<'a> props (Generic.Find.FindIn.Database dbName)
    
    let queryAsResultWithOutput<'a> props dbName = Generic.Find.queryAsResultWithOutput<'a> props (Generic.Find.FindIn.Database dbName)
    
    let getFirst = Generic.Find.getFirst