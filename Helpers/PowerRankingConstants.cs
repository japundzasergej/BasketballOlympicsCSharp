namespace BasketballOlympics.Helpers;

public static class PowerRankingConstants
{
    public const int RegularWinPoints = 2;
    public const int BlowoutWinBonus = 3;
    public const int RegularLossPoints = -2;
    public const int BlowoutLossPenalty = -3;
    public const int BaseRanking = 60;

    private static readonly Dictionary<string, int> _countryRankings = [];

    public static void InitRankings(Country country)
    {
        _countryRankings[country.ISOCode] = country.FIBARanking;
    }

    public static int IndexRankings(string ISOCode)
    {
        return _countryRankings.GetValueOrDefault(ISOCode, 99);
    }
}
