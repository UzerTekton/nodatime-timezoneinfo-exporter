using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NodaTime;
using NodaTime.TimeZones;

class TimeZoneInfoExporter
{
    static void Main()
    {
        var outputPath = "TimeZoneInfo.txt";
        var tzdb = DateTimeZoneProviders.Tzdb;
        var startYear = 2000;
        var endYear = 2100;
        int serializedCount = 0;

        using var writer = new StreamWriter(outputPath);
        // Write header block
        writer.WriteLine("# TimeZoneInfo Serialized Strings");
        writer.WriteLine($"# Exported with TimeZoneInfoExporter on {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}");
        writer.WriteLine($"# TZDB version used: {TzdbDateTimeZoneSource.Default.TzdbVersion}");
        writer.WriteLine($"# Contains serialized time zones from {startYear} to {endYear}.");
        writer.WriteLine("# Usage:");
        writer.WriteLine("# Deserialize with TimeZoneInfo.FromSerializedString.");
        writer.WriteLine("# Example:");
        writer.WriteLine("# TimeZoneInfo.FromSerializedString(\"" + "<PASTE THE STRING HERE>" + "\")");
        writer.WriteLine("# ----------------------------------------");
        writer.WriteLine();

        foreach (var id in tzdb.Ids)
        {
            Console.WriteLine($"Processing {id}...");
            var zone = tzdb[id];
            try
            {
                var serialized = SerializeZone(zone, id, startYear, endYear);
                writer.WriteLine($"# {id}");
                writer.WriteLine(serialized);
                writer.WriteLine();
                serializedCount++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to process {id}: {ex.Message}");
            }
        }

        Console.WriteLine($"Done. {serializedCount} time zones written to {outputPath}.");
    }

