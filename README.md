# TcpTest

This project is aimed to help improve disconnect reliability for both WatsonTcp and SimpleTcp under the following scenarios:

- **Server-side Dispose** Graceful termination of all client connections
- **Server-side Client Removal** Graceful termination of one client connection
- **Server-side Termination** Abrupt termination due to process abort or CTRL-C
- **Client-side Dispose** Graceful termination of a client connection
- **Client-Side Termination** Abrupt termination due to process abort or CTRL-C

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

On the server, use ```dispose```, ```remove```, ```q```, or ```ctrl-c```.

On the client, use ```dispose```, ```q```, or ```ctrl-c```. 
