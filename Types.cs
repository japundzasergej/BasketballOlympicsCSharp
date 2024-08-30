using System.Text.Json;
using System.Text.Json.Serialization;
using BasketballOlympics.Helpers;

namespace BasketballOlympics;

public sealed record Country(string Team, string ISOCode, int FIBARanking)
{
    public Tally Tally { get; init; } = new();
    public int Standing { get; set; }
    public List<Game> GamesPlayed { get; init; } = [];
    public List<string> OpponentsFaced { get; init; } = [];
    public int PowerRanking { get; set; }
};

public sealed record Game(DateOnly Date, string Opponent, string Result, string TeamName = "")
{
    public string Pullout { get; set; } = string.Empty;
    public string WinningTeam { get; set; } = string.Empty;
};

public sealed record Tally
{
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int TournamentPoints { get; set; }
    public int PointsGiven { get; set; }
    public int PointsReceived { get; set; }
    public int PointsDifferential { get; set; }
}

public sealed class Standings
{
    private readonly Dictionary<int, IEnumerable<Country>> _groupStandings = [];
    private readonly Dictionary<int, IEnumerable<Country>> _quarterFinalsStandings = [];
    private readonly Dictionary<int, IEnumerable<Country>> _semifinalsStandings = [];
    private readonly Dictionary<int, IEnumerable<Country>> _finalStandings = [];
    private readonly Random _random = new();
    private List<Country> _overallStandingsAfterGroups = [];

    public Standings()
    {
        // Base directory starts in bin/debug/net8.0, this just provides a cleaner way to access the Data folder containing relevant json files.

        var directory = AppContext.BaseDirectory.Split(Path.DirectorySeparatorChar);
        var slice = new ArraySegment<string>(directory, 0, directory.Length - 4);
        var path = Path.Combine([.. slice]);

        var tempGroups = Parser.GroupParser(Path.Combine(path, "Data", "groups.json"));
        var tempGames = Parser.GamesParser(Path.Combine(path, "Data", "exibitions.json"));

        // Converts chars parsed from the json file that signifies group names to integers for easier future iterations.

        foreach (var key in tempGroups.Keys)
        {
            int intKey =
                Converter.ConvertToInt(key)
                ?? throw new InvalidOperationException("Invalid group name.");
            _groupStandings[intKey] = [.. tempGroups[key]];
        }

        // Two separate nested for loops is never a good idea, but working with a fixed sample size such as for a basketball tournament where we're sure there's only 12 different teams so the number of iterations is going to be constantly 3 * 4, with 3 being the number of groups and 4 being the number of teams in the group, doesn't take much computing power.

        for (int i = 1; i <= 3; i++)
        {
            foreach (var country in _groupStandings[i])
            {
                PowerRankingConstants.InitRankings(country);
            }
        }

        for (int i = 1; i <= 3; i++)
        {
            foreach (var country in _groupStandings[i])
            {
                country.PowerRanking = Calculator.CalculateInitialPowerRanking(
                    tempGames[country.ISOCode],
                    country.ISOCode
                );
            }
        }
    }

    private void UpdateStandings()
    {
        _overallStandingsAfterGroups =
        [
            .. _overallStandingsAfterGroups.OrderByDescending(c => c, new TeamRankingComparer())
        ];
        RankCountries(_overallStandingsAfterGroups);
    }

    public IEnumerable<Country> InitSemifinals()
    {
        List<Country> semifinalsGroups = [];

        for (int i = 1; i <= 2; i++)
        {
            var countries = _semifinalsStandings[i];

            // Separates the winners of the quarterfinals from the already initialized semifinals groups.

            foreach (var country in countries)
            {
                bool isWinner = default;

                var lastGame = country.GamesPlayed[^1];
                if (lastGame.WinningTeam == country.ISOCode)
                    isWinner = true;

                if (isWinner == true)
                    semifinalsGroups.Add(country);
            }
        }
        return semifinalsGroups;
    }

    private void SegregateTeamsForSemifinals()
    {
        int key = 1;

        // Creates two groups for semifinals, one group made from concatenating Group D with Group E and another from Group F and Group G

        for (int i = 4; i < 7; i += 2)
        {
            var countriesA = _quarterFinalsStandings[i];
            var countriesB = _quarterFinalsStandings[i + 1];
            _semifinalsStandings[key] = [.. countriesA.Concat(countriesB)];
            if (i < 6)
                key++;
        }
    }

    public (Country[], Country[]) InitEliminationPhase()
    {
        EliminationGamesTeamSegregation();

        // First group is selected by concatenating group D and group G and the second by concantenating group E and group F.

        IEnumerable<Country> group1 =
        [
            .. _quarterFinalsStandings[4].Concat(_quarterFinalsStandings[7])
        ];

        IEnumerable<Country> group2 =
        [
            .. _quarterFinalsStandings[5].Concat(_quarterFinalsStandings[6])
        ];

        // Afterwards choose random pairings from aformentioned groups between two teams that haven't already played each other. Zero and first index representing a random team from the first and second group and respectively second and third index representing the other pairing.

        var finalGroup1 = EliminationPhasePicker([.. group1]);
        var finalGroup2 = EliminationPhasePicker([.. group2]);

        // The structure of the elimination phase is displayed here:

        Console.WriteLine("Eliminaciona faza:\n");

        for (int i = 0; i <= 2; i++)
        {
            if (i == 1)
                i++;
            var team1 = finalGroup1.ElementAt(i).Team;
            var team2 = finalGroup1.ElementAt(i + 1).Team;
            Console.WriteLine($"\t\t{team1} - {team2}");
            if (i == 2)
            {
                Console.WriteLine("\n");
            }
        }

        for (int i = 0; i <= 2; i++)
        {
            if (i == 1)
                i++;

            var team1 = finalGroup2.ElementAt(i).Team;
            var team2 = finalGroup2.ElementAt(i + 1).Team;
            Console.WriteLine($"\t\t{team1} - {team2}");
            if (i == 1)
            {
                Console.WriteLine("\n");
            }
        }

        // Teams are immediately segregated for semifinals by picking teams from group D and E representing the first semifinals pairing, and F and G for the second pairing. Losers are eliminated from the groups after the quarterfinals are simulated and the winners will face each other in the semifinals

        SegregateTeamsForSemifinals();

        return ([.. finalGroup1], [.. finalGroup2]);
    }