    // Create serialized TimeZoneInfo string from NodaTime zone and years range
    static string SerializeZone(DateTimeZone zone, string id, int startYear, int endYear)
    {
        var rules = new List<TimeZoneInfo.AdjustmentRule>();

        // Get intervals covering [startYear, endYear+1)
        var intervals = zone.GetZoneIntervals(new Interval(Instant.FromUtc(startYear, 1, 1, 0, 0),
            Instant.FromUtc(endYear + 1, 1, 1, 0, 0)), ZoneEqualityComparer.Options.MatchOffsetComponents).ToList();

        // Base offset from first interval's standard offset
        var baseOffset = intervals[0].StandardOffset.ToTimeSpan();


        for (int i = 0; i < intervals.Count - 1; i++)
        {
            var curr = intervals[i];

            // Only create adjustment rule if current interval has DST savings
            if (curr.Savings != Offset.Zero)
            {
                // Use January 1 to December 31 of current interval's year for rule dates
                var firstDayOfYear = new DateTime(curr.IsoLocalStart.Year, 1, 1);
                var lastDayOfYear = new DateTime(curr.IsoLocalEnd.Year, 12, 31);

                // Create floating transition times for DST start and end
                var daylightDelta = curr.Savings.ToTimeSpan();
                var daylightTransitionStart = CreateFloatingTransitionTime(curr.IsoLocalStart);
                var daylightTransitionEnd = CreateFloatingTransitionTime(curr.IsoLocalEnd);

                var candidateRule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
                    firstDayOfYear,
                    lastDayOfYear,
                    daylightDelta,
                    daylightTransitionStart,
                    daylightTransitionEnd);

                // === Grouping logic starts ===
                // If there is no previous rule, just add the rule
                if (rules.Count == 0)
                {

                    rules.Add(candidateRule);
                }
                // If there are any previous rule, check against the previous rule
                else
                {
                    var lastRule = rules[rules.Count - 1];
                    
                    // If consecutive rules are the same (often the previous year)
                    if (AreAdjustmentRulesEqualIgnoringDates(lastRule, candidateRule))

                    {
                    // Extend last rule's DateEnd to candidateRule.DateEnd
                    // This is to catch some changed rules that are ended, therefore not forever
                    
                        var extendedRule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
                            lastRule.DateStart, // 1 Jan of the previous year
                            (rules.Count > 1) ? candidateRule.DateEnd : DateTime.MaxValue.Date, // 31 Dec of the current year, or forever if one single rule is recurring past end year
                            lastRule.DaylightDelta,
                            lastRule.DaylightTransitionStart,
                            lastRule.DaylightTransitionEnd);
                            
                        // Just extending the last rule without adding new rule
                        rules[rules.Count - 1] = extendedRule;
                        
                    }
                    // If the rules are different
                    else
                    {
                        // Adjust last rule to end the day before new rule starts
                        var adjustedLastRule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
                            lastRule.DateStart,
                            curr.IsoLocalStart.ToDateTimeUnspecified().Date.AddDays(-1), // The day before current interval starts
                            lastRule.DaylightDelta,
                            lastRule.DaylightTransitionStart,
                            lastRule.DaylightTransitionEnd);

                        rules[rules.Count - 1] = adjustedLastRule;
                        
                        // Adjust candidate rule to start at the interval start day
                        candidateRule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
                            curr.IsoLocalStart.ToDateTimeUnspecified().Date, // current interval's actual start date
                            lastDayOfYear, // If rules are changing, it should not continue forever
                            daylightDelta,
                            daylightTransitionStart,
                            daylightTransitionEnd);

                        rules.Add(candidateRule);
                    }
                }
                // === Grouping logic ends ===
            }
        }

        // Create the custom TimeZoneInfo with base offset and DST rules (or none)
        var tzi = TimeZoneInfo.CreateCustomTimeZone(id, baseOffset, id, id, id, rules.ToArray());

        // Serialize using ToSerializedString (no parameters)
        return tzi.ToSerializedString();
    }

    // Convert LocalDateTime to floating TransitionTime for TimeZoneInfo
    static TimeZoneInfo.TransitionTime CreateFloatingTransitionTime(LocalDateTime ldt)
    {
        // .NET DayOfWeek: Sunday=0 ... Saturday=6
        // NodaTime IsoDayOfWeek: Monday=1 ... Sunday=7
        int dotnetDayOfWeek = ((int)ldt.DayOfWeek % 7);

        // Calculate week of month (1-5)
        int week = GetWeekOfMonth(ldt);
        if (week > 5) week = 5; // clamp to 5 to fix invalid week errors

        // Time of day component
        var timeOfDay = new DateTime(1, 1, 1, ldt.Hour, ldt.Minute, ldt.Second);

        // Create floating date rule (e.g. 2nd Sunday of March)
        return TimeZoneInfo.TransitionTime.CreateFloatingDateRule(timeOfDay, ldt.Month, week, (DayOfWeek)dotnetDayOfWeek);
    }

    // Calculate week of the month for a given LocalDateTime
    static int GetWeekOfMonth(LocalDateTime date)
    {
        // Day of week for the first day of the month (0 = Sunday ... 6 = Saturday)
        var firstDayOfMonth = new LocalDateTime(date.Year, date.Month, 1, 0, 0);
        int firstDayOfWeek = ((int)firstDayOfMonth.DayOfWeek % 7);

        // Calculate which week the day belongs to
        return ((date.Day + firstDayOfWeek - 1) / 7) + 1;
    }

    // Compare adjustment rules ignoring the start/end date properties
    static bool AreAdjustmentRulesEqualIgnoringDates(TimeZoneInfo.AdjustmentRule a, TimeZoneInfo.AdjustmentRule b)
    {
        if (a == null || b == null) return false;

        return a.DaylightDelta == b.DaylightDelta
            && TransitionTimesEqual(a.DaylightTransitionStart, b.DaylightTransitionStart)
            && TransitionTimesEqual(a.DaylightTransitionEnd, b.DaylightTransitionEnd);
    }

    // Compare two TransitionTime objects for equality
    static bool TransitionTimesEqual(TimeZoneInfo.TransitionTime a, TimeZoneInfo.TransitionTime b)
    {
        if (a.IsFixedDateRule != b.IsFixedDateRule) return false;
        if (a.Month != b.Month) return false;
        if (a.TimeOfDay != b.TimeOfDay) return false;

        if (a.IsFixedDateRule)
        {
            return a.Day == b.Day;
        }
        else
        {
            return a.Week == b.Week && a.DayOfWeek == b.DayOfWeek;
        }
    }
}
