namespace EncounterForgeStandalone.Engine;

static class CrEngine
{
    static readonly Dictionary<string, int> XpMap = new()
    {
        ["0"] = 10, ["1/8"] = 25, ["1/4"] = 50, ["1/2"] = 100,
        ["1"] = 200, ["2"] = 450, ["3"] = 700, ["4"] = 1100,
        ["5"] = 1800, ["6"] = 2300, ["7"] = 2900, ["8"] = 3900,
        ["9"] = 5000, ["10"] = 5900, ["11"] = 7200, ["12"] = 8400,
        ["13"] = 10000, ["14"] = 11500, ["15"] = 13000,
        ["16"] = 15000, ["17"] = 18000, ["18"] = 20000, ["19"] = 22000, ["20"] = 25000,
        ["21"] = 33000, ["22"] = 41000, ["23"] = 50000, ["24"] = 62000, ["25"] = 75000,
        ["26"] = 90000, ["27"] = 105000, ["28"] = 120000, ["29"] = 135000, ["30"] = 155000
    };

    public static double ToNumber(string cr) => cr switch
    {
        "1/8" => 0.125,
        "1/4" => 0.25,
        "1/2" => 0.5,
        _ => double.TryParse(cr, out var n) ? n : 0
    };

    public static int GetXp(string cr) => XpMap.TryGetValue(cr, out var xp) ? xp : 0;

    public static int GetProfBonus(string cr)
    {
        var n = ToNumber(cr);
        if (n < 5) return 2;
        if (n < 9) return 3;
        if (n < 13) return 4;
        if (n < 17) return 5;
        if (n < 21) return 6;
        if (n < 25) return 7;
        if (n < 29) return 8;
        return 9;
    }

    public static int GetTier(string cr)
    {
        var n = ToNumber(cr);
        if (n <= 1) return 1;
        if (n <= 4) return 2;
        if (n <= 7) return 3;
        if (n <= 10) return 4;
        if (n <= 13) return 5;
        return 6;
    }

    public static string ToDisplay(string cr) => cr;

    // Returns the largest CR at or below half of the given CR for summons
    static readonly double[] CrTable = [0, 0.125, 0.25, 0.5, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30];

    public static string HalfCr(string cr)
    {
        var n = ToNumber(cr);
        var half = n / 2.0;
        var floor = CrTable.Where(c => c <= half).Last();
        return floor switch
        {
            0.125 => "1/8",
            0.25 => "1/4",
            0.5 => "1/2",
            _ => ((int)floor).ToString()
        };
    }
}
