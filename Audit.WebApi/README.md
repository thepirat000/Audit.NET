#Audit.WebApi

**ASP.NET Web Api Audit Extension for [Audit.NET library](https://github.com/thepirat000/Audit.NET).** (An extensible framework to audit executing operations in .NET).

Generate Audit Trails for Web Api calls.

Audit.WebApi provides the infrastructure to log interactions with ASP.NET Web APIs. It can record action methods calls with caller info and arguments.

##Install

**[NuGet Package](https://www.nuget.org/packages/Audit.WebApi/)**
```
PM> Install-Package Audit.WebApi
```

##Usage

Decorate with an `AuditApiAttribute` the Web Api methods/controllers you want to audit.

For example:

```c#
```
