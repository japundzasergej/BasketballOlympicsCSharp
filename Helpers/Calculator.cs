using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BasketballOlympics;

namespace BasketballOlympics.Helpers;

public static class Calculator
{
    public static int CalculateInitialPowerRanking(IEnumerable<Game> games, string ISOCode)
    {
        // The ranking when initialized is calculated according to the quality of the opposing teams in the friendly matches leading up to the tournament. It's a range from 10 to a 100.

        int fibaRanking = PowerRankingConstants.IndexRankings(ISOCode);
        int powerRanking = PowerRankingConstants.BaseRanking - (fibaRanking * 2);

        foreach (var game in games)
        {
            var opponentRanking = PowerRankingConstants.IndexRankings(game.Opponent);
            ;
            var result = game.Result.Split('-');

            int pointsDifferential = Convert.ToInt32(result[0]) - Convert.ToInt32(result[1]);

            bool gameWon = Math.Sign(pointsDifferential) > 0;

            powerRanking += CalculatePointDifferential(
                fibaRanking,
                opponentRanking,
                pointsDifferential,
                gameWon
            );
        }

        return Math.Clamp(powerRanking, 10, 100);
    }

    public static int UpdatePowerRanking(
        string country1ISOCode,
        string country2ISOCode,
        int pointsDifferential,
        bool gameWon
    )
    {
        var result = CalculatePointDifferential(
            PowerRankingConstants.IndexRankings(country1ISOCode),
            PowerRankingConstants.IndexRankings(country2ISOCode),
            pointsDifferential,
            gameWon
        );
        return Math.Clamp(result, 10, 100);
    }

    private static int CalculatePointDifferential(
        int teamRanking,
        int opponentRanking,
        int pointsDifferential,
        bool gameWon
    )
    {
        int pointsGained = 0;

        // Blowout wins against higher ranked opponents net more power ranking points and vice versa for defeats.

        switch (Math.Sign(pointsDifferential))
        {
            case 1 when pointsDifferential >= 10:
                pointsGained += PowerRankingConstants.BlowoutWinBonus;
                break;
            case 1:
                pointsGained += PowerRankingConstants.RegularWinPoints;
                break;
            case -1 when pointsDifferential <= -10:
                pointsGained += PowerRankingConstants.BlowoutLossPenalty;
                break;
            case -1:
                pointsGained += PowerRankingConstants.RegularLossPoints;
                break;
        }

        if (gameWon && teamRanking < opponentRanking)
        {
            pointsGained += PowerRankingConstants.RegularWinPoints;
        }
        else if (!gameWon && teamRanking > opponentRanking)
        {
            pointsGained += PowerRankingConstants.RegularLossPoints;
        }

        return pointsGained;
    }

    public static decimal OddsOfWinning(int powerRanking1, int powerRanking2)
    {
        // Odds of winning a quarter are calculated based primarily on the power ranking differential, after calculating the base odds the more powerful teams are granted bonus points while the less powerful teams are subtracted a small amount.

        int powerRankingDifference = powerRanking1 - powerRanking2;

        decimal odds =
            Math.Sign(powerRankingDifference) != -1
                ? 100 - powerRankingDifference + 10
                : (100 - (100 + powerRankingDifference)) - 7;

        odds = Math.Clamp(odds, 10, 100);
        return odds;
    }
}
