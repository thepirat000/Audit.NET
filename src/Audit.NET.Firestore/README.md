# Audit.NET.Firestore
**Google Cloud Firestore provider for [Audit.NET library](https://github.com/thepirat000/Audit.NET).** (An extensible framework to audit executing operations in .NET).

Store the audit events in a Google Cloud Firestore database as documents in a collection.

## Install

**NuGet Package** 

```
PM> Install-Package Audit.NET.Firestore
```

[![NuGet Status](https://img.shields.io/nuget/v/Audit.NET.Firestore.svg?style=flat)](https://www.nuget.org/packages/Audit.NET.Firestore/)
[![NuGet Count](https://img.shields.io/nuget/dt/Audit.NET.Firestore.svg)](https://www.nuget.org/packages/Audit.NET.Firestore/)

## Usage
Please see the [Audit.NET Readme](https://github.com/thepirat000/Audit.NET#usage)

## Configuration
Set the static `Audit.Core.Configuration.DataProvider` property to set the Firestore data provider, or call the `UseFirestore` method on the fluent configuration. This should be done before any `AuditScope` creation, i.e. during application startup.

### Basic Configuration

Using the fluent configuration API:

```c#
Audit.Core.Configuration.Setup()
    .UseFirestore(config => config
        .ProjectId("your-project-id")
        .Collection("AuditEvents"));
```

Or directly assigning the data provider:

```c#
Audit.Core.Configuration.DataProvider = new Audit.Firestore.Providers.FirestoreDataProvider()
{
    ProjectId = "your-project-id",
    Collection = "AuditEvents"
};
```

### Authentication

The Firestore provider supports several authentication methods:

#### Default Application Credentials
If running on Google Cloud Platform or with properly configured environment variables:

```c#
Audit.Core.Configuration.Setup()
    .UseFirestore(config => config
        .ProjectId("your-project-id")
        .Collection("AuditEvents"));
```

#### Service Account Key File
Using a JSON credentials file:

```c#
Audit.Core.Configuration.Setup()
    .UseFirestore(config => config
        .ProjectId("your-project-id")
        .CredentialsFromFile("path/to/credentials.json")
        .Collection("AuditEvents"));
```

#### Service Account JSON String
Using credentials as a JSON string:

```c#
Audit.Core.Configuration.Setup()
    .UseFirestore(config => config
        .ProjectId("your-project-id")
        .CredentialsFromJson(credentialsJsonString)
        .Collection("AuditEvents"));
```

#### Custom FirestoreDb Instance
Using a pre-configured FirestoreDb instance:

```c#
var firestoreDb = FirestoreDb.Create("your-project-id");
Audit.Core.Configuration.Setup()
    .UseFirestore(config => config
        .FirestoreDb(firestoreDb)
        .Collection("AuditEvents"));
```

### Provider Options

- **ProjectId**: The Google Cloud project ID (required unless using custom FirestoreDb).
- **Database**: The Firestore database name. Default is "(default)".
- **Collection**: The Firestore collection name for storing audit events. Can be a fixed string or a function of the audit event.
- **CredentialsFilePath**: Path to the service account credentials JSON file.
- **CredentialsJson**: Service account credentials as a JSON string.
- **FirestoreDb**: A custom pre-configured FirestoreDb instance.
- **IdBuilder**: A function that returns the document ID to use for a given audit event. By default, Firestore generates the ID automatically.
- **SanitizeFieldNames**: Whether to sanitize field names by replacing dots with underscores. Default is false.
- **ExcludeNullValues**: Whether to exclude null values from the stored audit event data. Default is false.

### Advanced Configuration

#### Dynamic Collection Names
You can configure collection names based on the audit event:

```c#
Audit.Core.Configuration.Setup()
    .UseFirestore(config => config
        .ProjectId("your-project-id")
        .Collection(auditEvent => $"Audit_{auditEvent.EventType}")
        .IdBuilder(auditEvent => $"{auditEvent.EventType}_{DateTime.UtcNow.Ticks}"));
```

#### Multiple Databases
Firestore supports multiple databases per project:

```c#
Audit.Core.Configuration.Setup()
    .UseFirestore(config => config
        .ProjectId("your-project-id")
        .Database("audit-database")
        .Collection("AuditEvents"));
```

## Querying Events

The Firestore data provider includes methods to retrieve and query audit events.

### Get an Event by ID

```c#
var event = firestoreDataProvider.GetEvent("eventId");
```

### Query Events (In-Memory)

The `QueryEvents()` method returns an IQueryable that loads all events into memory:

```c#
var events = firestoreDataProvider.QueryEvents()
    .Where(ev => ev.EventType == "Login")
    .OrderByDescending(ev => ev.StartDate)
    .Take(10)
    .ToList();
```

### Query Events (Firestore Native)

For better performance with large collections, use the native Firestore query methods:

```c#
// Query with Firestore filters
var events = await firestoreDataProvider.QueryEventsAsync(query => query
    .WhereEqualTo("EventType", "Login")
    .WhereGreaterThan("StartDate", DateTime.UtcNow.AddDays(-7))
    .OrderByDescending("StartDate")
    .Limit(10));
```

### Access Native Firestore Collection

You can access the native Firestore collection reference for advanced operations:

```c#
var collection = firestoreDataProvider.GetFirestoreCollection();
var query = collection.WhereEqualTo("Environment.UserName", "admin");
var snapshot = await query.GetSnapshotAsync();
```

## Field Name Restrictions

Firestore has restrictions on field names (cannot contain dots). You can enable automatic sanitization to replace dots with underscores:

```c#
Audit.Core.Configuration.Setup()
    .UseFirestore(config => config
        .ProjectId("your-project-id")
        .SanitizeFieldNames(true)); // Automatically replace dots with underscores
```

## Output Sample

Audit events are stored as Firestore documents with the following structure:

```json
{
  "_timestamp": "2024-01-10T10:30:45Z",
  "EventType": "Order:Update",
  "Environment": {
    "UserName": "john.doe",
    "MachineName": "WORKSTATION01",
    "DomainName": "CORPORATE",
    "CallingMethodName": "OrderService.UpdateOrder",
    "Culture": "en-US"
  },
  "StartDate": "2024-01-10T10:30:45.123Z",
  "EndDate": "2024-01-10T10:30:45.789Z",
  "Duration": 666,
  "Target": {
    "Type": "Order",
    "Old": {
      "OrderId": 12345,
      "Status": "Pending",
      "Total": 150.00
    },
    "New": {
      "OrderId": 12345,
      "Status": "Approved",
      "Total": 150.00
    }
  },
  "CustomFields": {
    "ApprovedBy": "manager@company.com",
    "ApprovalReason": "Valid payment method"
  }
}
```

## Performance Considerations

1. **Document Size**: Firestore documents have a maximum size of 1MB. Large audit events may exceed this limit.

2. **Indexing**: Create composite indexes for fields you frequently query together.

3. **Collection Size**: For high-volume auditing, consider:
   - Using dynamic collection names to partition data
   - Implementing a retention policy to delete old events
   - Using batch operations for bulk inserts

4. **Field Names**: Field name sanitization (dots to underscores) adds a small overhead. Enable it only if your field names contain dots.

## Connection Testing

You can test the Firestore connection:

```c#
var provider = new FirestoreDataProvider()
{
    ProjectId = "your-project-id"
};

await provider.TestConnectionAsync();
``` 