using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleWeatherDisplay;

public class Config
{
	public static ConfigEntry<bool> ApplyToShipScreen { get; private set; }
	public static ConfigEntry<bool> ApplyToLandingSequence { get; private set; }

	public static ConfigEntry<bool> DisplayClearWeather { get; private set; }
	public static ConfigEntry<bool> CapitalizeTitle { get; private set; }
	public static ConfigEntry<bool> MoveToTop { get; private set; }

	public Config(ConfigFile cfg)
	{
		ApplyToShipScreen = cfg.Bind("General", "ApplyToShipScreen", true, "Whether to apply the mod's changes to the ship screen.");
		ApplyToLandingSequence = cfg.Bind("General", "ApplyToLandingSequence", true, "Whether to apply the mod's changes to the moon info popup on landing.");

		DisplayClearWeather = cfg.Bind("Tweaks", "DisplayClearWeather", true, "Still displays weather when it's clear.");
		CapitalizeTitle = cfg.Bind("Tweaks", "CapitalizeTitle", true, "Capitalizes the word 'WEATHER' like the rest of the moon info.");
		MoveToTop = cfg.Bind("Tweaks", "MoveToTop", true, "Moves the weather display to the top, making it always visible on the screen.");
	}

	public static string GetWeatherTitle()
	{
		return CapitalizeTitle.Value ? "WEATHER" : "Weather";
	}
}
