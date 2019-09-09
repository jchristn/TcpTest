# TcpTest

This project is aimed to help improve disconnect reliability for both WatsonTcp and SimpleTcp under the following scenarios:

- Graceful termination of connection ```Dispose()```
- Non-graceful termination of connection through abrupt process termination ```ctrl-c```

Please note: this repository will likely not be maintained in a versioned manner with release numbers.

## Starting the Server

The server listens on ```127.0.0.1:8000```.  Clone and build the source.
```
$ git clone https://github.com/jchristn/tcptest.git
$ cd tcptest
$ dotnet build -f netcoreapp2.2
$ dotnet server/bin/debug/netcoreapp2.2/server.dll
```

## Starting the Client

Start the server first.  
```
$ dotnet client/bin/debug/netcoreapp2.2/client.dll
```

## Operation

Use the dispose commands or use ctrl-c to test.
