using System.Collections.Generic;
using System.Linq;
using COM3D2.EditModeItemManager;
using HarmonyLib;

namespace COM3D2.EditModeFavorites;

partial class EditModeFavorites {
	public static void ToggleFavoritePreset(string fileName) {
		if (ItemManager.TryGetPreset(fileName, out var preset)) {
			preset.IsFavorite = !preset.IsFavorite;
			ItemManager.SaveDatabase();
		}
	}

	public static bool IsFavoritePreset(string fileName) {
		return ItemManager.TryGetPreset(fileName, out var preset) && preset.IsFavorite;
	}

	private static void OnPresetButtonCreated(object sender, PresetButtonCreatedEventArgs e) {
		AddFavoriteOverlay(e.Container, true, IsFavoritePreset(e.PresetButton.preset.strFileName));
	}

	[HarmonyPatch(typeof(CharacterMgr), nameof(CharacterMgr.PresetListLoad))]
	[HarmonyPostfix]
	private static void PresetListLoad(ref List<CharacterMgr.Preset> __result) {
		if (SortFavoritesFirst) {
			__result = __result.OrderByDescending(e => IsFavoritePreset(e.strFileName)).ToList();
		}
	}

	[HarmonyPatch(typeof(PresetMgr), nameof(PresetMgr.ClickPreset))]
	[HarmonyPrefix]
	private static bool PreClickPreset(PresetMgr __instance) {

		if (IsFavoriteModifierPressed) {
			ToggleFavoritePreset(UIButton.current.name);
			__instance.UpdatePresetList();
			return false;
		}

		return true;
	}
}
