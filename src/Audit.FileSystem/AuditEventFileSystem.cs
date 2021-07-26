using Audit.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Audit.FileSystem
{
    /// <summary>
    /// Represents the output of the audit process for a File System event
    /// </summary>
    public class AuditEventFileSystem : AuditEvent
    {
        /// <summary>
        /// Gets or sets the file system event details.
        /// </summary>
        public FileSystemEvent FileSystemEvent { get; set; }
    }
 
}
