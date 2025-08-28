namespace Audit.EntityFramework;

public class ColumnValueChange
{
    public object OriginalValue { get; set; }
    public object NewValue { get; set; }
}