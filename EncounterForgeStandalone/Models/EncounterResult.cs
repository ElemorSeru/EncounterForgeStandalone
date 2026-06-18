namespace EncounterForgeStandalone.Models;

public record PartyEstimate(double Dpr, double Hp);

public record CreatureProfile(double Dpr, int Hp, int Ac);

public record EncounterRounds(double RoundsToDefeat, double RoundsToThreaten);

public record CreatureResult(string Name, string Cr, CreatureProfile Profile, Creature Creature);

public record EncounterResult(
    List<CreatureResult> Results,
    PartyEstimate Party,
    EncounterRounds Rounds,
    string Outcome,
    int PlayerCount,
    int PlayerLevel,
    int EnemyCount);
