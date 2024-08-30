using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BasketballOlympics.Helpers;

namespace BasketballOlympics;

public class Simulator : ISimulator
{
    private readonly Standings _standings = new();
    private readonly Random _random = new();
    private DateOnly _currentDate = new(2024, 8, 11);
    private int _numberOfGamesPlayed = default;
    private const int _quarters = 4;
    private const int _averagePointsPerQuarter = 17;
    private int _teamPullingOutChance = 3;

    public void Simulate()
    {
        SimulateGroupPhase();
        SimulateQuarterfinals();
        SimulateFinals();
    }

    private void SimulateFinals()
    {
        var semifinalsGroups = _standings.InitSemifinals();

        List<Country> winners = [];
        List<Country> losers = [];

        Console.WriteLine("Polufinale:\n");

        var game1 = SimulateGame(semifinalsGroups.ElementAt(0), semifinalsGroups.ElementAt(1));
        var game2 = SimulateGame(semifinalsGroups.ElementAt(2), semifinalsGroups.ElementAt(3));

        PrintGamePlayed(game1);
        PrintGamePlayed(game2);

        foreach (var country in semifinalsGroups)
        {
            if (country.ISOCode == game1.WinningTeam || country.ISOCode == game2.WinningTeam)
            {
                winners.Add(country);
            }
            else
            {
                losers.Add(country);
            }
        }

        SimulateFinalGames(winners, losers);
    }

    private void SimulateFinalGames(List<Country> winners, List<Country> losers)
    {
        string bronze;
        string silver;
        string gold;

        Console.WriteLine("Utakmica za treće mesto:\n");

        var bronzeGame = SimulateGame(losers[0], losers[1]);

        PrintGamePlayed(bronzeGame);

        bronze = DetermineWinner(bronzeGame)[0];

        var goldGame = SimulateGame(winners[0], winners[1]);

        Console.WriteLine("Finale:\n");
        PrintGamePlayed(goldGame);

        var topMedalists = DetermineWinner(goldGame);

        silver = topMedalists[1];
        gold = topMedalists[0];

        Console.WriteLine("Medalje:\n");
        Console.WriteLine($"\t\t Zlato: {gold}");
        Console.WriteLine($"\t\t Srebro: {silver}");
        Console.WriteLine($"\t\t Bronza: {bronze}");
    }

    private void SimulateQuarterfinals()
    {
        // Removed the chance of teams collectivelly pulling out in the higher stages of the tournament.

        _teamPullingOutChance = 0;

        var (group1, group2) = _standings.InitEliminationPhase();
        Console.WriteLine("Ćetvrtfinale:\n");

        for (int i = 0; i < group1.Length - 1; i += 2)
        {
            var game = SimulateGame(group1[i], group1[i + 1]);
            PrintGamePlayed(game);

            if (i == group1.Length - 2)
            {
                Console.WriteLine("\t");
            }
        }

        for (int i = 0; i < group2.Length - 1; i += 2)
        {
            var game = SimulateGame(group2[i], group2[i + 1]);
            PrintGamePlayed(game);
        }
    }

    private void SimulateGroupPhase()
    {
        string currentStage = string.Empty;

        for (int i = 1; i <= 3; i++)
        {
            currentStage = $"Grupna faza - {Converter.ConvertToRomanNumeral(i)} kolo:\n";
            Console.WriteLine(currentStage);

            // These are the initial pairings.

            int team1 = 0;
            int team2 = 1;
            int team3 = 2;
            int team4 = 3;

            for (int j = 1; j <= 3; j++)
            {
                // When going through the second iteration and up switch teams around to ensure each team plays every other team in their groups without meeting twice.

                switch (i)
                {
                    case 2:
                        {
                            team2 = 2;
                            team3 = 1;
                            team4 = 3;
                        }
                        break;
                    case 3:
                        {
                            team2 = 3;
                            team3 = 2;
                            team4 = 1;
                        }
                        break;
                    default:
                        break;
                }
                Console.WriteLine($"\tGrupa {Converter.ConvertToChar(j)}");

                var group = _standings.GroupRankings(false)[j];

                var game1 = SimulateGame(group.ElementAt(team1), group.ElementAt(team2));
                var game2 = SimulateGame(group.ElementAt(team3), group.ElementAt(team4));

                PrintGamePlayed(game1);

                PrintGamePlayed(game2);
            }
        }

        var standings = _standings.GroupRankings(true);

        Console.WriteLine("Konačan plasman u grupama:\n");

        for (int n = 1; n <= 3; n++)
        {
            var group = standings[n];
            Console.WriteLine(
                $"\tGrupa {Converter.ConvertToChar(n)} (Ime - pobede/porazi/bodovi/postignuti koševi/primljeni koševi/koš razlika)\n"
            );
            foreach (var country in group)
            {
                Console.WriteLine(
                    $"\t{country.Standing}. {country.Team} \t {country.Tally.Wins} / {country.Tally.Losses} /  {country.Tally.TournamentPoints} / {country.Tally.PointsGiven} / {country.Tally.PointsReceived} / {(Math.Sign(country.Tally.PointsDifferential) > 0 ? $"+{country.Tally.PointsDifferential}" : country.Tally.PointsDifferential)}"
                );
            }
        }
    }

