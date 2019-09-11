Current Status
==============

This project is currently in development and by no means production ready! Please consult the tables below to see which features are supported. The endpoints refer to the endpoints listed in the official CouchDb documentation.
Since this library is in its infancy there is no nuget package available. I plan to add automated builds after completing the basic feature set.

General
-------

| Feature        | Status |
|----------------|--------|
| Authentication | ✔️      |


Databases endpoint
------------------
| Endpoint                | HEAD | GET | POST | PUT | DELETE |
|-------------------------|------|-----|------|-----|--------|
| /db                     | ✔️    | ✔️   | ✔️    | ✔️   | ✔️      |
| /db/_all_docs           |      | ✔️   | ✔️    |     |        |
| /db/_design_docs        |      | ❌   | ❌    |     |        |
| /db/_bulk_get           |      |     | ❌    |     |        |
| /db/_bulk_docs          |      |     | ❌    |     |        |
| /db/_find               |      |     | 👨‍💻*   |     |        |
| /db/_index              |      | ❌   | ❌    |     | ❌      |
| /db/_explain            |      |     | ❌    |     |        |
| /db/_shards             |      | ❌   |      |     |        |
| /db/_shards/doc         |      | ❌   |      |     |        |
| /db/_sync_shards        |      |     | ❌    |     |        |
| /db/_changes            |      | ❌   |      |     |        |
| /db/_compact            |      |     | ❌    |     |        |
| /db/_compact/design-doc |      |     | ❌    |     |        |
| /db/_ensure_full_commit |      |     | ❌    |     |        |
| /db/_view_cleanup       |      |     | ❌    |     |        |
| /db/_security           |      | ❌   |      | ❌   |        |
| /db/_purge              |      |     | ❌    |     |        |
| /db/_purged_infos_limit |      | ❌   |      | ❌   |        |
| /db/_missing_revs       |      |     | ❌    |     |        |
| /db/_revs_diff          |      |     | ❌    |     |        |
| /db/_revs_limit         |      | ❌   |      | ❌   |        |


* The ```_find``` endpoint is not yet fully implemented. It currently only allows the user to query for equality. However, multiple selectors are supported as well as subfield matching.

Server endpoint
---------------
| Endpoint                    | HEAD | GET | POST | PUT | DELETE |
|-----------------------------|------|-----|------|-----|--------|
| /                           |      | ✔️   |      |     |        |
| /_active_tasks              |      | ❌   |      |     |        |
| /_all_dbs                   |      | ✔️   |      |     |        |
| /_dbs_info                  |      |     | ✔️    |     |        |
| /_cluster_setup             |      | ❌   | ❌    |     |        |
| /_db_updates                |      | ❌   |      |     |        |
| /_membership                |      | ❌   |      |     |        |
| /_replicate                 |      |     | ❌    |     |        |
| /_scheduler/jobs            |      | ❌   |      |     |        |
| /_scheduler/docs            |      | ❌   |      |     |        |
| /_node/{node-name}/_stats   |      | ❌   |      |     |        |
| /_node/{node-name}/_system  |      | ❌   |      |     |        |
| /_node/{node-name}/_restart |      |     | ❌    |     |        |
| /_utils                     |      | ❌   |      |     |        |
| /_up                        |      | ❌   |      |     |        |
| /_uuids                     |      | ❌   |      |     |        |
| /favicon.ico                |      | ❌   |      |     |        |
|                             |      |     |      |     |        |


How-to
======
Usage of this library is simple.
Start by opening the library.

```
open b0wter.CouchDb.Lib
```

There is a submodule for _database_ and _server_ endpoints. A _documents_ endoint will follow soon. Each type of query has its own submodule containing:

	* a `query` method (sometimes there are multiple methods)
	* a `Response` that contains the results of a successful response from the CouchDb Server
	* a `Result` type that contains all error cases (matching the ones listed in the official documentation) and a `Success` type that contains the `Response`

Connection Details
------------------
Each query takes a `DbProperties.T` argument that contains the information necessary to connect to the server. Use `b0wter.CouchDb.Lib.DbProperties.create` to create an instance.
Note that adding credentials does not automatically authenticate you. You will have to run a `b0wter.CouchDb.Lib.Core.authenticate` request to do so. Since all HTTP requests share the same cookie container you only need to authenticate once for all subsequent requests.

Query Examples
--------------
If your CouchDb server requires authentication please take a look at _Connection Details_.

Every query uses the `async` computational expression. Here is a quick example of how a check for the existance of a database works:

```
open b0wter.CouchDb.Lib

let doesTestDbExist () =
	async {
		let! exists = Database.Exists.query p "test-db"
		match exists with
        | Database.Exists.Result.Exists ->         printf "The database exists."
        | Database.Exists.Result.DoesNotExist ->   printf "The database does not exist."
        | Database.Exists.Result.RequestError e -> printf "Encountered a request error: %s" e.reason
	}
```

All other requests work in the same way. Here is another example using the `_find`-endpoint:

```
let findWithMultipleSelectors () =
	async {
		let nameFindSelector = Find.TypedSelector("name", "myName", id)
		let typefindSelector = Find.TypedSelector("type", "myType", id)
		let multiSelector = Find.MultiSelector([nameFindSelector; typeFindSelector])
		let findParams = Find.createExpression multiSelector
		let! result = Database.Find.query<MyDocumentType> p "test-db" findParams
		do printfn "%A" result
	}
```

If you need more control over the search expression you can create the `b0wter.CouchDb.Lib.Find.Expression` yourself instead of using the premade `Find.createExpression`. 

In case you only require a single selector the previous example boils down to this:

```
let findWithSingleSelectors () =
	async {
		let nameFindSelector = Find.TypedSelector("name", "myName", id)
		let findParams = Find.createExpression nameFindSelector
		let! result = Database.Find.query<MyDocumentType> p "test-db" findParams
		do printfn "%A" result
	}
```

The last parameter of a `TypedSelector` is a translator function that translates the argument (second parameter) to a string. In the above examples it is easy since it's already a string. So we can use the identity function.