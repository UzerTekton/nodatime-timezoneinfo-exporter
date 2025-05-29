# NodaTimeTimeZoneInfoExporter
by Uzer Tekton

## Description
This tool generates **TimeZoneInfo serialized strings** using NodaTime TZDB.

For use in `TimeZoneInfo.FromSerializedString(String)` in a Unity script.

The point is by generating our own serialized strings (and embedding into a project), we can ensure:

- It uses latest IANA time zone data (from NodaTime's embedded TZDB source).

- It does NOT rely on Windows built-in timezone data, therefore the project is cross-platform and portable.

This program was meant for generating serialized strings for use in Unity scripting (in Udon for VRChat to be precise) and is tested to work in VRChat SDK 3.8.1,  but I don't know if the generated strings will work for any other purposes.

## Prerequisites
Requires:

- Some way to compile the cs file e.g. .NET SDK

- NodaTime NuGet package


## How to run for beginner
1. Install .NET SDK

2. Ensure you have the latest IANA TZDB:
  - In Windows cmd, type `dotnet add package NodaTime` to install/update the package.
    - (Optional) To specify a version: `dotnet add package NodaTime --version x.y.z`
  
3. Easiest and quickest way is to save the cs file somewhere and type `dotnet run` in Windows cmd at the location. It should create the txt file in the same folder.

## Usage

- Find the time zone you want inside this file, copy its entire line into your Unity script as a string. This is the "serialized string".

- Use `TimeZoneInfo.FromSerializedString(serializedString)` to get the TimeZoneInfo object.

- Use the TimeZoneInfo in e.g. `TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo)`

- Example:
  ```
   // This is copied from one of the lines inside the text file
  string serialized = "Custom TimeZone,3,Custom TimeZone,-06:00:00,-06:00:00,01:00:00,03,8,2:00:00,11,1,2:00:00,,,,,";
  
  TimeZoneInfo tz = TimeZoneInfo.FromSerializedString(serialized);
  
  // result
  DateTime local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
  ```

### Contact

Leave a message on my Discord server or DM me: https://discord.gg/yG4HnBM8Du
