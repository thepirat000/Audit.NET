# Audit.NET.Udp
**UDP provider for [Audit.NET library](https://github.com/thepirat000/Audit.NET)** (An extensible framework to audit executing operations in .NET).

Sends Audit Logs as UDP datagrams to a remote host or a multicast group.

## Install

**NuGet Package** 
To install the package run the following command on the Package Manager Console:

```
PM> Install-Package Audit.NET.Udp
```

[![NuGet Status](https://img.shields.io/nuget/v/Audit.NET.Udp.svg?style=flat)](https://www.nuget.org/packages/Audit.NET.Udp/)

## Usage
Please see the [Audit.NET Readme](https://github.com/thepirat000/Audit.NET#usage)

## Configuration
Set the static `Audit.Core.Configuration.DataProvider` property to set the UDP data provider, or call the `UseUdp` method on the fluent configuration. This should be done before any `AuditScope` creation, i.e. during application startup.

For example:
```c#
Audit.Core.Configuration.DataProvider = new UdpDataProvider()
{
	RemoteAddress = IPAddress.Parse("224.0.0.1"),
	RemotePort = 3333
};
```

Or by using the [fluent configuration API](https://github.com/thepirat000/Audit.NET#configuration-fluent-api):
```c#
Audit.Core.Configuration.Setup()
    .UseUdp(config => config
		.RemoteAddress("224.0.0.1")
		.RemotePort(3333));
```

### Provider Options

Mandatory:
- **RemoteAddress**: The address of the remote host or multicast group to which the underlying UdpClient should send the audit events.
- **RemotePort**: The port number of the remote host or multicast group to which the underlying UdpClient should send the audit events.

Optional:
- **MulticastMode**: To indicate if the RemoteAddress is a multicast group (default is Auto-Detect).
- **CustomSerializer**: To specify a custom serialization method for the events to send as UDP packets (default is JSON encoded as UTF-8).
- **CustomDeserializer**: To specify a custom deserialization method for the events to receive UDP packets.

### Notes

The theoretical limit for the maximum size of a UDP packet is approximately 64Kb, so events that serializes to a length larger than that, will make the provider fail with an exception of type `SocketException`.
