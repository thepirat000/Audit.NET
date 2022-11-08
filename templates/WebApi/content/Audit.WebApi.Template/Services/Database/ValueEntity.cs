using System.ComponentModel.DataAnnotations;

namespace Audit.WebApi.Template.Services.Database
{
    public class ValueEntity
    {
        [Key]
        public int Id { get; set; }
        public string? Value { get; set; }
    }
}