using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using EncounterForgeStandalone.Engine;

namespace EncounterForgeStandalone.Export;

static class PdfExporter
{
    public static void Export(EncounterResult result, string path)
    {
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(44);
                page.PageColor(Colors.White);
                page.Content().Column(col =>
                {
                    foreach (var r in result.Results)
                    {
                        col.Item().Component(new StatBlockComponent(r.Creature));
                        col.Item().PaddingBottom(28);
                    }
                });
            });
        });

        doc.GeneratePdf(path);
    }
}

class StatBlockComponent : IComponent
{
    const string ColorHeader = "#7D1D1D";
    const string ColorRule = "#9B2020";
    const string ColorText = "#1C1208";
    const string ColorMuted = "#5C3D1C";
    const string ColorBg = "#FAE9C8";
    const string ColorGold = "#F5C8A8";

    static readonly Dictionary<string, (string Label, Func<CreatureStats, int> Stat)> SkillMap = new()
    {
        ["ath"] = ("Athletics", s => s.Str),
        ["acr"] = ("Acrobatics", s => s.Dex),
        ["ste"] = ("Stealth", s => s.Dex),
        ["arc"] = ("Arcana", s => s.Int),
        ["inv"] = ("Investigation", s => s.Int),
        ["his"] = ("History", s => s.Int),
        ["prc"] = ("Perception", s => s.Wis),
        ["sur"] = ("Survival", s => s.Wis),
        ["ins"] = ("Insight", s => s.Wis),
        ["dec"] = ("Deception", s => s.Cha),
        ["per"] = ("Persuasion", s => s.Cha),
        ["itm"] = ("Intimidation", s => s.Cha)
    };

    readonly Creature _c;
    public StatBlockComponent(Creature creature) => _c = creature;

    public void Compose(IContainer container)
    {
        container.Border(1).BorderColor(ColorRule).Column(col =>
        {
            col.Item()
               .Background(ColorHeader)
               .PaddingHorizontal(10).PaddingVertical(8)
               .Column(h =>
               {
                   h.Item().Text(_c.Name.ToUpper())
                       .FontSize(22).FontColor(Colors.White).Bold().FontFamily("Georgia");
                   h.Item().Text(TypeLine())
                       .FontSize(10).FontColor(ColorGold).Italic().FontFamily("Georgia");
               });

            col.Item()
               .Background(ColorBg)
               .BorderTop(2).BorderColor(ColorRule)
               .PaddingHorizontal(10).PaddingVertical(7)
               .Row(row =>
               {
                   Pill(row, "AC", _c.Stats.Ac.ToString());
                   row.ConstantItem(14);
                   Pill(row, "HP", _c.Stats.Hp.ToString());
                   row.ConstantItem(14);
                   Pill(row, "Speed", FormatSpeeds());
                   row.ConstantItem(14);
                   Pill(row, "CR", CrEngine.ToDisplay(_c.Cr));
                   row.ConstantItem(14);
                   Pill(row, "Prof", $"+{_c.ProfBonus}");
                   row.ConstantItem(14);
                   Pill(row, "XP", CrEngine.GetXp(_c.Cr).ToString("N0"));
               });

            col.Item().Height(2).Background(ColorRule);

            col.Item()
               .Background(ColorBg)
               .PaddingHorizontal(10).PaddingVertical(10)
               .Column(body =>
               {
                   AbilityScoreRow(body);
                   body.Item().PaddingVertical(6).Height(1).Background(ColorRule);

                   var saves = BuildSavingThrows();
                   if (saves.Count > 0) PropLine(body, "Saving Throws", string.Join(", ", saves));
                   var skills = BuildSkills();
                   if (skills.Count > 0) PropLine(body, "Skills", string.Join(", ", skills));
                   if (_c.Resistances.Count > 0) PropLine(body, "Damage Resistances", string.Join(", ", _c.Resistances));
                   if (_c.Immunities.Count > 0) PropLine(body, "Damage Immunities", string.Join(", ", _c.Immunities));
                   if (_c.ConditionImmunities.Count > 0) PropLine(body, "Condition Immunities", string.Join(", ", _c.ConditionImmunities));
                   PropLine(body, "Senses", BuildSenses());
                   PropLine(body, "Languages", "-");

                   body.Item().PaddingVertical(6).Height(1).Background(ColorRule);

                   foreach (var trait in _c.Traits.Where(t => t.LegendaryType == null || t.LegendaryType == "resistance"))
                       AbilityBlock(body, trait.Name, ResolveDesc(trait));

                   if (_c.SpellInfo != null)
                       AbilityBlock(body, "Spellcasting",
                           SpellEngine.BuildSpellcastingText(_c.SpellInfo, _c.Name));

                   body.Item().PaddingVertical(6).Height(1).Background(ColorRule);
                   SectionHeader(body, "Actions");

                   foreach (var action in _c.Actions)
                   {
                       var actionName = string.IsNullOrEmpty(action.Recharge)
                           ? action.Name
                           : $"{action.Name} (Recharge {action.Recharge})";
                       AbilityBlock(body, actionName, BuildActionText(action));
                   }

                   var legendaryActions = _c.Traits.Where(t => t.LegendaryType == "action").ToList();
                   var lairActions = _c.Traits.Where(t => t.LegendaryType == "lair").ToList();

                   if (legendaryActions.Count > 0)
                   {
                       body.Item().PaddingVertical(4).Height(1).Background(ColorRule);
                       SectionHeader(body, "Legendary Actions");
                       body.Item().PaddingBottom(5).Text(
                           $"{_c.Name} can take 3 legendary actions, choosing from the options below. " +
                           "Only one legendary action can be used at a time and only at the end of another creature's turn. " +
                           $"{_c.Name} regains spent legendary actions at the start of its turn.")
                           .FontSize(10).FontColor(ColorText).Italic().FontFamily("Georgia");
                       foreach (var la in legendaryActions)
                       {
                           var cost = la.LegendaryCost is > 1 ? $" (Costs {la.LegendaryCost} Actions)" : "";
                           AbilityBlock(body, la.Name + cost, la.Description);
                       }
                   }

                   if (lairActions.Count > 0)
                   {
                       body.Item().PaddingVertical(4).Height(1).Background(ColorRule);
                       SectionHeader(body, "Lair Actions");
                       foreach (var la in lairActions)
                           AbilityBlock(body, la.Name, ResolveDesc(la));
                   }
               });
        });
    }

