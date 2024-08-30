using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasketballOlympics.Helpers;

public static class Converter
{
    public static int? ConvertToInt(char c)
    {
        return c switch
        {
            'A' => 1,
            'B' => 2,
            'C' => 3,
            'D' => 4,
            'E' => 5,
            'F' => 6,
            'G' => 7,
            _ => null
        };
    }

    public static char? ConvertToChar(int i)
    {
        return i switch
        {
            1 => 'A',
            2 => 'B',
            3 => 'C',
            4 => 'D',
            5 => 'E',
            6 => 'F',
            7 => 'G',
            _ => null
        };
    }

    public static string ConvertToRomanNumeral(int i)
    {
        if (i <= 0)
            return string.Empty;
        StringBuilder builder = new();

        int[] values = { 1, 4, 5 };
        string[] symbols = { "I", "IV", "V" };
        for (int j = 0; j < values.Length; j++)
        {
            while (i >= values[j])
            {
                builder.Append(symbols[j]);
                i -= values[j];
            }
        }
        return builder.ToString();
    }
}
