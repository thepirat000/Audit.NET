#if NET45
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Mapping;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Reflection;

/*
Original modified to support edmx, etc.
https://michaelmairegger.wordpress.com/2013/03/30/find-primary-keys-from-entities-from-dbcontext/
and
https://romiller.com/2014/04/08/ef6-1-mapping-between-types-tables/
and
https://romiller.com/2015/08/05/ef6-1-get-mapping-between-properties-and-columns/
*/

/// <summary>
/// Entity Helper Methods for EF 6
/// </summary>
public sealed class EntityKeyHelper
{
    //Singleton
    private static readonly Lazy<EntityKeyHelper> LazyInstance = new Lazy<EntityKeyHelper>(() => new EntityKeyHelper());
    //Type -> KeyNames
    private readonly Dictionary<Type, string[]> _keyNamesCache = new Dictionary<Type, string[]>();
    //Type -> TableName
    private readonly Dictionary<Type, string> _tableNamesCache = new Dictionary<Type, string>();
    //Type -> PropertyName -> ColumnName
    private readonly Dictionary<Type, Dictionary<string, string>> _columnNamesCache = new Dictionary<Type, Dictionary<string, string>>();

    private EntityKeyHelper() { }

    public static EntityKeyHelper Instance
    {
        get { return LazyInstance.Value; }
    }

    #region Private Methods
    private string[] GetKeyNames(DbContext context, Type entityType)
    {
        entityType = GetBaseEntityType(entityType);
        string[] keys;
        if (_keyNamesCache.TryGetValue(entityType, out keys))
        {
            return keys;
        }
        ObjectContext objectContext = ((IObjectContextAdapter)context).ObjectContext;
        //create method CreateObjectSet with the generic parameter of the base-type
        MethodInfo method = typeof(ObjectContext).GetMethod("CreateObjectSet", Type.EmptyTypes)
                                                 .MakeGenericMethod(entityType);
        dynamic objectSet = method.Invoke(objectContext, null);
        IEnumerable<dynamic> keyMembers = objectSet.EntitySet.ElementType.KeyMembers;
        string[] keyNames = keyMembers.Select(k => (string)k.Name).ToArray();

        _keyNamesCache.Add(entityType, keyNames);
        return keyNames;
    }

    private EntitySetMapping GetMapping(Type type, DbContext context)
    {
        var metadata = ((IObjectContextAdapter)context).ObjectContext.MetadataWorkspace;

        // Get the part of the model that contains info about the actual CLR types
        var objectItemCollection = ((ObjectItemCollection)metadata.GetItemCollection(DataSpace.OSpace));

        // Get the entity type from the model that maps to the CLR type
        var entityType = metadata
                .GetItems<EntityType>(DataSpace.OSpace)
                      .Single(e => objectItemCollection.GetClrType(e) == type);

        // Get the entity set that uses this entity type
        var entitySet = metadata
            .GetItems<EntityContainer>(DataSpace.CSpace)
                  .Single()
                  .EntitySets
                  .Single(s => s.ElementType.Name == entityType.Name);

        // Find the mapping between conceptual and storage model for this entity set
        var mapping = metadata.GetItems<EntityContainerMapping>(DataSpace.CSSpace)
                      .Single()
                      .EntitySetMappings
                      .Single(s => s.EntitySet == entitySet);

        return mapping;
    }

    private Type GetBaseEntityType(Type type)
    {
        //retreive the base type
        while (type.BaseType != typeof(object))
        {
            type = type.BaseType;
        }
        return type;
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Gets the primary key keys and values for a given entity in a given db context.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <param name="context">The db context.</param>
    public Dictionary<string, object> GetPrimaryKeyValues(object entity, DbContext context)
    {
        var result = new Dictionary<string, object>();
        var entityType = GetBaseEntityType(entity.GetType());
        var keyNames = GetKeyNames(context, entityType);
        for (int i = 0; i < keyNames.Length; i++)
        {
            var columnName = GetColumnName(entity.GetType(), keyNames[i], context);
            result.Add(columnName, entityType.GetProperty(keyNames[i]).GetValue(entity, null));
        }
        return result;
    }

    /// <summary>
    /// Gets the name of the table given an entity type (or proxy) and a db context.
    /// </summary>
    /// <param name="type">The entity type (or proxy).</param>
    /// <param name="context">The db context.</param>
    public string GetTableName(Type type, DbContext context)
    {
        type = GetBaseEntityType(type);
        string name;
        if (_tableNamesCache.TryGetValue(type, out name))
        {
            return name;
        }
        // Get the type mapping
        var mapping = GetMapping(type, context);

        // Find the storage entity set (table) that the entity is mapped
        var table = mapping
            .EntityTypeMappings.Single()
            .Fragments.Single()
            .StoreEntitySet;

        // Return the table name from the storage entity set
        var tableName = (string)table.MetadataProperties["Table"].Value ?? table.Name;
        _tableNamesCache[type] = tableName;
        return tableName;
    }

    /// <summary>
    /// Gets the name of the column given an entity type (or proxy), a property name and a db context.
    /// </summary>
    /// <param name="type">The entity type (or proxy).</param>
    /// <param name="propertyName">The property name.</param>
    /// <param name="context">The db context.</param>
    public string GetColumnName(Type type, string propertyName, DbContext context)
    {
        type = GetBaseEntityType(type);
        if (_columnNamesCache.ContainsKey(type) && _columnNamesCache[type].ContainsKey(propertyName))
        {
            return _columnNamesCache[type][propertyName];
        }
        // Get the type mapping
        var mapping = GetMapping(type, context);
        // Find the storage property (column) that the property is mapped
        var columnName = mapping
            .EntityTypeMappings.Single()
            .Fragments.Single()
            .PropertyMappings
            .OfType<ScalarPropertyMapping>()
                  .SingleOrDefault(m => m.Property.Name == propertyName)?
                .Column
                .Name;

        if (!_columnNamesCache.ContainsKey(type))
        {
            _columnNamesCache[type] = new Dictionary<string, string>(); // Not thread-safe, but not dangerous since at most it will lost some cached values
        }
        _columnNamesCache[type].Add(propertyName, columnName);
        return columnName;
    }
    #endregion
}
#endif