﻿namespace Shared.Leaderboard
{
    public class LeaderboardRecord
    {
        public string Username
        {
            get; private set;
        }

        public float TotalAssetValue
        {   
            get; private set;
        }

        public LeaderboardRecord(string username, float totalAssetValue)
        {
            Username = username;
            TotalAssetValue = totalAssetValue;
        }
    }
}