using System;

namespace Audit.Core
{
    /// <summary>
    /// Struct representing a setting whose value can be set directly or as a function of the AuditEvent.<br/><br/>
    /// For example:<br/>
    /// <br/>
    /// Setting&lt;string&gt; s;<br/>
    /// Setting&lt;string&gt; s = "value";<br/>
    /// var s = new Setting&lt;string&gt;("value");<br/>
    /// var s = new Setting&lt;string&gt;(auditEvent => auditEvent.EventType);<br/>
    /// </summary>
    /// <typeparam name="T">The setting value type</typeparam>
    public readonly struct Setting<T>
    {
        private readonly bool _isBuilder;
        private readonly T _value;
        private readonly Func<AuditEvent, T> _valueBuilder;

        /// <summary>
        /// Creates a new Setting with the default value of T
        /// </summary>
        public Setting() : this(default(T))
        {
        }

        /// <summary>
        /// Creates a new Setting with the given value
        /// </summary>
        /// <param name="value">The setting value</param>
        public Setting(T value)
        {
            _value = value;
            _valueBuilder = null;
            _isBuilder = false;
        }

        /// <summary>
        /// Creates a new Setting with the given value builder
        /// </summary>
        /// <param name="valueBuilder">The setting value builder</param>
        public Setting(Func<AuditEvent, T> valueBuilder)
        {
            _value = default;
            _valueBuilder = valueBuilder;
            _isBuilder = true;
        }
        
        /// <summary>
        /// Gets the setting value for the given AuditEvent
        /// </summary>
        /// <param name="auditEvent">The audit event</param>
        public T GetValue(AuditEvent auditEvent)
        {
            return _isBuilder ? _valueBuilder(auditEvent) : _value;
        }

        /// <summary>
        /// Gets the default value of the setting.<br/>
        /// If the setting was created with a value builder, the default value corresponds to the outcome of the value builder when provided with a null AuditEvent.<br/>
        /// If the setting was created with a value, that value is returned.<br/>
        /// </summary>
        public T GetDefault()
        {
            return GetValue(null);
        }

        /// <summary>
        /// Implicit conversion from T to Setting&lt;T&gt;
        /// </summary>
        /// <param name="value">The setting value</param>
        public static implicit operator Setting<T>(T value) => new(value);

        /// <summary>
        /// Implicit conversion from Func&lt;AuditEvent, T&gt; to Setting&lt;T&gt;
        /// </summary>
        /// <param name="func">The setting value func</param>
        public static implicit operator Setting<T>(Func<AuditEvent, T> func) => new(func);
    }
}