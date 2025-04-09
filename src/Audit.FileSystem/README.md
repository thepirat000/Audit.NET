# Audit.FileSystem

**File System Extension for [Audit.NET library](https://github.com/thepirat000/Audit.NET).** 

Generate Audit Logs by intercepting file system events via [FileSystemWatcher](https://docs.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher?view=netstandard-2.0).

Audit.FileSystem provides the infrastructure to create audit logs from the file system events, like creating, renaming, modifying or deleting files and directories.
It relies on [FileSystemWatcher](https://docs.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher?view=netstandard-2.0) class to intercept the events, so the same 
[limitations](https://blogs.msdn.microsoft.com/winsdk/2015/05/19/filesystemwatcher-follies/) applies.

## Install

**NuGet Package** 

To install the package run the following command on the Package Manager Console:

```
PM> Install-Package Audit.FileSystem
```

[![NuGet Status](https://img.shields.io/nuget/v/Audit.FileSystem.svg?style=flat)](https://www.nuget.org/packages/Audit.FileSystem/)
[![NuGet Count](https://img.shields.io/nuget/dt/Audit.FileSystem.svg)](https://www.nuget.org/packages/Audit.FileSystem/)

## Usage

To enable the audit log for a directory, create an instance of `FileSystemMonitor` clas, and call its `Start()` method:

```c#
var fsMon = new Audit.FileSystem.FileSystemMonitor(@"c:\");
fsMon.Options.IncludeSubdirectories = true;
fsMon.Start();
```

Or by using the `FileSystemMonitorOptions` to provide the configuration:

```c#
var fsMon = new Audit.FileSystem.FileSystemMonitor(new FileSystemMonitorOptions()
{
    Path = @"c:\",
    IncludeSubdirectories = true,
    Filter = "*.txt",
    IncludeContentPredicate = fi => fi.Length <= 1024 ? FileSystem.ContentType.Text : FileSystem.ContentType.None,
    CustomFilterPredicate = e => !e.FullPath.StartsWith("$RECYCLE.BIN")                    
});
```

## Configuration

### Output

The audit events are stored using a _Data Provider_. You can use one of the [available data providers](https://github.com/thepirat000/Audit.NET#data-providers-included) or implement your own. Please refer to the [data providers](https://github.com/thepirat000/Audit.NET#data-providers) section on Audit.NET documentation.

### Settings

The `FileSystemMonitorOptions` class include the following settings:

Mandatory:
- **Path**: The path of the directory to monitor.

Optional:
- **EventTypeName**: A string that identifies the event type. Default is "[\{type}] \{name}". Can contain the following placeholders: 
  - \{type}: replaced with the event type (Change, Rename, Create or Delete)
  - \{name}: replaced with the file/directory name
  - \{path}: replaced with the full file/directory path
- **IncludeSubdirectories**:  To indicate if the subdirectories of the provided `Path` should be monitored. Default is false.
- **IncludedEventTypes**: A list indicating the event types (Change, Rename, Create or Delete) that should be included on the audit. Default is NULL meaning all the event types will be logged.
- **Filter**: The [filter string](https://docs.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher.filter?view=netstandard-2.0#System_IO_FileSystemWatcher_Filter) used to determine what files are monitored. Default is "\*.*"
- **CustomFilterPredicate**: Allows to filter events with a custom function that given a file event, returns true if the entry should be logged and false otherwise. Default includes all the files satisfying the provided `Filter` string.
- **IncludeContentPredicate**: Allows to determine if the file contents should be included in the log with a custom function that given a file event, returns a `ContentType` indicating whether the contents
should be included as a string (`Text`), as a byte array (`Binary`) or not included (`None`).
By default content is not included.
- **NotifyFilters**: The [notify filters](https://docs.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher.notifyfilter?view=netstandard-2.0#System_IO_FileSystemWatcher_NotifyFilter). Default is DirectoryName | FileName | LastAccess | LastWrite.
- **IgnoreMD5**: To indicate if the MD5 computation should be ignored. By default the MD5 hash of the file is included on the log.
- **InternalBufferSize**: Gets or sets the size (in bytes) of the [internal buffer](https://docs.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher.internalbuffersize?view=netstandard-2.0#System_IO_FileSystemWatcher_InternalBufferSize).
- **AuditDataProvider**: To indicate the Audit Data Provider to use. Default is NULL to use the [globally configured data provider](https://github.com/thepirat000/Audit.NET#data-provider).
- **CreationPolicy**: To indicate the event creation policy to use. Default is NULL to use the [globally configured creation policy](https://github.com/thepirat000/Audit.NET#creation-policy). 
- **AuditScopeFactory**: Allows to set a specific audit scope factory. By default the globally configured [`AuditScopeFactory`](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET/AuditScopeFactory.cs) is used. 

## Output

Audit.FileSystem output includes:

- Execution time. 
- Environment information.
- File/Directory name, attributes and properties
- File MD5 hash (optional)
- File contents (optional)

## Output Details

The following table describes the Audit.FileSystem output fields:

### [FileSystemEvent](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.FileSystem/FileSystemEvent.cs)

Describes an event from the file system.

| Field Name | Type | Description | 
| ------------ | ---------------- |  -------------- |
| Object | FileSystemObjectType | Indicates the object type: `File`, `Directory` or `Unknown` |
| Event  | FileSystemEventType | The file system event type: `Create`, `change`, `Rename` or `Delete` |
| Errors | string | Any error encountered when processing the file/directory |
| Attributes | string | The file/directory attributes |
| Name | string | The file/directory name |
| OldName | string | In case of rename, the old file/directory name |
| Extension | string | The file extension including the point |
| FullPath | string | The full path to the file/directory |
| Length | long | The file length in bytes |
| CreationTime | datetime | The file/directory creation date and time |
| LastAccessTime | datetime | The file/directory last access date and time |
| LastWriteTime | datetime | The file/directory last write date and time |
| ReadOnly | boolean | Value indicating if the file is read only |
| MD5 | boolean | The MD5 hash of the file |
| FileContent | FileContent | The file contents when included |


### [FileContent](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.FileSystem/FileContent.cs)

Represents the contents of an audited file.

| Field Name | Type | Description | 
| ------------ | ---------------- |  -------------- |
| Type | ContentType | The content type: `Text` or `Binary` |
| Value  | string/byte array | The string (text) or byte array (binary) with the file contents |

## Output Sample

File creation:
```javascript
{
  "EventType": "[Created] file.txt",
  "Environment": {
    "UserName": "Federico",
    "MachineName": "HP",
    "DomainName": "HP",
    "Culture": "en-US"
  },
  "StartDate": "2017-11-26T23:01:44.5567169-06:00",
  "EndDate": "2017-11-26T23:01:44.5567169-06:00",
  "Duration": 0,
  "FileSystemEvent": {
    "Object": "File",
    "Event": "Create",
    "Attributes": "Archive",
    "Name": "file.txt",
    "Extension": ".txt",
    "FullPath": "c:\\Users\\Federico\\Documents\\file.txt",
    "Length": 694,
    "CreationTime": "2017-11-26T23:01:11.750589-06:00",
    "LastAccessTime": "2017-11-26T23:01:11.750589-06:00",
    "LastWriteTime": "2017-11-26T23:01:11.7515849-06:00",
    "MD5": "ddc032e5fe9bb3aa15144cdc35d959c5"
  }
}
```

File renaming
```javascript
{
  "EventType": "[Renamed] renamed.txt",
  "Environment": {
    "UserName": "Federico",
    "MachineName": "HP",
    "DomainName": "HP",
    "Culture": "en-US"
  },
  "StartDate": "2017-11-26T23:01:37.8409103-06:00",
  "EndDate": "2017-11-26T23:01:37.8409103-06:00",
  "Duration": 0,
  "FileSystemEvent": {
    "Object": "File",
    "Event": "Rename",
    "OldName": "file.txt",
    "Name": "renamed.txt",
    "Extension": ".txt",
    "FullPath": "c:\\Users\\Federico\\Documents\\renamed.txt"
  }
}
```

IO Exception:
```javascript
{
  "EventType": "[Created] tmpFC2D.tmp",
  "Environment": {
    "UserName": "Federico",
    "MachineName": "HP",
    "DomainName": "HP",
    "Culture": "en-US"
  },
  "StartDate": "2017-11-26T23:01:03.7363727-06:00",
  "EndDate": "2017-11-26T23:01:03.7363727-06:00",
  "Duration": 0,
  "FileSystemEvent": {
    "Object": "File",
    "Event": "Create",
    "Errors": [
      "IOException when getting file attributes: Could not find file 'c:\\Users\\Federico\\AppData\\Local\\Temp\\tmpFC2D.tmp'."
    ],
    "Name": "tmpFC2D.tmp",
    "Extension": ".tmp",
    "FullPath": "c:\\Users\\Federico\\AppData\\Local\\Temp\\tmpFC2D.tmp"
  }
}
```

## ZZZ Projects - Sponsorship

[Entity Framework Extensions](https://entityframework-extensions.net/) and [Dapper Plus](https://dapper-plus.net/) are major sponsors and are proud to contribute to the development of Audit.NET

Combine the power of auditing with the speed of Bulk Operations to get the best of both worlds — audit and performance.

[![Entity Framework Extensions - Sponsor](https://raw.githubusercontent.com/thepirat000/Audit.NET/master/documents/entity-framework-extensions-sponsor.png)](https://entityframework-extensions.net/bulk-insert)

[![Dapper Plus - Sponsor](https://raw.githubusercontent.com/thepirat000/Audit.NET/master/documents/dapper-plus-sponsor.png)](https://dapper-plus.net/bulk-insert)