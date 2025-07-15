using System;

namespace AIEduChatbot.UnityReactBridge.Data
{
    /// <summary>
    /// User data structure that matches the React application's user interface
    /// </summary>
    [Serializable]
    public class UserData
    {
        public string id;
        public string email;
        public string name;
        public string type;
        public string image;
        public string cognitoIdToken;
        public long tokenExpiryTimestamp; // Unix timestamp in milliseconds

        public UserData()
        {
            // Default constructor for JSON deserialization
        }

        public UserData(string id, string email, string name = null, string type = "guest", string image = null, string cognitoIdToken = null, long tokenExpiryTimestamp = 0)
        {
            this.id = id;
            this.email = email;
            this.name = name;
            this.type = type;
            this.image = image;
            this.cognitoIdToken = cognitoIdToken;
            this.tokenExpiryTimestamp = tokenExpiryTimestamp;
        }

        /// <summary>
        /// Creates a default/guest user for fallback scenarios
        /// </summary>
        public static UserData CreateGuest()
        {
            return new UserData(
                id: "guest_" + System.DateTime.Now.Ticks,
                email: "guest@example.com",
                name: "Guest Player",
                type: "guest"
            );
        }

        /// <summary>
        /// Checks if the user has a valid Cognito ID token
        /// </summary>
        public bool HasCognitoIdToken()
        {
            return !string.IsNullOrEmpty(cognitoIdToken);
        }

        /// <summary>
        /// Checks if the Cognito ID token is expired based on the provided timestamp
        /// </summary>
        public bool IsTokenExpired()
        {
            if (!HasCognitoIdToken() || tokenExpiryTimestamp <= 0)
                return true;

            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return currentTime >= tokenExpiryTimestamp;
        }

        /// <summary>
        /// Gets the time remaining until token expiration in milliseconds
        /// </summary>
        public long GetTimeToExpiry()
        {
            if (!HasCognitoIdToken() || tokenExpiryTimestamp <= 0)
                return 0;

            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return Math.Max(0, tokenExpiryTimestamp - currentTime);
        }

        public override string ToString()
        {
            return $"UserData(id: {id}, email: {email}, name: {name}, type: {type}, hasCognitoToken: {HasCognitoIdToken()}, tokenExpired: {IsTokenExpired()})";
        }
    }
}