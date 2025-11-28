using System;
using System.Collections.Generic;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.ScreenSystem;
using Enlisted.Mod.Core.Logging;

namespace Enlisted.Features.Equipment.UI
{
	/// <summary>
	/// Gauntlet popup that lists eligible troops with hover hints showing their loadouts.
	/// ESC and Back button close the popup without selection.
	/// </summary>
	public class TroopSelectionPopupBehavior
	{
		private static GauntletLayer _layer;
		// 1.3.4 API: LoadMovie now returns GauntletMovieIdentifier instead of IGauntletMovie
		private static GauntletMovieIdentifier _movie;
		private static TroopSelectionPopupVM _vm;

		public static void Show(List<TroopSelectionItemVM> items, Action<TroopSelectionItemVM> onSelect, Action onCancel)
		{
			try
			{
				Close();

				ModLogger.Info("TroopSelection", $"DEBUG: Creating popup with {items.Count} items");
				_vm = new TroopSelectionPopupVM(items, onSelect, OnRequestClose);
				ModLogger.Info("TroopSelection", $"DEBUG: VM created, Items count: {_vm.Items.Count}");
				_vm.RefreshValues();
				// 1.3.4 API: GauntletLayer constructor params reordered to (string name, int localOrder, bool shouldClear)
			_layer = new GauntletLayer("TroopSelectionPopup", 1002, false);
				ModLogger.Info("TroopSelection", $"DEBUG: Loading movie TroopSelectionPopup");
				_movie = _layer.LoadMovie("TroopSelectionPopup", _vm);
				ModLogger.Info("TroopSelection", $"DEBUG: Movie loaded successfully");

				// Hotkeys first, then restrictions (matches working pattern we use elsewhere)
				_layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
				_layer.InputRestrictions.SetInputRestrictions(true);
				_layer.IsFocusLayer = true;
				ScreenManager.TopScreen.AddLayer(_layer);
				ScreenManager.TrySetFocus(_layer);

				void OnRequestClose()
				{
					try
					{
						onCancel?.Invoke();
					}
					finally
					{
						Close();
					}
				}
			}
			catch (Exception ex)
			{
				ModLogger.Error("TroopSelection", $"Gauntlet popup failed: {ex.Message}");
				onCancel?.Invoke();
				Close();
			}
		}

		public static void Close()
		{
			try
			{
				if (_layer != null)
				{
					_layer.InputRestrictions.ResetInputRestrictions();
					_layer.IsFocusLayer = false;
					if (_movie != null)
					{
						_layer.ReleaseMovie(_movie);
					}
					var top = ScreenManager.TopScreen;
					if (top != null)
					{
						top.RemoveLayer(_layer);
					}
				}
			}
			catch (Exception ex)
			{
				ModLogger.Error("TroopSelection", $"Error closing popup: {ex.Message}");
			}
			finally
			{
				_layer = null;
				_movie = null;
				_vm = null;
			}
		}
	}
}


