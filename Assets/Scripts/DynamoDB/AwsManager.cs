using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon;
using Amazon.CognitoIdentity;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;

namespace DynamoDBForUnity
{
    public class AwsManager : Singleton<AwsManager>
    {
        private static IAmazonDynamoDB _ddbClient;
        private CognitoAWSCredentials _credentials;
        private DynamoDBContext _ddbContext;

        public string IdentityPoolId;
        public string Region;
        public string TableName;

        public PlayerInfo Player { get; private set; }


        public delegate void ClearEvent();
        public event ClearEvent ClearDisplay;

        public delegate void AppendEvent(string message);
        public event AppendEvent AppendDisplay;

        public delegate void ErrorEvent(string message);
        public event ErrorEvent ErrorDisplay;


        protected override void OnAwake()
        {
            if (string.IsNullOrEmpty(IdentityPoolId) ||
                string.IsNullOrEmpty(Region) ||
                string.IsNullOrEmpty(TableName))
                throw new ArgumentNullException();

            UnityInitializer.AttachToGameObject(gameObject);
            _credentials = new CognitoAWSCredentials(IdentityPoolId, RegionEndpoint.GetBySystemName(Region));

            if (_credentials != null)
            {
                _ddbClient = new AmazonDynamoDBClient(_credentials, RegionEndpoint.GetBySystemName(Region));
                AWSConfigs.HttpClient = AWSConfigs.HttpClientOption.UnityWebRequest;
                _ddbContext = new DynamoDBContext(_ddbClient);

                Player = new PlayerInfo
                {
                    UserId = _credentials.GetIdentityId()
                };

                SetPlayerInfo();
            }
            else
            {
                throw new ArgumentNullException();
            }
        }

