namespace EncounterForgeStandalone.Engine;

static class CrEngine
{
    static readonly Dictionary<string, int> XpMap = new()
    {
        ["0"] = 10, ["1/8"] = 25, ["1/4"] = 50, ["1/2"] = 100,
        ["1"] = 200, ["2"] = 450, ["3"] = 700, ["4"] = 1100,
        ["5"] = 1800, ["6"] = 2300, ["7"] = 2900, ["8"] = 3900,
        ["9"] = 5000, ["10"] = 5900, ["11"] = 7200, ["12"] = 8400,
        ["13"] = 10000, ["14"] = 11500, ["15"] = 13000
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
        return 6;
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
}
