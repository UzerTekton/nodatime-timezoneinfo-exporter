# NodaTimeTimeZoneInfoExporter
by Uzer Tekton

## Description
This tool generates **TimeZoneInfo serialized strings** using NodaTime TZDB.

For use in `TimeZoneInfo.FromSerializedString(String)` in a Unity script for example.

The point is by generating our own serialized strings (and embedding into a Unity project), we can ensure:

  - The game uses latest IANA time zone data (from NodaTime's embedded TZDB source).

  - The game does not rely on the device built-in timezone data (which can be outdated) nor rely on the OS-dependant `TimeZoneInfo.FindSystemTimeZoneById()` (which requires different `id` for non-Windows), therefore the project is cross-platform and portable.

## Features and Limitations

- By default it only checks for rules from the year 2000 to 2100. However you can set to a longer period if you want, but it will increase the string size of some time zones.

  - This will not affect you if your time zone does not have DST, or has a constant DST rule (it will be condensed into one rule).

- It does not generate with `displayName` `standardDisplayName` `daylightDisplayName`.


## Prerequisites

Requires:

- Some way to compile the cs file e.g. .NET SDK

- NodaTime NuGet package


## How to run for beginner
1. Install .NET SDK

2. Ensure you have the latest IANA TZDB:
  - In Windows cmd, type `dotnet add package NodaTime` to install/update the package.
    - (Optional) To specify a version: `dotnet add package NodaTime --version x.y.z`
  
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
  Europe/London;0;Europe/London;Europe/London;Europe/London;[01:01:2000;12:31:2100;60;[0;02:00:00;3;5;0;];[0;02:00:00;10;5;0;];];
  
  ```
  
  And then paste it in Unity script or where you want to use `TimeZoneInfo.FromSerializedString`:
  
  ```
                       // PASTE HERE as a string
  string serialized = "Europe/London;0;Europe/London;Europe/London;Europe/London;[01:01:2000;12:31:2100;60;[0;02:00:00;3;5;0;];[0;02:00:00;10;5;0;];];";
  
  // Use the string to get your TimeZoneInfo
  TimeZoneInfo tz = TimeZoneInfo.FromSerializedString(serialized);
  
  // And then do conversions with it or whatever you want
  DateTime local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
  ```

### Contact

Leave a message on my Discord server or DM me: https://discord.gg/yG4HnBM8Du
