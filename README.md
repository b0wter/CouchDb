Current Status
==============

[![Build Status](https://b0wter.visualstudio.com/b0wter.CouchDb/_apis/build/status/b0wter.CouchDb?branchName=master)](https://b0wter.visualstudio.com/b0wter.CouchDb/_build/latest?definitionId=28&branchName=master)

This project is currently in development and by no means production ready! Please consult the tables below to see which features are supported. The endpoints refer to the endpoints listed in the official CouchDb documentation.

You can get the library from [nuget](https://www.nuget.org/packages/b0wter.CouchDb/).

Contributing
------------
Contributions (bug fixes, features, ...) are welcome! Please submit a pull request. I will merge the PR if it satisfies the following requirements:

 * The integration tests succeed.
 * You have added tests (if it's a new feature) or
 * You have fixed tests (it it's a bugfix)
 * The new methods/member have XML documentation tags
 * I am convinced it will not break anything :)

If you are in doubt please submit an issue!


Features
=======
Note that (optional) query parameter are currently *not supported*! Even for endpoints that are marked as working. Query parameters that are required (like the `rev` parameter for `/db/docid/` `[PUT]`) are supported.

| Authentication | Status |
|--------|--------|
| Cookie | ✔️      |
| Basic  | ❌      |
| Proxy  | ❌      |


Databases endpoint
------------------
| Endpoint                | HEAD | GET | POST | PUT | DELETE |
|-------------------------|------|-----|------|-----|--------|
| /db                     | ✔️    | ✔️   | ✔️    | ✔️   | ✔️      |
| /db/_all_docs           |      | ✔️   | ✔️    |     |        |
| /db/_design_docs        |      | ❌   | ❌    |     |        |
| /db/_bulk_get           |      |     | ❌    |     |        |
| /db/_bulk_docs          |      |     | ❌    |     |        |
| /db/_find               |      |     | ✔️    |     |        |
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


Documents endpoint
------------------
| Endpoint           | HEAD | GET | POST | PUT | DELETE | COPY |
|--------------------|------|-----|------|-----|--------|------|
| /db/doc            | ✔️    | ✔️   |      | ✔️   | ✔️      | ✔️   |
| /db/doc/attachment | ❌    | ❌   | ❌    | ❌   | ❌      |      |

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

More examples
-------------
Please take a look at the [integration tests](https://github.com/b0wter/CouchDb/tree/master/tests/integration) for more examples. Each query is run at least once.

Id and revision properties
--------------------------
In order to do any meaningful PUT/POST operations your records need to define an `_id` and a `_rev` property. You can name these fields whatever you like. However, you have to add the `[<JsonProperty("_id")]` and `[<JsonProperty("_rev")]` attribute.
```
type MyRecord = {
	[<JsonProperty("_id")>]
	myId: System.Guid
	[<JsonProperty("_rev")>]
	myRevision: string
	myA: int
	myB: float }
}
```
In case you miss these attributes CouchDb will assign these values on its own. But since their names are unknown to the deserializer these values will never be used. Thus, updating a document will result in the creation of a new document!

Why use this approach? I could define interfaces are make the requirement that all objects need to be inherited from an abstract base class but I want to keep things as simple as possible.

Tests
=====

Integration tests
-----------------
The repository comes with a project for integration tests. These tests require you to have a couchdb server running. You need to set the connection details either in `tests/integration/appsettings.json` or pass them as environment variables, e.g.:
```
COUCHDB_HOST=localhost
COUCHDB_PORT=5984
COUCHDB_USER=admin
COUCHDB_PASSWORD=password
```

### Running a local CouchDb server

The two recommended ways to start a local CouchDb instance use docker. If you want to install CouchDb on your local system please consult the os specific instructions on the CouchDb page.

#### Using docker-compose
There is a `docker-compose.yml` in `tests/integration`. Simply change into that directory and run `docker-compose up` to start a server on the default port (5984), username "admin" and password "password".

#### Using docker run
The following command is the same as running the `docker-compose.yml`. It will automatically destroy the container on exit.
```
$ docker run --rm -it -p 5984:5984 -e COUCHDB_USER=admin -e COUCHDB_PASSWORD=password
``` 

### Running the tests

There are two ways to run the tests. If you have the dotnet sdk installed you can use the `dotnet test` command, otherwise you'll have to rely on a `Dockerfile`.

#### Use dotnet cli
If you just want to see the results of the test just run the following command in the repository root:
```
$ dotnet test
```
If you want to generate an XML result file use:
```
$ dotnet test --test-adapter-path:. --logger:xunit
```
instead. The results will be stored in `tests/integration/TestResults/TestResults.xml`.

#### Use docker
The repository root contains a `Dockerfile` that compiles its contents. You will need to first build the image and then run it:
```
$ docker build -t couchdb-lib .
$ HOSTIP=$(ip -4 addr show docker0 | grep -Po 'inet \K[\d.]+')
$ docker run --rm --name couchdb-tests -it -e COUCHDB_HOST=$HOSTIP couchdb-lib:latest
```
This will show you the test results in the terminal and delete the container after running the tests. You *must* supply the ip address of the CouchDb server even if it's running on localhost since localhost inside the test container is not the same as localhost on your machine! The given command expects the CouchDb to run as a docker container with exposed port (5984).

Test results are automatically generated. To retrieve them you can either mount a local folder to `/output` in the container or remove the `--rm` flag and run
```
$ docker cp couchdb_tests:/output/integration.xml <YOUR_LOCAL_FOLDER>
$ docker rm couchdb_tests
```
after the tests have finished.

### Why not use a new container instance for each test?

The latest version of the CouchDb Docker image takes several minutes to start.
This prohibits any useful use of fresh containers for every tests.
Once this issue has been fixed I will look into changing the behaviour.

### Can't the tests start the container?

I am currently looking into using 
[Docker.Net](https://github.com/microsoft/Docker.DotNet) to 
automatically create and run the docker container. However, this work
is not finished yet.
