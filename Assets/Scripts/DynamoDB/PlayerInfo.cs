using Amazon.DynamoDBv2.DataModel;

namespace DynamoDBForUnity
{

    [DynamoDBTable(nameof(DynamoDBForUnity))]
    public class PlayerInfo
    {
        [DynamoDBHashKey] 
        public string UserId { get; set; }
        [DynamoDBProperty]
        public string Initials { get; set; }
        [DynamoDBProperty]
        public int HighScore { get; set; }

    }
}