    private Game SimulateGame(Country country1, Country country2)
    {
        if (_numberOfGamesPlayed % 4 == 0)
            _currentDate = _currentDate.AddDays(2);

        var team1PullingOut = _random.Next(1, 100) <= _teamPullingOutChance;
        var team2PullingOut = _random.Next(1, 100) <= _teamPullingOutChance;
        var countries = new Country[] { country1, country2 };

        // There have been cases where both teams pulled out, it doesn't make much sense so I've removed that feature.

        if (team1PullingOut is true && team2PullingOut is true)
        {
            team1PullingOut = !team1PullingOut;
            team2PullingOut = !team1PullingOut;
        }

        if (team1PullingOut || team2PullingOut)
        {
            // On pullout the loser doesn't gain any tournament points.

            var pulloutGame = new Game(_currentDate, country2.Team, "0-0", TeamName: country1.Team);

            if (team1PullingOut)
            {
                OnGamePullOut(pulloutGame, country1, country2);

                return pulloutGame;
            }
            else if (team2PullingOut)
            {
                OnGamePullOut(pulloutGame, country2, country1);

                return pulloutGame;
            }

            country1.GamesPlayed.Add(pulloutGame);
            country2.GamesPlayed.Add(pulloutGame);
        }

        int team1Points = 0;
        int team2Points = 0;

        for (int i = 1; i <= _quarters; i++)
        {
            var team1Odds = Calculator.OddsOfWinning(country1.PowerRanking, country2.PowerRanking);

            bool team1Wins = SimulateWinner(team1Odds);

            int winningPoints = QuarterPoints(true);
            int losingPoints = QuarterPoints(false);

            if (team1Wins)
            {
                team1Points += winningPoints;
                team2Points += losingPoints;
            }
            else
            {
                team1Points += losingPoints;
                team2Points += winningPoints;
            }

            // This factors in the chance for free throws per quarter.

            team1Points += _random.Next(0, 6);
            team2Points += _random.Next(0, 6);
        }

        // If it does come to a tie by the end of the fourth quarter overtime is simulated until one team wins.

        while (team1Points == team2Points)
        {
            var team1Odds = Calculator.OddsOfWinning(country1.PowerRanking, country2.PowerRanking);

            bool team1Wins = SimulateWinner(team1Odds);

            var (team1PointsToAdd, team2PointsToAdd) = SimulateOvertime(team1Wins);
            team1Points += team1PointsToAdd;
            team2Points += team2PointsToAdd;
        }

        var team1PointDifferential = team1Points - team2Points;
        var team2PointDifferential = team2Points - team1Points;

        bool team1Won = team1PointDifferential > team2PointDifferential;

        if (team1Won)
        {
            OnGameWin(country1, country2);
        }
        else
        {
            OnGameWin(country2, country1);
        }

        country1.Tally.PointsDifferential += team1PointDifferential;
        country2.Tally.PointsDifferential += team2PointDifferential;
        country1.Tally.PointsGiven += team1Points;
        country1.Tally.PointsReceived += team2Points;
        country2.Tally.PointsGiven += team2Points;
        country2.Tally.PointsReceived += team1Points;

        // Update power rankings according to the game result.

        country1.PowerRanking += Calculator.UpdatePowerRanking(
            country1.ISOCode,
            country2.ISOCode,
            team1PointDifferential,
            team1Won
        );
        country2.PowerRanking += Calculator.UpdatePowerRanking(
            country2.ISOCode,
            country1.ISOCode,
            team2PointDifferential,
            team1Won
        );

        var game = new Game(
            _currentDate,
            country2.Team,
            $"{team1Points}-{team2Points}",
            TeamName: country1.Team
        )
        {
            WinningTeam = team1Won ? country1.ISOCode : country2.ISOCode
        };

        country1.GamesPlayed.Add(game);
        country2.GamesPlayed.Add(game);

        country1.OpponentsFaced.Add(country2.Team);
        country2.OpponentsFaced.Add(country1.Team);
        _numberOfGamesPlayed++;

        return game;
    }

    private bool SimulateWinner(decimal odds)
    {
        int randomNumber = _random.Next(1, 100);
        return randomNumber <= odds;
    }

    private (int, int) SimulateOvertime(bool team1Wins)
    {
        int team1Points = 0;
        int team2Points = 0;
        int winningPoints = Math.Abs(QuarterPoints(true) / 2);
        int losingPoints = Math.Abs(QuarterPoints(false) / 2);
        if (team1Wins)
        {
            team1Points += winningPoints;
            team2Points += losingPoints;
        }
        else
        {
            team1Points += losingPoints;
            team2Points += winningPoints;
        }
        return (team1Points, team2Points);
    }

    private static void OnGameWin(Country country1, Country country2)
    {
        country1.Tally.Wins++;
        country1.Tally.TournamentPoints += 2;
        country2.Tally.TournamentPoints += 1;
        country2.Tally.Losses++;
    }

    private static void OnGamePullOut(Game pulloutGame, Country country1, Country country2)
    {
        pulloutGame.Pullout = $"{country1.Team} predao/la utakmicu.";
        pulloutGame.WinningTeam = country2.Team;
        country1.Tally.Losses++;
        country2.Tally.Wins++;
        country2.Tally.TournamentPoints += 2;
    }

    private int QuarterPoints(bool won)
    {
        return won
            ? _averagePointsPerQuarter + _random.Next(2, 7)
            : _averagePointsPerQuarter - _random.Next(0, 5);
        ;
    }

    private static void PrintGamePlayed(Game game)
    {
        Console.WriteLine(
            $"\t\t {game.TeamName} - {game.Opponent} ({game.Result}) {(string.IsNullOrEmpty(game.Pullout) ? "" : $" ({game.Pullout})")}"
        );
    }

    private static string[] DetermineWinner(Game game)
    {
        var winnerLoser = new string[2];
        var score = game.Result.Split('-');

        if (Convert.ToInt32(score[0]) > Convert.ToInt32(score[1]))
        {
            winnerLoser[0] = game.TeamName;
            winnerLoser[1] = game.Opponent;
        }
        else
        {
            winnerLoser[0] = game.Opponent;
            winnerLoser[1] = game.TeamName;
        }

        return winnerLoser;
    }
}
