Run integration tests
---------------------

There is a dedicated section on how to run the integration tests in the [repositody readme](https://github.com/b0wter/CouchDb/blob/master/README.md#integration-tests).

Structure
---------
The project contains a source code folder for each of the five "big" api endpoints defined by the [CouchDb documentation](https://docs.couchdb.org/en/stable/api/server/index.html):

 * Server
 * Database
 * Documents
 * Design Documents
 * Local Documents

Each of these folders contains one source code file per api endpoint in the collection. Each of these files contains at least one test.
