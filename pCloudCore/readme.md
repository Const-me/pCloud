A straightforward implementation of the API documented at https://docs.pcloud.com/
The library consumes binary protocol, because I don’t like neither JSON nor HTTP too much.
TLS traffic encryption is supported.

The APIs uses async-await a lot.
You can send multiple requests over the same connection, they will be pipelined i.e. the client will send several requests, then start waiting for their responses.
The maximum in-flight requests count is limited to 8.
The public API is thread safe.

You may want to change `Authentication.deviceInfoString` public field to the name of your app.
The default value is derived from `[AssemblyProduct]` and version attributes of your application.

The library doesn’t support 100% of the API, only a subset of it: login/logout, directory operations like create/rename/move/list, file operations like create/read/write/delete, upload links, and a few more requests.
Feel free to fork and implement whatever extra you need.