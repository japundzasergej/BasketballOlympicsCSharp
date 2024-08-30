using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasketballOlympics.Helpers;

public class TeamRankingComparer : IComparer<Country>
{
    public int Compare(Country? x, Country? y)
    {
        // Initially compare the tournament points total for each team.

        int result = x.Tally.TournamentPoints.CompareTo(y.Tally.TournamentPoints);

        // If one or more teams have the same number of points we check if the teams have played against each other in the group stages and based on that the victor is ranked higher.

        if (result == 0)
        {
            var gamePlayed = x.GamesPlayed.FirstOrDefault(g =>
                g.Opponent == y.Team || g.TeamName == y.Team
            );

            // If teams haven't played each other lastly we check the points differentials.

            if (gamePlayed is null)
            {
                result = x.Tally.PointsDifferential.CompareTo(y.Tally.PointsDifferential);
                if (result == 0)
                {
                    result = x.Tally.PointsGiven.CompareTo(y.Tally.PointsGiven);
                }
            }
            else
            {
                result = CompareHeadToHead(x.ISOCode, gamePlayed.WinningTeam);
            }
        }
        return result;
    }

    private static int CompareHeadToHead(string team1ISOCode, string winner)
    {
        return team1ISOCode == winner ? 1 : -1;
    }
}
