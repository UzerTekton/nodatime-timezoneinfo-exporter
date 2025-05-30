# NodaTimeTimeZoneInfoExporter

by Uzer Tekton

## Description

This tool generates **TimeZoneInfo serialized strings** using the NodaTime default TZDB which uses the latest IANA time zone data.

## Purpose

For use in `TimeZoneInfo.FromSerializedString(String)` in a Unity script for example.

By generating our own serialized strings (and embedding into a Unity project), we can ensure:

  - The game uses latest IANA time zone data (from NodaTime's default TZDB source).

  - The game does not rely on the device built-in timezone data (which can be outdated) nor rely on the OS-dependant `TimeZoneInfo.FindSystemTimeZoneById()` (which requires a different set of `id`s for non-Windows), therefore the project is cross-platform and portable.

    - This can be useful if you need the game to run on both PC and Android.

## Features and limitations

- It saves all the serialized string from all time zones into a txt file. By default this is `TimeZoneInfo.txt`.

  - It also prints the date when the file was generated, and the TZDB version it uses (e.g. should be 2025b as of writing).

- By default it only checks for, and create rules from the year 2000 to 2100 (with exceptions, see below). However you can manually set (variables `startYear` and `endYear`) to a longer period if you want.

  - This means, for some time zones that have inconsistent rules year-by-year based on number of weeks and weekdays, the rules will continue only up to `endYear`, and effectively become UTC forever afterwards. Increasing `endYear` will increase the string size of these time zones.

  - However this will not affect time zones that do not have any DST rules, or have a single consistent DST rule, which is set to last effectively forever into the future (but not the past). (Microsoft docs says `DateTime.MaxValue.Date` means no end date, even though the text file reads `9999`.)

- During conversion at `CreateCustomTimeZone()` it does not include the correct names for `displayName`, `standardDisplayName`, `daylightDisplayName`.

  - Therefore if you need to read the name from the TimeZoneInfo, only `id` is available.


## Prerequisites

- Some way to run a `.cs` file e.g. .NET SDK

- Latest NodaTime NuGet package


## How to run for beginner

1. Install .NET SDK

2. Ensure you have the latest IANA TZDB: (This ensures you have the latest IANA time zone data)

  - In Windows cmd, type `dotnet add package NodaTime` to install/update the package.
  
3. Use dotnet to run the script. (Plenty of guides out there teaching you how to run your first dotnet)

4. It should create the txt file in the same folder.

## Usage

- Find the time zone you want inside this file, copy the serialized string into your Unity script as a string.

- Use `TimeZoneInfo.FromSerializedString(serializedString)` to get the TimeZoneInfo object.

- Use the TimeZoneInfo in e.g. `TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo)`

- Example:

  Open the text file, find your time zone, and copy the part without the title.
  
  ```
  // DO NOT COPY THIS PART
  # Europe/London
  
  // COPY THIS PART
  Europe/London;0;Europe/London;Europe/London;Europe/London;[01:01:2000;12:31:9999;60;[0;02:00:00;3;5;0;];[0;02:00:00;10;5;0;];];
  
  ```
  
  And then paste it in Unity script or where you want to use `TimeZoneInfo.FromSerializedString`:
  
  ```
                       // PASTE HERE as a string
  string serialized = "Europe/London;0;Europe/London;Europe/London;Europe/London;[01:01:2000;12:31:9999;60;[0;02:00:00;3;5;0;];[0;02:00:00;10;5;0;];];";
  
  // Use the string to get your TimeZoneInfo
  TimeZoneInfo tz = TimeZoneInfo.FromSerializedString(serialized);
  
  // And then do conversions with it or whatever you want
  DateTime local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
  ```

### Contact

Leave a message on my Discord server or DM me: https://discord.gg/yG4HnBM8Du
