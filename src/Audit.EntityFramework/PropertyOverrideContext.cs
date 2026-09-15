namespace Audit.EntityFramework;

/// <summary>
/// Context passed to <c>Override</c> callbacks when resolving audit column values.
/// </summary>
/// <remarks>
/// For EF Core complex properties, <see cref="ComplexPropertyPath"/> identifies the leaf column
/// (for example <c>Contact2.Number</c> or <c>address.country.code</c> for JSON-mapped complexes).
/// <para>
/// When a parent complex property override is used (for example <c>Person.Contact2</c>),
/// <see cref="PropertyName"/> is the parent property name (<c>Contact2</c>) and
/// <see cref="SourceValue"/> is the leaf scalar being audited.
/// </para>
/// <para>
/// Parent-level <c>Ignore</c>, <c>Override</c> and <c>Format</c> apply to <b>direct scalar children</b>
/// of that complex property only (one level). Nested complex children resolve against their own
/// complex CLR type configuration and their immediate parent complex property.
/// </para>
/// </remarks>
public sealed class PropertyOverrideContext
{
    /// <summary>
    /// The property name used for configuration lookup (the leaf property, or the parent complex property when falling back).
    /// </summary>
    public string PropertyName { get; set; }

#if EF_CORE_8_OR_GREATER
    /// <summary>
    /// The full path of the complex property leaf being audited, or <c>null</c> for regular entity properties.
    /// </summary>
    public string ComplexPropertyPath { get; set; }

    /// <summary>
    /// <c>true</c> when <see cref="SourceValue"/> is the original value; <c>false</c> when it is the new/current value.
    /// </summary>
    public bool IsOriginal { get; set; }
#endif

    /// <summary>
    /// The value before override/format is applied.
    /// </summary>
    public object SourceValue { get; set; }
}
