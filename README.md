# Audit.NET
A small framework to audit .NET object changes

Generate an [audit log](https://en.wikipedia.org/wiki/Audit_trail) with evidence for reconstruction and examination of activities that have affected a specific operation or procedure. 

With Audit.NET you can easily generate tracking information about an operation being executed.

###Usage

Surround the operation code you want to audit with a `using` block, indicating the object(s) to track.

The library will gather contextual information about the user and the machine, as well as the tracked object's state, and optionally [Comments]() and [Custom Fields]() provided.

It will generate an output (event) for each operation. You decide where to save the events by injecting your own persistence mechanism or by using one of the configurable mechanisms provided:

- File Log
- Windows Event Log
- Mongo DB
- Sql Server
- Azure Document DB