        /// <summary>
        /// Get player info for current player
        /// </summary>
        public void SetPlayerInfo()
        {
            var request = new ScanRequest
            {
                TableName = TableName,
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> { { ":id", new AttributeValue { S = Player.UserId } } },
                FilterExpression = $"{nameof(PlayerInfo.UserId)} = :id",
                ProjectionExpression = $"{nameof(PlayerInfo.Initials)}, {nameof(PlayerInfo.HighScore)}"
            };

            _ddbClient.ScanAsync(request, result =>
            {
                var userInfo = result.Response.Items.FirstOrDefault();
                if (userInfo != null)
                {
                    foreach (var kp in userInfo)
                    {
                        switch (kp.Key)
                        {
                            case nameof(PlayerInfo.Initials):
                                Player.Initials = GetAttributeValue(kp.Value);
                                break;
                            case nameof(PlayerInfo.HighScore):
                                Player.HighScore = int.Parse(GetAttributeValue(kp.Value));
                                break;
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Updates initials for current player
        /// </summary>
        /// <param name="initials">initials to change to</param>
        public void UpdateInitials(string initials)
        {
            ClearDisplay?.Invoke();

            var request = new UpdateItemRequest
            {
                TableName = TableName,
                Key = new Dictionary<string, AttributeValue> { { nameof(PlayerInfo.UserId), new AttributeValue { S = Player.UserId } } },
                ExpressionAttributeNames = new Dictionary<string, string> { { "#I", nameof(PlayerInfo.Initials) } },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> { { ":i", new AttributeValue { S = initials } } },
                UpdateExpression = "Set #I = :i"
            };

            _ddbClient.UpdateItemAsync(request, result =>
            {
                if (result.Exception == null)
                {
                    Player.Initials = initials;
                    AppendDisplay?.Invoke($"Updated {nameof(PlayerInfo.Initials)} to: {initials}");
                }
                else
                {
                    ErrorDisplay?.Invoke(result.Exception.ToString());
                }
            });
        }

        /// <summary>
        /// Updates current player high score
        /// </summary>
        /// <param name="score">high score to update to</param>
        public void UpdateHighScore(string score)
        {
            ClearDisplay?.Invoke();

            var request = new UpdateItemRequest()
            {
                TableName = TableName,
                Key = new Dictionary<string, AttributeValue> { { nameof(PlayerInfo.UserId), new AttributeValue { S = Player.UserId } } },
                ExpressionAttributeNames = new Dictionary<string, string> { { "#HS", nameof(PlayerInfo.HighScore) } },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> { { ":hs", new AttributeValue { N = score } } },
                UpdateExpression = "Set #HS = :hs"
            };

            _ddbClient.UpdateItemAsync(request, result =>
            {
                if (result.Exception == null)
                {
                    Player.HighScore = int.Parse(score);
                    AppendDisplay?.Invoke($"Updated {nameof(PlayerInfo.HighScore)} to: {score}");
                }
                else
                {
                    ErrorDisplay?.Invoke(result.Exception.ToString());
                }
            });
        }

        /// <summary>
        /// Creates or Updates player info to database
        /// </summary>
        /// <param name="playerInfo">player info to create/update</param>
        public void CreatePlayerInfo(PlayerInfo playerInfo)
        {
            ClearDisplay?.Invoke();
            AppendDisplay?.Invoke($"Creating new {nameof(PlayerInfo)} for {playerInfo.UserId}");

            _ddbContext.SaveAsync(playerInfo, result =>
            {
                if (result.Exception == null)
                {
                    AppendDisplay?.Invoke($"{nameof(PlayerInfo)} saved");
                }
                else
                {
                    ErrorDisplay?.Invoke(result.Exception.ToString());
                }
            });
        }

        /// <summary>
        /// Scans all player info entries in database
        /// </summary>
        public void ScanEntries()
        {
            var request = new ScanRequest
            {
                TableName = TableName,
                ProjectionExpression = $"{nameof(PlayerInfo.UserId)}, {nameof(PlayerInfo.Initials)}, {nameof(PlayerInfo.HighScore)}"
            };
            ScanRequest(request);
        }

        /// <summary>
        /// Scans database for high scores bigger than zero
        /// </summary>
        public void HighScoreBiggerThanZero()
        {
            var request = new ScanRequest
            {
                TableName = TableName,
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> { { ":val", new AttributeValue { N = "0" } } },
                FilterExpression = $"{nameof(PlayerInfo.HighScore)} > :val",
                ProjectionExpression = $"{nameof(PlayerInfo.UserId)}, {nameof(PlayerInfo.Initials)}, {nameof(PlayerInfo.HighScore)}"
            };

            ScanRequest(request);
        }

        /// <summary>
        /// Scans request from database
        /// </summary>
        /// <param name="request">request to scan from database</param>
        private void ScanRequest(ScanRequest request)
        {
            ClearDisplay?.Invoke();
            _ddbClient.ScanAsync(request, result =>
            {
                if (result.Exception == null)
                {
                    foreach (var item in result.Response.Items)
                        DisplayItem(item);
                }
                else
                {
                    ErrorDisplay?.Invoke(result.Exception.ToString());
                }
            });
        }

        /// <summary>
        /// Displays column name and value
        /// </summary>
        /// <param name="attributeDict"></param>
        public void DisplayItem(Dictionary<string, AttributeValue> attributeDict)
        {
            var sb = new StringBuilder("************************************************");
            sb.Append(Environment.NewLine);

            foreach (var kvp in attributeDict)
            {
                sb.Append($"[{kvp.Key}]: ");
                sb.Append(GetAttributeValue(kvp.Value));
                sb.Append(Environment.NewLine);
            }

            sb.Append("************************************************");

            AppendDisplay?.Invoke(sb.ToString());
        }

        /// <summary>
        /// Retrieves set attribute value
        /// </summary>
        /// <param name="attribute">attribute</param>
        /// <returns>attribute value</returns>
        private string GetAttributeValue(AttributeValue attribute)
        {
            var str = string.Empty;

            if (attribute.S != null)
                str = $"{attribute.S}";
            else if (attribute.N != null)
                str = attribute.N;
            else if (attribute.SS != null)
                str = string.Join(",", attribute.SS.ToArray());
            else if (attribute.NS != null)
                str = string.Join(",", attribute.NS.ToArray());

            return str;
        }
    }
}