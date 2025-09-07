using System;
using System.Collections.Generic;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.InputSystem;
using TaleWorlds.Core.ViewModelCollection.Information;

namespace Enlisted.Features.Equipment.UI
{
	public class TroopSelectionPopupVM : ViewModel
	{
		[DataSourceProperty]
		public MBBindingList<TroopSelectionItemVM> Items { get; private set; }

		[DataSourceProperty]
		public string HeaderText { get; private set; }


		public Action RequestClose { get; set; }

		private readonly Action<TroopSelectionItemVM> _onSelect;

		[DataSourceProperty]
		public TroopSelectionItemVM SelectedItem { get; private set; }

		public TroopSelectionPopupVM(List<TroopSelectionItemVM> items, Action<TroopSelectionItemVM> onSelect, Action requestClose)
		{
			Items = new MBBindingList<TroopSelectionItemVM>();
			foreach (var i in items)
			{
				i.OnClick = SetSelected;
				Items.Add(i);
			}
			_onSelect = onSelect;
			RequestClose = requestClose;
			HeaderText = "Select equipment to use";
		}

		public void ExecuteClose()
		{
			RequestClose?.Invoke();
		}

		public void Select(TroopSelectionItemVM item)
		{
			_onSelect?.Invoke(item);
			RequestClose?.Invoke();
		}

		public void ExecuteConfirm()
		{
			if (SelectedItem != null)
			{
				_onSelect?.Invoke(SelectedItem);
			}
			RequestClose?.Invoke();
		}

		private void SetSelected(TroopSelectionItemVM item)
		{
			SelectedItem = item;
			OnPropertyChanged(nameof(SelectedItem));
		}

	}

	public class TroopSelectionItemVM : ViewModel
	{
		[DataSourceProperty]
		public string Name { get; private set; }

		[DataSourceProperty]
		public string HintText { get; private set; }

		[DataSourceProperty]
		public ImageIdentifierVM Visual { get; private set; }

		public object Payload { get; }

		public Action<TroopSelectionItemVM> OnClick { get; set; }

		public TroopSelectionItemVM(string name, string hint, object payload)
		{
			Name = name;
			HintText = hint;
			Payload = payload;
			
			// Add character portrait using current TaleWorlds pattern
			try
			{
				var troop = payload as CharacterObject;
				if (troop != null)
				{
					Visual = new ImageIdentifierVM(SandBox.ViewModelCollection.SandBoxUIHelper.GetCharacterCode(troop, false));
				}
			}
			catch
			{
				Visual = null;
			}
		}

		public void ExecuteSelect()
		{
			OnClick?.Invoke(this);
		}
	}
}


