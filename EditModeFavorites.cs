using System;
using BepInEx;
using BepInEx.Configuration;
using COM3D2.EditModeItemManager;
using HarmonyLib;
using UnityEngine;

namespace COM3D2.EditModeFavorites;

enum ModifierKey {
	Control,
	Alt,
	Shift,
}

[BepInPlugin("net.perdition.com3d2.editmodefavorites", PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInDependency("net.perdition.com3d2.editmodeitemmanager")]
public partial class EditModeFavorites : BaseUnityPlugin {
	private static ConfigEntry<bool> _configFavoriteSorting;
	private static ConfigEntry<ModifierKey> _configToggleFavoriteModifier;

	private static bool _doSort = false;
	private static SceneEdit.SMenuItem _clickCallbackItem;

	private static bool SortFavoritesFirst => _configFavoriteSorting.Value;

	private static bool IsFavoriteModifierPressed {
		get => _configToggleFavoriteModifier.Value switch {
			ModifierKey.Control => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl),
			ModifierKey.Alt => Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt),
			ModifierKey.Shift => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift),
			_ => throw new NotImplementedException(),
		};
	}

	private void Awake() {
		_configFavoriteSorting = Config.Bind("General", "SortFavoritesFirst", true, "Place favorites first instead of their default sorting position");
		_configToggleFavoriteModifier = Config.Bind("General", "ToggleFavoriteModifier", ModifierKey.Control, "Modifier key used to toggle favorite status");

		_configFavoriteSorting.SettingChanged += (o, e) => {
			if (SceneEdit.Instance) {
				SortAll();
				SceneEdit.Instance.UpdateCurrentItemPanel(true);
			}
		};

		ItemManager.MenuItemButtonCreated += OnMenuItemButtonCreated;
		ItemManager.GroupSetButtonCreated += OnGroupSetButtonCreated;
		ItemManager.PresetButtonCreated += OnPresetButtonCreated;

		Harmony.CreateAndPatchAll(typeof(EditModeFavorites));
	}

	public static void ToggleFavorite(string fileName) {
		if (ItemManager.TryGetItem(fileName, out var item)) {
			item.IsFavorite = !item.IsFavorite;
			ItemManager.SaveDatabase();
		}
	}

	public static bool IsFavoriteItem(string fileName) {
		return ItemManager.TryGetItem(fileName, out var item) && item.IsFavorite;
	}

	public static bool HasFavoriteItems(SceneEdit.SMenuItem item) {
		return item.m_bGroupLeader && item.m_listMember.Exists(item => IsFavoriteItem(item.m_strMenuFileName));
	}

	private static void AddFavoriteOverlay(OverlayContainer container, bool useOffset, bool isActive, bool isPartial = false) {
		container.FavoriteOverlay = new FavoriteFrame(container, useOffset) {
			Active = isActive,
			Partial = isPartial,
		};
	}

	private static void OnMenuItemButtonCreated(object sender, MenuItemEventArgs e) {
		var isFavoriteItem = IsFavoriteItem(e.MenuItem.m_strMenuFileName);
		AddFavoriteOverlay(e.Container, e.IsSetItem, isFavoriteItem || (SceneEdit.Instance.m_bUseGroup && HasFavoriteItems(e.MenuItem)), !isFavoriteItem);
	}

	private static void OnGroupSetButtonCreated(object sender, GroupSetButtonCreatedEventArgs e) {
		AddFavoriteOverlay(e.Container, false, IsFavoriteItem(e.MenuItem.m_strMenuFileName));
	}

	[HarmonyPatch(typeof(SceneEdit), nameof(SceneEdit.m_bUseGroup), MethodType.Setter)]
	[HarmonyPrefix]
	private static void OnSetUseGroup(SceneEdit __instance, bool value) {
		if (value == GameMain.Instance.CMSystem.EditItemGroup || __instance.m_Panel_PartsType == null) {
			return;
		}
		_doSort = true;
	}

	[HarmonyPatch(typeof(SceneEdit.SPartsType), nameof(SceneEdit.SPartsType.SortItem))]
	[HarmonyPrefix]
	private static bool SortItem(SceneEdit.SPartsType __instance) {
		if (!SortFavoritesFirst || !ItemManager.IsItemPartType(__instance)) return true;

		__instance.m_listMenu.Sort((x, y) => {
			// default item always comes first
			if (x.m_boDelOnly != y.m_boDelOnly) {
				return x.m_boDelOnly ? -1 : 1;
			}
			var aIsFavorite = IsFavoriteItem(x.m_strMenuFileName) || (SceneEdit.Instance.m_bUseGroup && HasFavoriteItems(x));
			var bIsFavorite = IsFavoriteItem(y.m_strMenuFileName) || (SceneEdit.Instance.m_bUseGroup && HasFavoriteItems(y));
			if (aIsFavorite != bIsFavorite) {
				return aIsFavorite ? -1 : 1;
			} else if (x.m_fPriority != y.m_fPriority) {
				return (int)x.m_fPriority - (int)y.m_fPriority;
			} else {
				return x.m_strMenuName.CompareTo(y.m_strMenuName);
			}
		});

		foreach (var i in __instance.m_listMenu) {
			i.SortColorItem();
		}

		return false;
	}

	private static void SortAll() {
		foreach (var category in SceneEdit.Instance.CategoryList) {
			category.SortItem();
		}
	}

	[HarmonyPatch(typeof(SceneEdit), nameof(SceneEdit.UpdateCurrentItemPanel))]
	[HarmonyPrefix]
	private static void UpdateCurrentItemPanel() {
		if (_doSort) {
			SortAll();
			_doSort = false;
		}
	}

	[HarmonyPatch(typeof(SceneEdit), nameof(SceneEdit.ClickCallback))]
	[HarmonyPrefix]
	private static bool PreClickCallback(SceneEdit __instance) {
		var buttonEdit = UIButton.current.GetComponentInChildren<ButtonEdit>();
		var item = buttonEdit.m_MenuItem;

		if (item != null && !item.m_bColor && IsFavoriteModifierPressed) {
			if (!item.m_boDelOnly) {
				ToggleFavorite(item.m_strMenuFileName);
				item.m_ParentPartsType.SortItem();
				if (__instance.m_Panel_GroupSet.goMain.activeSelf && __instance.m_listBtnGroupMember.Count > 0) {// set item
					_clickCallbackItem = __instance.m_listBtnGroupMember[0].mi;
				}
				__instance.UpdatePanel_MenuItem(item.m_ParentPartsType);
				_clickCallbackItem = null;
			}
			return false;
		}

		return true;
	}

	// hack to prevent group panel from hiding when toggling set favorites
	[HarmonyPatch(typeof(SceneEdit), nameof(SceneEdit.UpdatePanel_GroupSet))]
	[HarmonyPrefix]
	private static void UpdatePanel_GroupSet(ref SceneEdit.SMenuItem f_miSelected) {
		if (_clickCallbackItem != null) {
			f_miSelected = _clickCallbackItem;
		}
	}

	// prevent erratic scroll behavior in set group frames (?)
	[HarmonyPatch(typeof(SceneEdit), nameof(SceneEdit.UpdatePanel_GroupSet))]
	[HarmonyPostfix]
	private static void PostUpdatePanel_GroupSet(SceneEdit __instance, SceneEdit.SMenuItem f_miSelected) {
		if (f_miSelected == null || f_miSelected.m_mpn == MPN.set_body || !f_miSelected.m_bGroupLeader || !__instance.m_bUseGroup) {
			return;
		}
		if (f_miSelected.m_strCateName.StartsWith("set_")) {
			__instance.m_Panel_GroupSet.ResetScrollPos(0f);
		}
	}
}
