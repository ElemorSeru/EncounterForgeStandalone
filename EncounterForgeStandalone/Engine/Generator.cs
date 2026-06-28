using EncounterForgeStandalone.Models;

namespace EncounterForgeStandalone.Engine;

static class Generator
{
    public static EncounterResult Generate(int playerCount, int playerLevel, int enemyCount,
        string difficulty, string theme, bool solo, int intensityOffset = 0, bool dprFirst = false)
    {
        var pCount = Math.Clamp(playerCount, 1, 8);
        var pLevel = Math.Clamp(playerLevel, 1, 20);
        var count = solo ? 1 : Math.Clamp(enemyCount, 1, 10);

        var party = CombatEstimator.EstimatePartyGeneric(pCount, pLevel);
        var envelope = CombatEstimator.ComputeEnvelope(party, difficulty, count, solo, intensityOffset);
        var targetCr = CombatEstimator.NearestCrForStats(envelope.PerEnemyHp, envelope.PerEnemyDpr);

        var results = new List<CreatureResult>();
        for (int i = 0; i < count; i++)
        {
            var creature = CreatureAssembler.Assemble(targetCr, theme, null, solo,
                dprFirst: dprFirst, targetDpr: envelope.PerEnemyDpr);
            Calibrator.Calibrate(creature, envelope.PerEnemyHp, envelope.PerEnemyDpr);
            var profile = CombatEstimator.EstimateCreatureProfile(creature);
            results.Add(new CreatureResult(creature.Name, creature.Cr, profile, creature));

            // If the creature has a summon ability, generate a companion at half CR with 40% resources.
            if (creature.Actions.Any(a => a.Id == "summon_lesser"))
            {
                var summonCr = CrEngine.HalfCr(targetCr);
                var summon = CreatureAssembler.Assemble(summonCr, theme, null, false, isSummon: true,
                    dprFirst: dprFirst, targetDpr: envelope.PerEnemyDpr * 0.4);
                Calibrator.Calibrate(summon, envelope.PerEnemyHp * 0.4, envelope.PerEnemyDpr * 0.4);
                summon.Name = $"{creature.Name}'s Summon";
                var summonProfile = CombatEstimator.EstimateCreatureProfile(summon);
                results.Add(new CreatureResult(summon.Name, summon.Cr, summonProfile, summon));
            }
        }

        var groupHp = results.Sum(r => r.Profile.Hp);
        var groupDpr = results.Sum(r => r.Profile.Dpr);
        var rounds = CombatEstimator.EstimateRounds(party, groupHp, groupDpr);
        var outcome = CombatEstimator.EstimateOutcome(rounds);

        return new EncounterResult(results, party, rounds, outcome, pCount, pLevel, count, theme, solo, difficulty, intensityOffset, dprFirst);
    }

    public static ReadoutData ComputeReadout(int playerCount, int playerLevel, int enemyCount,
        string difficulty, bool solo, int intensityOffset = 0)
    {
        var pCount = Math.Clamp(playerCount, 1, 8);
        var pLevel = Math.Clamp(playerLevel, 1, 20);
        var count = solo ? 1 : Math.Clamp(enemyCount, 1, 10);

        var party = CombatEstimator.EstimatePartyGeneric(pCount, pLevel);
        var envelope = CombatEstimator.ComputeEnvelope(party, difficulty, count, solo, intensityOffset);
        var targetCr = CombatEstimator.NearestCrForStats(envelope.PerEnemyHp, envelope.PerEnemyDpr);
        var rounds = CombatEstimator.EstimateRounds(party, envelope.GroupHp, envelope.GroupDpr);
        var outcome = CombatEstimator.EstimateOutcome(rounds);

        return new ReadoutData(
            CrEngine.ToDisplay(targetCr),
            envelope.GroupDpr, (int)Math.Round(envelope.GroupHp),
            party.Dpr, (int)Math.Round(party.Hp),
            rounds.RoundsToDefeat, rounds.RoundsToThreaten,
            outcome);
    }
}

public record ReadoutData(
    string TargetCr,
    double EnemyDpr, int EnemyHp,
    double PartyDpr, int PartyHp,
    double RoundsDefeat, double RoundsThreaten,
    string Outcome);
