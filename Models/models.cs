using System;
using System.Collections.Generic;

namespace DutchBlitzBackend.Models
{
    public class Player
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public int Score { get; set; }
        public bool IsReady { get; set; } = false;
    }

    public class UpdatePlayerName
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
    }

    public class RoundScore
    {
        public int Dutch { get; set; }
        public int Blitz { get; set; }
        public int Total { get; set; }
    }

    public class Round
    {
        public required string Id { get; set; }
        public long CreatedAt { get; set; }
        public bool isRoundDone { get; set; }
        public List<PlayerScore> Scores { get; set; } = new();
    }

    public class PlayerScore
    {
        public required string PlayerId { get; set; }
        public required RoundScore Score { get; set; }
        public bool IsDone { get; set; } = false; // NEW
    }

    public class GameData
    {
        public required string GameId { get; set; }
        public bool GameStatus { get; set; }
        public List<Player> Players { get; set; } = new();
        public List<Round> Rounds { get; set; } = new();
        public int ScoreLimit { get; set; } = 75;
        public required string HostId { get; set; }
    }

    public class CreateGameRequest
    {
        public required string GameId { get; set; }
        public required string HostPlayerId { get; set; }
        public required string HostPlayerName { get; set; }
        public int ScoreLimit { get; set; } = 75;
    }
}