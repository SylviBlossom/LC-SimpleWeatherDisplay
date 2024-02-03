using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Linq;

namespace SimpleWeatherDisplay;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
	public static Plugin Instance { get; private set; }
	public static new Config Config { get; private set; }
	public static new ManualLogSource Logger { get; private set; }

	private void Awake()
	{
		Instance = this;
		Config = new(base.Config);
		Logger = base.Logger;

		Harmony.CreateAndPatchAll(typeof(Plugin), PluginInfo.PLUGIN_GUID);

		Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
	}

	[HarmonyILManipulator]
	[HarmonyPatch(typeof(StartOfRound), "SetMapScreenInfoToCurrentLevel")]
	private static void StartOfRound_SetMapScreenInfoToCurrentLevel(ILContext il)
	{
		var cursor = new ILCursor(il);

		if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdstr("Weather: ")))
		{
			Logger.LogError("Failed IL hook for StartOfRound.SetMapScreenInfoToCurrentLevel @ Non-clear weather string");
			return;
		}

		cursor.EmitDelegate<Func<string, string>>(str =>
		{
			if (!Config.ApplyToShipScreen.Value)
			{
				return str;
			}
			return $"{Config.GetWeatherTitle()}: ";
		});

		if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdstr("")))
		{
			Logger.LogError("Failed IL hook for StartOfRound.SetMapScreenInfoToCurrentLevel @ Clear weather string");
			return;
		}

		cursor.EmitDelegate<Func<string, string>>(str =>
		{
			if (!Config.ApplyToShipScreen.Value || !Config.DisplayClearWeather.Value)
			{
				return str;
			}
			return $"{Config.GetWeatherTitle()}: Clear";
		});

		var replacedCount = 0;

		while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdstr("Orbiting: ")))
		{
			if (!cursor.TryGotoNext(MoveType.AfterLabel, instr => instr.MatchCall<string>("Concat")))
			{
				break;
			}

			cursor.EmitDelegate<Func<string[], string[]>>(text =>
			{
				if (!Config.ApplyToShipScreen.Value || !Config.MoveToTop.Value)
				{
					return text;
				}

				var list = text.ToList();

				var weatherIndex = list.FindIndex(line => line.StartsWith("weather: ", StringComparison.InvariantCultureIgnoreCase));

				if (weatherIndex == -1)
				{
					if (Config.DisplayClearWeather.Value)
					{
						// display warning if this shouldn't happen
						Logger.LogWarning("Found no weather display on screen");
					}
					// no weather found, ignore
					return text;
				}

				var weather = list[weatherIndex];

				list.RemoveAt(weatherIndex);
				list.RemoveAt(weatherIndex - 1); // assumed newline

				var firstLineIndex = list.IndexOf("\n");

				if (firstLineIndex == -1)
				{
					firstLineIndex = list.Count;
				}

				list.Insert(firstLineIndex, "\n");
				list.Insert(firstLineIndex + 1, weather);

				return list.ToArray();
			});

			replacedCount++;
		}

		if (replacedCount < 2)
		{
			Logger.LogError($"Failed IL hook for StartOfRound.SetMapScreenInfoToCurrentLevel @ Screen text (Replaced {replacedCount}/2)");
			return;
		}
	}

	[HarmonyILManipulator]
	[HarmonyPatch(typeof(StartOfRound), "openingDoorsSequence", MethodType.Enumerator)]
	private static void StartOfRound_openingDoorsSequence(ILContext il)
	{
		var cursor = new ILCursor(il);

		if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<SelectableLevel>("LevelDescription")))
		{
			Logger.LogError("Failed IL hook for StartOfRound.openingDoorsSequence");
			return;
		}

		cursor.Emit(OpCodes.Ldloc_1);
		cursor.EmitDelegate<Func<string, StartOfRound, string>>((desc, self) =>
		{
			if (!Config.ApplyToLandingSequence.Value)
			{
				return desc;
			}

			if (!Config.DisplayClearWeather.Value && self.currentLevel.currentWeather == LevelWeatherType.None)
			{
				return desc;
			}

			var weatherName = self.currentLevel.currentWeather != LevelWeatherType.None ? self.currentLevel.currentWeather.ToString() : "Clear";
			var weatherLine = $"{Config.GetWeatherTitle()}: {weatherName}";

			return Config.MoveToTop.Value ? $"{weatherLine}\n{desc}" : $"{desc}\n{weatherLine}";
		});
	}
}