    void AbilityScoreRow(ColumnDescriptor body)
    {
        var labels = new[] { "STR", "DEX", "CON", "INT", "WIS", "CHA" };
        var scores = new[] { _c.Stats.Str, _c.Stats.Dex, _c.Stats.Con, _c.Stats.Int, _c.Stats.Wis, _c.Stats.Cha };

        body.Item().Row(row =>
        {
            for (int i = 0; i < 6; i++)
            {
                var score = scores[i];
                var mod = StatMod(score);
                var modStr = mod >= 0 ? $"+{mod}" : mod.ToString();
                var label = labels[i];

                row.RelativeItem().Column(c =>
                {
                    c.Item().AlignCenter().Text(label)
                        .FontSize(9).Bold().FontColor(ColorRule).FontFamily("Georgia");
                    c.Item().AlignCenter().Text(score.ToString())
                        .FontSize(14).Bold().FontColor(ColorText).FontFamily("Georgia");
                    c.Item().AlignCenter().Text(modStr)
                        .FontSize(10).FontColor(ColorMuted).FontFamily("Georgia");
                });
            }
        });
    }

    void PropLine(ColumnDescriptor body, string label, string value)
    {
        body.Item().PaddingBottom(2).Text(t =>
        {
            t.Span(label + " ").FontSize(10).Bold().FontColor(ColorRule).FontFamily("Georgia");
            t.Span(value).FontSize(10).FontColor(ColorText).FontFamily("Georgia");
        });
    }

    void SectionHeader(ColumnDescriptor body, string text)
    {
        body.Item().PaddingBottom(4).Text(text)
            .FontSize(14).Bold().Italic().FontColor(ColorHeader).FontFamily("Georgia");
    }

    void AbilityBlock(ColumnDescriptor body, string name, string desc)
    {
        body.Item().PaddingBottom(5).Text(t =>
        {
            t.Span(name + ". ").FontSize(10).Bold().Italic().FontColor(ColorText).FontFamily("Georgia");
            t.Span(desc).FontSize(10).FontColor(ColorText).FontFamily("Georgia");
        });
    }

    static void Pill(RowDescriptor row, string label, string value)
    {
        row.AutoItem().Column(c =>
        {
            c.Item().Text(label).FontSize(8).Bold().FontColor(ColorRule).FontFamily("Georgia");
            c.Item().Text(value).FontSize(12).Bold().FontColor(ColorText).FontFamily("Georgia");
        });
    }

