// NodaTimeTimeZoneInfoExporter
// by Uzer Tekton
//
//
// MIT License
//
// Copyright (c) 2025 Uzer Tekton
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//
//
// This tool generates TimeZoneInfo serialized strings using NodaTime TZDB
// Requires: NodaTime NuGet package
// NOTE: This does NOT use Windows built-in timezone data.
// It uses the latest IANA time zone data from NodaTime's embedded TZDB source.
//
// To ensure you have the latest IANA TZDB:
// 1. Run: `dotnet add package NodaTime` to install/update the package.
// 2. To specify a version: `dotnet add package NodaTime --version x.y.z`
//
// Usage:
// - Find the time zone you want inside this file, copy its entire line into your Unity script as a string.
// - Use TimeZoneInfo.FromSerializedString(serializedString) to get the TimeZoneInfo object.
//
// Example:
// string serialized = "Custom TimeZone,3,Custom TimeZone,-06:00:00,-06:00:00,01:00:00,03,8,2:00:00,11,1,2:00:00,,,,,";
// TimeZoneInfo tz = TimeZoneInfo.FromSerializedString(serialized);
// DateTime local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);

using System;
using System.IO;
using System.Collections.Generic;
using NodaTime;
using NodaTime.TimeZones;

class TimeZoneInfoExporter
{
    static void Main(string[] args)
    {
        string outputPath = "TimeZoneDatabase.txt";

        var headerLines = new List<string>
        {
            "// TimeZoneInfo serialized strings generated with NodaTimeTimeZoneInfoExporter",
            "// Using NodaTime's TZDB (latest IANA time zone data).",
            $"// Generated on: {DateTime.UtcNow:u} UTC",
            "//",
            "// How to use:",
            "// Copy a full line from this file into your Unity C# script as a string.",
            "// Then call TimeZoneInfo.FromSerializedString(yourString) to get the TimeZoneInfo instance.",
            "//",
            "// Example:",
            "// string serialized = \"Custom TimeZone,3,Custom TimeZone,-06:00:00,-06:00:00,01:00:00,03,8,2:00:00,11,1,2:00:00,,,,,\";",
            "// TimeZoneInfo tz = TimeZoneInfo.FromSerializedString(serialized);",
            "// DateTime local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);",
            "// Debug.Log($\"Local time in {tz.Id}: {local}\");",
            ""
        };

        var tzdb = DateTimeZoneProviders.Tzdb;
        var now = SystemClock.Instance.GetCurrentInstant();

        var lines = new List<string>();
        lines.AddRange(headerLines);

        foreach (var id in tzdb.Ids)
        {
            try
            {
                var zone = tzdb[id];
                var interval = zone.GetZoneInterval(now);
                var baseOffset = interval.StandardOffset.ToTimeSpan();
                var dstOffset = interval.Savings.ToTimeSpan();

                string displayName = $"(UTC{FormatOffset(baseOffset)}) {id}";
                string standardName = id + " Standard Time";
                string daylightName = id + " Daylight Time";

                TimeZoneInfo tzi;
                if (dstOffset == TimeSpan.Zero)
                {
                    tzi = TimeZoneInfo.CreateCustomTimeZone(id, baseOffset, displayName, standardName);
                }
                else
                {
                    var timeOfDay = new DateTime(1, 1, 1, 2, 0, 0);

                    var dstStart = TimeZoneInfo.TransitionTime.CreateFixedDateRule(timeOfDay, 3, 8);
                    var dstEnd = TimeZoneInfo.TransitionTime.CreateFixedDateRule(timeOfDay, 11, 1);

                    var adjustment = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
                        new DateTime(2000, 1, 1),
                        new DateTime(2099, 12, 31),
                        dstOffset,
                        dstStart,
                        dstEnd);

                    tzi = TimeZoneInfo.CreateCustomTimeZone(
                        id,
                        baseOffset,
                        displayName,
                        standardName,
                        daylightName,
                        new[] { adjustment });
                }

                string serialized = tzi.ToSerializedString();
                lines.Add(serialized);
                Console.WriteLine($"Serialized: {id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed for {id}: {ex.Message}");
            }
        }

        File.WriteAllLines(outputPath, lines);
        Console.WriteLine($"Written {lines.Count - headerLines.Count} zones to {outputPath}");
    }

    static string FormatOffset(TimeSpan offset)
    {
        return (offset < TimeSpan.Zero ? "-" : "+") + offset.ToString(@"hh\:mm");
    }
}
