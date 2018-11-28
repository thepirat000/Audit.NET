#if NET45
using Audit.EntityFramework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Mapping;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Reflection;

/*
https://michaelmairegger.wordpress.com/2013/03/30/find-primary-keys-from-entities-from-dbcontext/
https://romiller.com/2014/04/08/ef6-1-mapping-between-types-tables/
https://romiller.com/2015/08/05/ef6-1-get-mapping-between-properties-and-columns/
https://lowrymedia.com/2014/06/10/ef6-1-mapping-between-types-tables-including-derived-types/
*/

/// <summary>
/// Entity Helper Methods for EF 6
/// </summary>
public sealed class EntityKeyHelper
{
    //Singleton
    private static readonly Lazy<EntityKeyHelper> LazyInstance = new Lazy<EntityKeyHelper>(() => new EntityKeyHelper());
    //Type -> KeyNames
    private readonly ConcurrentDictionary<Type, string[]> _keyNamesCache = new ConcurrentDictionary<Type, string[]>();
    //Type -> ForeignKeyNames
    private readonly ConcurrentDictionary<Type, string[]> _foreignKeyNamesCache = new ConcurrentDictionary<Type, string[]>();
    //Type -> TableName
    private readonly ConcurrentDictionary<Type, EntityName> _tableNamesCache = new ConcurrentDictionary<Type, EntityName>();
    //Type -> PropertyName -> ColumnName
    private readonly ConcurrentDictionary<Type, Dictionary<string, string>> _columnNamesCache = new ConcurrentDictionary<Type, Dictionary<string, string>>();

    private EntityKeyHelper() { }

    public static EntityKeyHelper Instance
    {
        get { return LazyInstance.Value; }
    }

    #region Private Methods
    private string[] GetKeyNames(DbContext context, Type entityType)
    {
        entityType = GetBaseEntityType(context, entityType);
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

        _keyNamesCache[entityType] = keyNames;
        return keyNames;
    }

    private string[] GetForeignKeyNames(DbContext context, Type entityType)
    {
        entityType = GetBaseEntityType(context, entityType);
        string[] keys;
        if (_foreignKeyNamesCache.TryGetValue(entityType, out keys))
        {
            return keys;
        }
        ObjectContext objectContext = ((IObjectContextAdapter)context).ObjectContext;
        //create method CreateObjectSet with the generic parameter of the base-type
        MethodInfo method = typeof(ObjectContext).GetMethod("CreateObjectSet", Type.EmptyTypes)
                                                 .MakeGenericMethod(entityType);
        dynamic objectSet = method.Invoke(objectContext, null);

        var navProps = objectSet.EntitySet.ElementType.NavigationProperties as IEnumerable<NavigationProperty>;
        string[] keyNames = navProps?.SelectMany(n => n.GetDependentProperties()).Select(fk => fk.Name).Distinct().ToArray();

        _foreignKeyNamesCache[entityType] = keyNames;
        return keyNames;
    }

    private MappingFragment GetMappingFragment(Type type, DbContext context)
    {
        var metadata = ((IObjectContextAdapter)context).ObjectContext.MetadataWorkspace;

        // Get the part of the model that contains info about the actual CLR types
        var objectItemCollection = ((ObjectItemCollection)metadata.GetItemCollection(DataSpace.OSpace));

        // Get the entity type from the model that maps to the CLR type
        var entityType = metadata
                .GetItems<EntityType>(DataSpace.OSpace)
                      .Single(e => objectItemCollection.GetClrType(e) == type);


        // Get the entity set that uses this entity type
        var entitySet = metadata.GetItems(DataSpace.CSpace)
            .Where(x => x.BuiltInTypeKind == BuiltInTypeKind.EntityType)
            .Cast<EntityType>()
            .Single(x => x.Name == entityType.Name);

        var entitySetMappings = metadata.GetItems<EntityContainerMapping>(DataSpace.CSSpace).Single().EntitySetMappings.ToList();

        // Find the mapping between conceptual and storage model for this entity set
        var mapping = entitySetMappings.SingleOrDefault(x => x.EntitySet.Name == entitySet.Name);
        if (mapping != null)
        {
            return mapping.EntityTypeMappings.Single().Fragments.Single();
        }
        else
        {
            mapping = entitySetMappings.SingleOrDefault(x => x.EntityTypeMappings.Where(y => y.EntityType != null).Any(y => y.EntityType.Name == entitySet.Name));
            if (mapping != null)
            {
                return mapping.EntityTypeMappings.Where(x => x.EntityType != null).Single(x => x.EntityType.Name == entityType.Name && x.Fragments.Count > 0).Fragments.Single();
            }
            else
            {
                var entitySetMapping = entitySetMappings.Single(x => x.EntityTypeMappings.Any(y => y.IsOfEntityTypes.Any(z => z.Name == entitySet.Name)));
                return entitySetMapping.EntityTypeMappings.First(x => x.IsOfEntityTypes.Any(y => y.Name == entitySet.Name)).Fragments.Single();
            }
        }
    }