    string TypeLine()
    {
        var size = _c.Stats.Size switch
        {
            "tiny" => "Tiny", "sm" => "Small", "med" => "Medium",
            "lg" => "Large", "huge" => "Huge", "grg" => "Gargantuan",
            _ => "Medium"
        };
        var theme = _c.Theme == "any" ? "monstrosity" : _c.Theme;
        return $"{size} {theme}, unaligned";
    }

    string FormatSpeeds()
    {
        var parts = _c.Stats.Speeds.Select(kvp =>
            kvp.Key == "walk" ? $"{kvp.Value} ft." : $"{kvp.Key} {kvp.Value} ft.");
        return string.Join(", ", parts);
    }

    string BuildActionText(ActionData action)
    {
        var saveDc = 8 + _c.ProfBonus + StatMod(_c.Stats.Wis);
        var desc = (action.Description ?? "").Replace("{dc}", saveDc.ToString());

        var hasResolvedDamage = action.ResolvedDamage != null;
        var dmgFormula = hasResolvedDamage ? action.ResolvedDamage![0] : "";
        var dmgType = hasResolvedDamage && action.ResolvedDamage!.Length > 1 ? action.ResolvedDamage[1] : "damage";
        var avg = hasResolvedDamage ? (int)Math.Round(CombatEstimator.DiceAverage(dmgFormula)) : 0;

        if (action.ActionType is "mwak" or "rwak" or "rsak")
        {
            var attackStat = action.ActionType == "mwak" ? _c.Stats.Str : _c.Stats.Dex;
            var toHit = _c.ProfBonus + StatMod(attackStat);
            var toHitStr = toHit >= 0 ? $"+{toHit}" : toHit.ToString();
            var rangeStr = action.Range <= 5 ? $"reach {action.Range} ft." : $"range {action.Range} ft.";
            var label = action.ActionType == "mwak" ? "Melee Weapon Attack" : "Ranged Weapon Attack";
            var suffix = string.IsNullOrWhiteSpace(desc) ? "" : " " + desc;
            if (!hasResolvedDamage) return $"{label}: {toHitStr} to hit, {rangeStr}, one target.{suffix}";
            return $"{label}: {toHitStr} to hit, {rangeStr}, one target. Hit: {avg} ({dmgFormula}) {dmgType} damage.{suffix}";
        }

        if (action.ActionType == "save")
        {
            var dmgLine = hasResolvedDamage
                ? $"Damage: {avg} ({dmgFormula}) {dmgType} on a failed save, half on a success."
                : "";
            if (string.IsNullOrWhiteSpace(desc)) return string.IsNullOrWhiteSpace(dmgLine) ? "-" : dmgLine;
            return string.IsNullOrWhiteSpace(dmgLine) ? desc : $"{desc} {dmgLine}";
        }

        // util, summon, heal, etc.
        return string.IsNullOrWhiteSpace(desc) ? "-" : desc;
    }

    string ResolveDesc(TraitData trait)
    {
        var saveDc = 8 + _c.ProfBonus + StatMod(_c.Stats.Wis);
        return trait.Description
            .Replace("{dc}", saveDc.ToString())
            .Replace("{prof}", _c.ProfBonus.ToString());
    }

    List<string> BuildSavingThrows()
    {
        var saves = new List<string>();
        var tier = CrEngine.GetTier(_c.Cr);
        if (tier >= 3) saves.Add($"Con +{_c.ProfBonus + StatMod(_c.Stats.Con)}");
        if (tier >= 4)
        {
            saves.Add($"Wis +{_c.ProfBonus + StatMod(_c.Stats.Wis)}");
            saves.Add($"Cha +{_c.ProfBonus + StatMod(_c.Stats.Cha)}");
        }
        return saves;
    }

    List<string> BuildSkills()
    {
        return _c.Skills
            .Where(kvp => SkillMap.ContainsKey(kvp.Key))
            .Select(kvp =>
            {
                var (label, statFn) = SkillMap[kvp.Key];
                var bonus = _c.ProfBonus * kvp.Value + StatMod(statFn(_c.Stats));
                return $"{label} +{bonus}";
            })
            .ToList();
    }

    string BuildSenses()
    {
        var parts = _c.Senses.Select(kvp => $"{kvp.Key} {kvp.Value} ft.").ToList();
        var profMult = _c.Skills.TryGetValue("prc", out var m) ? m : 0;
        var passive = 10 + StatMod(_c.Stats.Wis) + _c.ProfBonus * profMult;
        parts.Add($"passive Perception {passive}");
        return string.Join(", ", parts);
    }

    static int StatMod(int score) => (score - 10) / 2;
}
