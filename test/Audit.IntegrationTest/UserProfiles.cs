using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace Audit.IntegrationTest
{
    public class UserProfiles
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public int UserId { get; set; }
        [BsonRequired]
        public string UserName { get; set; }
        [BsonRequired]
        public string Password { get; set; }
        public string Role { get; set; }
        [BsonRequired]
        public string Email { get; set; }
        [BsonRequired]
        public string ProjectId { get; set; }
    }
}