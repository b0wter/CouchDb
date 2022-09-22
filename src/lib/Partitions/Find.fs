namespace b0wter.CouchDb.Lib.Partitions

open b0wter.CouchDb.Lib

module Find =

    type ExecutionStats = Generic.Find.ExecutionStats
    
    type MetaData = Generic.Find.MetaData
    
    type Response<'a> = Generic.Find.Response<'a>
    
    type Result<'a> = Generic.Find.Result<'a>
    
    let asResult<'a>  = Generic.Find.asResult<'a>
    
    let queryWithOutput<'a> (props: DbProperties.DbProperties) (dbName: string) (partition: string) (expression: Mango.Expression) =
        Generic.Find.queryWithOutput<'a> props (Generic.Find.FindIn.Partition {| dbName = dbName; partitionName = partition |}) expression
    
    let query<'a> (props: DbProperties.DbProperties) (dbName: string) (partition: string) (expression: Mango.Expression) =
        Generic.Find.query<'a> props (Generic.Find.FindIn.Partition {| dbName = dbName; partitionName = partition |}) expression
        
    let queryJObjectsWithOutput props dbName partition =
        Generic.Find.queryJObjectsWithOutput props (Generic.Find.FindIn.Partition {| dbName = dbName; partitionName = partition |})

    let queryJObjectsAsResultWithOutput props dbName partition expression =
        Generic.Find.queryJObjectsAsResultWithOutput props (Generic.Find.FindIn.Partition {| dbName = dbName; partitionName = partition |}) expression

    let queryObjects props dbName partition = Generic.Find.queryObjects props (Generic.Find.FindIn.Partition {| dbName = dbName; partitionName = partition |})
    
    let queryAsResult<'a> props dbName partition = Generic.Find.queryAsResult<'a> props (Generic.Find.FindIn.Partition {| dbName = dbName; partitionName = partition |})
    
    let queryAsResultWithOutput<'a> props dbName partition = Generic.Find.queryAsResultWithOutput<'a> props (Generic.Find.FindIn.Partition {| dbName = dbName; partitionName = partition |})
    
    let getFirst = Generic.Find.getFirst