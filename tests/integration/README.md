Run integration tests
=====================
In order to run the integration tests an instance of CouchDb needs to be running. 
The tests require it to run on `localhost` with the default port (5984).
`admin` and `password` are used as credentials.

The easiest way to make this happen is to use docker:

```
$ docker run --rm --name "integration-test-couchdb" -it -p 5894:5894 -e COUCHDB_USER=admin -e COUCHDB_PASSWORD=password couchdb:latest
```

This will start a container in interactive mode (so that you can see the STDOUT output). 
Once all the tests have been run you can close the container with `CTRL`+`c`.
Due to the `--rm` parameter the container will automatically be deleted.

Why not use a new container instance for each test?
---------------------------------------------------
The latest version of the CouchDb Docker image takes several minutes to start.
This prohibits any useful use of fresh containers for every tests.
Once this issue has been fixed I will look into changing the behaviour.

Can't the tests start the container?
------------------------------------
I am currently looking into using 
[Docker.Net](https://github.com/microsoft/Docker.DotNet) to 
automatically create and run the docker container. However, this work
is not finished yet.