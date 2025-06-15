using Microsoft.AspNetCore.Mvc;
using DutchBlitzBackend.Models;
using System.Collections.Concurrent;
using System;
using System.Linq;

namespace DutchBlitzBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GameController : ControllerBase
    {

        private static ConcurrentDictionary<string, GameData> Games = new();

        [HttpPost("create")]
        public IActionResult CreateGame([FromBody] CreateGameRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.GameId))
                return BadRequest("GameId is required");

            if (Games.ContainsKey(request.GameId))
                return Conflict("Game with this ID already exists");

            if (string.IsNullOrWhiteSpace(request.HostPlayerId) || string.IsNullOrWhiteSpace(request.HostPlayerName))
                return BadRequest("Host player ID and name are required");

            var hostPlayer = new Player
            {
                Id = request.HostPlayerId,
                Name = request.HostPlayerName,
                Score = 0,
                IsReady = true
            };

            var game = new GameData
            {
                GameId = request.GameId,
                HostId = hostPlayer.Id,
                Players = new List<Player> { hostPlayer },
                ScoreLimit = request.ScoreLimit,
            };

            Games[request.GameId] = game;
            Console.WriteLine($"Game created: {game.GameId} by {hostPlayer.Name}: {hostPlayer.Id}");
            return Ok(game);
        }

        [HttpDelete("delete-player/{gameId}/{playerId}")]
        public IActionResult LeaveGame(string gameId, string playerId)
        {
            if (!Games.TryGetValue(gameId, out GameData? game)) 
                return NotFound("Game not found");
            var player = game.Players.First(p => p.Id == playerId);
            if (player == null)
                return NotFound("Player not found ");
            game.Players.Remove(player);
            if (game.Players.Count == 0)
                Games.TryRemove(gameId, out _); 
            return Ok(game);
        }

        [HttpDelete("delete/{gameid}")]
        public IActionResult DeleteGame(string gameid)
        {
            if (!Games.TryRemove(gameid, out GameData? game)) 
                return NotFound("Game not found");
            return Ok(game);
        }

        [HttpPost("join/{gameId}")]
        public IActionResult JoinGame(string gameId, [FromBody] Player player)
        {
            if (!Games.TryGetValue(gameId, out GameData? game)) 
                return NotFound("Game not found");

            //check if player exists
            if (game.Players.Any(p => p.Id == player.Id))
                return BadRequest("Player already exists in the game");

           

            game.Players.Add(player);
            return Ok(player);
        }

        [HttpPost("toggle-ready/{gameId}/{playerId}")]
        public IActionResult ToggleReady(string gameId, string playerId)
        {
            if (!Games.TryGetValue(gameId, out GameData? game)) 
                return NotFound();

            var player = game.Players.FirstOrDefault(p => p.Id == playerId);
            if (player == null)
                return NotFound();

            player.IsReady = !player.IsReady;
            return Ok(player);
        }

        [HttpPost("start/{gameId}")]
        public IActionResult StartGame(string gameId)
        {
            if (!Games.TryGetValue(gameId, out GameData? game))
                return NotFound();

            if (!game.Players.All(p => p.IsReady))
                return BadRequest("Not all players are ready");

            game.GameStatus = true;
            return Ok();
        }

        [HttpPost("add-round/{gameId}")]
        public IActionResult AddRound(string gameId)
        {
            if (!Games.TryGetValue(gameId, out GameData? game))
                return NotFound();
            var round = new Round
            {
                Id = Guid.NewGuid().ToString(),
                CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
            game.Rounds.Add(round);
            return Ok(round);
        }


        [HttpPost("submit-score/{gameId}")]
        public IActionResult SubmitScore(string gameId, [FromBody] PlayerScore score)
        {
            if (!Games.TryGetValue(gameId, out GameData? game)) 
                return NotFound();

            var round = game.Rounds.LastOrDefault() ?? new Round
            {
                Id = Guid.NewGuid().ToString(),
                CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            if (!game.Rounds.Contains(round))
                game.Rounds.Add(round);

            round.Scores.RemoveAll(s => s.PlayerId == score.PlayerId);
            round.Scores.Add(score);

            return Ok(round);
        }

        [HttpGet("{gameId}")]
        public IActionResult GetGame(string gameId)
        {
            if (!Games.TryGetValue(gameId, out GameData? game)) 
                return NotFound();

            return Ok(game);
        }

        [HttpGet("GetAllGames")]
        public IActionResult GetAllGames()
        {
            return Ok(Games);

        }

        [HttpPatch("update-name")]
        public IActionResult UpdatePlayerName([FromBody] UpdatePlayerName data)
        {
            var game = Games.Values.FirstOrDefault(g => g.Players.Any(p => p.Id == data.Id));
            if (game == null)
                return NotFound("Game not found");
            var player = game.Players.FirstOrDefault(p => p.Id == data.Id);
            if (player == null)
                return NotFound("Player not found");
            player.Name = data.Name;
            return Ok(player.Name);

        }

        [HttpPost("round-done/{gameId}/{roundId}/{playerId}")]
        public IActionResult MarkPlayerDone(string gameId, string roundId, string playerId)
        {
            if (!Games.TryGetValue(gameId, out GameData? game))
                return NotFound("Game not found");

            var round = game.Rounds.FirstOrDefault(r => r.Id == roundId);
            if (round == null)
                return NotFound("Round not found");

            var score = round.Scores.FirstOrDefault(s => s.PlayerId == playerId);
            if (score == null)
                return NotFound("Score not found for this player");

            score.IsDone = true;
            return Ok(score);
        }

        [HttpGet("round-ready/{gameId}/{roundId}")]
        public IActionResult IsRoundReady(string gameId, string roundId)
        {
            if (!Games.TryGetValue(gameId, out GameData? game))
                return NotFound("Game not found");

            var round = game.Rounds.FirstOrDefault(r => r.Id == roundId);
            if (round == null)
                return NotFound("Round not found");

            bool allDone = game.Players.All(p =>
                round.Scores.Any(s => s.PlayerId == p.Id && s.IsDone)
            );

            return Ok(new { allDone });
        }

        [HttpPost("complete-round/{gameId}/{roundId}")]
        public IActionResult CompleteRound(string gameId, string roundId)
        {
            if (!Games.TryGetValue(gameId, out GameData? game))
                return NotFound("Game not found");

            var round = game.Rounds.FirstOrDefault(r => r.Id == roundId);
            if (round == null)
                return NotFound("Round not found");

            bool allDone = game.Players.All(p =>
                round.Scores.Any(s => s.PlayerId == p.Id && s.IsDone)
            );

            if (!allDone)
                return BadRequest("Not all players have completed the round");

            round.isRoundDone = true;

            foreach (var ps in round.Scores)
            {
                var player = game.Players.FirstOrDefault(p => p.Id == ps.PlayerId);
                if (player != null)
                {
                    player.Score += ps.Score.Total;
                }
            }

            return Ok(round);
        }

        [HttpGet("round-status/{gameId}/{roundId}")]
        public IActionResult GetRoundStatus(string gameId, string roundId)
        {
            if (!Games.TryGetValue(gameId, out GameData? game))
                return NotFound("Game not found");

            var round = game.Rounds.FirstOrDefault(r => r.Id == roundId);
            if (round == null)
                return NotFound("Round not found");

            var status = game.Players.Select(p =>
            {
                var score = round.Scores.FirstOrDefault(s => s.PlayerId == p.Id);
                return new
                {
                    PlayerId = p.Id,
                    PlayerName = p.Name,
                    Submitted = score != null,
                    IsDone = score?.IsDone ?? false,
                    Total = score?.Score.Total ?? 0
                };
            });

            return Ok(status);
        }







    }

}