    private Type GetObjectEntityType(Type type)
    {
        return ObjectContext.GetObjectType(type);
    }

    private Type GetBaseEntityType(DbContext context, Type type)
    {
        var objectContext = ((IObjectContextAdapter)context).ObjectContext;
        type = ObjectContext.GetObjectType(type);
        if (type.BaseType != null && type.BaseType != typeof(object))
        {
            var baseIsMapped = objectContext.MetadataWorkspace.TryGetType(type.BaseType.Name, type.BaseType.Namespace, DataSpace.OSpace, out var edmType);
            if (baseIsMapped)
            {
                return GetBaseEntityType(context, type.BaseType);
            }
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
        var entityType = GetObjectEntityType(entity.GetType());
        var keyNames = GetKeyNames(context, entityType);
        for (int i = 0; i < keyNames.Length; i++)
        {
            var columnName = GetColumnName(entity.GetType(), keyNames[i], context);
            result.Add(columnName, entityType.GetProperty(keyNames[i]).GetValue(entity, null));
        }
        return result;
    }

    /// <summary>
    /// Gets the foreign keys and values for a given entity in a given db context.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <param name="context">The db context.</param>
    public Dictionary<string, object> GetForeignKeysValues(object entity, DbContext context)
    {
        var result = new Dictionary<string, object>();
        var entityType = GetObjectEntityType(entity.GetType());
        var fkNames = GetForeignKeyNames(context, entityType);
        foreach (var fk in fkNames)
        {
            result.Add(fk, entityType.GetProperty(fk).GetValue(entity, null));
        }
        return result;
    }

    /// <summary>
    /// Gets the name of the table given an entity type (or proxy) and a db context.
    /// </summary>
    /// <param name="type">The entity type (or proxy).</param>
    /// <param name="context">The db context.</param>
    public EntityName GetTableName(Type type, DbContext context)
    {
        type = GetObjectEntityType(type);
        EntityName name;
        if (_tableNamesCache.TryGetValue(type, out name))
        {
            return name;
        }
        // Get the type mapping
        var mappingFragment = GetMappingFragment(type, context);

        // Find the storage entity set (table) that the entity is mapped
        var table = mappingFragment.StoreEntitySet;

        // Return the table name from the storage entity set
        name = new EntityName()
        {
            Table = (string)table.MetadataProperties["Table"].Value ?? table.Name ?? type.Name,
            Schema = table.Schema
        };

        _tableNamesCache[type] = name;
        return name;
    }

    /// <summary>
    /// Gets the name of the column given an entity type (or proxy), a property name and a db context.
    /// </summary>
    /// <param name="type">The entity type (or proxy).</param>
    /// <param name="propertyName">The property name.</param>
    /// <param name="context">The db context.</param>
    public string GetColumnName(Type type, string propertyName, DbContext context)
    {
        type = GetObjectEntityType(type);
        if (_columnNamesCache.ContainsKey(type) && _columnNamesCache[type].ContainsKey(propertyName))
        {
            return _columnNamesCache[type][propertyName];
        }
        // Get the type mapping
        var mappingFragment = GetMappingFragment(type, context);
        // Find the storage property (column) that the property is mapped
        var columnName = mappingFragment
            .PropertyMappings
            .OfType<ScalarPropertyMapping>()
                  .SingleOrDefault(m => m.Property.Name == propertyName)?
                .Column
                .Name;
        if (columnName == null)
        {
            // Try to get the column name from the base type, if any
            var baseType = GetBaseEntityType(context, type);
            if (baseType != type)
            {
                return GetColumnName(baseType, propertyName, context);
            }
        }
        if (columnName == null)
        {
            columnName = propertyName;
        }
        if (!_columnNamesCache.ContainsKey(type))
        {
            _columnNamesCache[type] =
                new Dictionary<string, string>(); // Not thread-safe, but not dangerous since at most it will lost some cached values
        }
        _columnNamesCache[type][propertyName] = columnName;
        return columnName;
    }
    #endregion
}
#endif