Current Status
==============

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



