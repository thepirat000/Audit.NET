#if EF_FULL
using System;
using System.Collections.Generic;
using System.Text;
#if IS_NK_JSON
using Newtonsoft.Json;
#else
using System.Text.Json.Serialization;
#endif


namespace Audit.EntityFramework
{
    public class AssociationEntryRecord
    {
        public string Schema { get; set; }
        public string Table { get; set; }
        public object Entity { get; set; }
        public IDictionary<string, object> PrimaryKey { get; set; }
        [JsonIgnore]
        internal object InternalEntity { get; set; }
    }
}
#endif