    private IEnumerable<Country> EliminationPhasePicker(List<Country> group)
    {
        var finalGroup = new Country[group.Count];

        // First picks a random index between 0 and 1 to select teams from their group and then pairs them with a random team from the opposite group.

        int firstIndex = _random.Next(0, 2);
        int secondIndex = _random.Next(2, 4);

        finalGroup[0] = group[firstIndex];
        finalGroup[1] = group[secondIndex];

        // Third and fourth index are always the opposite value of the first and second.

        int thirdIndex = firstIndex == 0 ? 1 : 0;
        int fourthIndex = secondIndex == 2 ? 3 : 2;

        finalGroup[2] = group[thirdIndex];
        finalGroup[3] = group[fourthIndex];

        var testing1 = PlayedAgainstEachOther(finalGroup[0], finalGroup[1]);
        var testing2 = PlayedAgainstEachOther(finalGroup[2], finalGroup[3]);

        if (PlayedAgainstEachOther(finalGroup[0], finalGroup[1]))
        {
            finalGroup[1] = group[fourthIndex];
            finalGroup[3] = group[secondIndex];
        }

        // If after the check the teams have already played each other (this does happen ocassionally in my simulation), teams will play their counterparts in their group.

        if (
            PlayedAgainstEachOther(finalGroup[0], finalGroup[1])
            || PlayedAgainstEachOther(finalGroup[2], finalGroup[3])
        )
        {
            finalGroup[0] = group[0];
            finalGroup[1] = group[1];
            finalGroup[2] = group[2];
            finalGroup[3] = group[3];
        }

        return finalGroup;
    }

    // Recursive approach that doesn't quite work.

    //private IEnumerable<Country> EliminationPhasePicker(
    //    List<Country> group,
    //    Country[] finalGroup,
    //    int maxRecursionDepth = 4
    //)
    //{

    //    if (maxRecursionDepth <= 0)
    //    {
    //        return finalGroup;
    //    }

    //    if (finalGroup.Length == 4 && finalGroup.All(c => c != null))
    //    {
    //        if (
    //            !PlayedAgainstEachOther(finalGroup[0], finalGroup[1])
    //            && !PlayedAgainstEachOther(finalGroup[2], finalGroup[3])
    //        )
    //        {
    //            return finalGroup;
    //        }
    //    }

    //    int firstIndex = _random.Next(0, 2);
    //    int secondIndex = _random.Next(2, 4);

    //    finalGroup[0] = group[firstIndex];
    //    finalGroup[1] = group[secondIndex];

    //    int thirdIndex = firstIndex == 0 ? 1 : 0;
    //    int fourthIndex = secondIndex == 2 ? 3 : 2;

    //    finalGroup[2] = group[thirdIndex];
    //    finalGroup[3] = group[fourthIndex];

    //    return EliminationPhasePicker(group, finalGroup, maxRecursionDepth - 1);
    //}

    private static bool PlayedAgainstEachOther(Country country1, Country country2)
    {
        return country1.OpponentsFaced.Contains(country2.Team);
    }

    private void EliminationGamesTeamSegregation()
    {
        UpdateStandings();

        int previousStandings = 0;
        int standings = 2;

        Console.WriteLine("Šeširi:\n");

        // Teams in the elimination phase are selected by their overall rank compared to other teams across all groups after the group stages are finalized. Teams that have a standing between 1 and 2 will be selected for group D, between 3 and 4 for group E and soforth up untill group G is formed and the total numbers of teams remaining will be the 8 best teams in the tournament.

        for (int i = 4; i <= 7; i++)
        {
            var countries = _overallStandingsAfterGroups.Where(c =>
                c.Standing > previousStandings && c.Standing <= standings
            );

            _quarterFinalsStandings[i] = [.. countries];

            Console.WriteLine($"\t Šešir {Converter.ConvertToChar(i)}:\n");
            Console.WriteLine($"\t\t{countries.ElementAt(0).Team}");
            Console.WriteLine($"\t\t{countries.ElementAt(1).Team}");
            previousStandings += 2;
            standings += 2;
        }
    }

    public Dictionary<int, IEnumerable<Country>> GroupRankings(bool final)
    {
        if (final)
        {
            SortGroups();
            return _groupStandings;
        }
        return _groupStandings;
    }

    private void SortGroups()
    {
        for (int i = 1; i <= 3; i++)
        {
            var group = _groupStandings[i];

            group = group.OrderByDescending(c => c, new TeamRankingComparer());

            // After the group stage is finalized all the countries will be added to the overall ranking collection which is empty untill this point.

            foreach (var country in group)
            {
                _overallStandingsAfterGroups.Add(country);
            }

            RankCountries(group);

            _groupStandings[i] = group;
        }
    }

    private static void RankCountries(IEnumerable<Country> countries)
    {
        int rank = 1;
        foreach (var country in countries)
        {
            country.Standing = rank;
            rank++;
        }
    }
}
