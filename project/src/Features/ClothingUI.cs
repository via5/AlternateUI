using UnityEngine;
using System.Collections;
using System;

namespace AUI.ClothingUI
{
	class CurrentClothingPanel : DynamicItemsUI.CurrentControls.ItemPanel
	{
		private readonly VUI.Button adjustments_;
		private readonly VUI.Button physics_;
		private readonly VUI.CheckBox visible_;
		private bool ignore_ = false;

		public CurrentClothingPanel(DynamicItemsUI.Controls c)
			: base(c)
		{
			adjustments_ = AddWidget(new VUI.ToolButton("A", OpenAdjustments, "Adjustments"));
			physics_ = AddWidget(new VUI.ToolButton("P", OpenPhysics, "Physics"));
			visible_ = AddWidget(new VUI.CheckBox("V", OnVisible, false, "Hides all materials"));
		}

		public DAZClothingItem ClothingItem
		{
			get { return Item as DAZClothingItem; }
		}

		protected override void DoUpdate()
		{
			try
			{
				ignore_ = true;
				adjustments_.Enabled = true;
				physics_.Enabled = HasSim();
				visible_.Checked = IsClothingVisible();
			}
			finally
			{
				ignore_ = false;
			}
		}

		protected override void DoActiveChanged(bool b)
		{
			adjustments_.Enabled = b;
			physics_.Enabled = b && HasSim();
		}

		private bool IsClothingVisible()
		{
			if (Item == null)
				return false;

			foreach (var mo in Item.GetComponentsInChildren<MaterialOptions>())
			{
				var j = mo.GetBoolJSONParam("hideMaterial");
				if (j != null)
				{
					if (!j.val)
						return true;
				}
			}

			return false;
		}

		private bool HasSim()
		{
			var sim = Item?.GetComponentInChildren<ClothSimControl>();
			return (sim != null);
		}

		private void OnVisible(bool b)
		{
			if (ignore_) return;

			if (Item != null)
			{
				foreach (var mo in Item.GetComponentsInChildren<MaterialOptions>())
				{
					var j = mo.GetBoolJSONParam("hideMaterial");
					if (j != null)
					{
						try
						{
							// this can throw an nre in
							// MaterialOptions.SetMaterialHide(), not sure why
							j.val = !b;
						}
						catch (Exception)
						{
						}
					}
				}
			}
		}

		public void OpenPhysics()
		{
			ClothingUI.OpenPhysics(ClothingItem);
		}

		public void OpenAdjustments()
		{
			ClothingUI.OpenAdjustments(ClothingItem);
		}
	}



	class ClothingCurrentControls : DynamicItemsUI.CurrentControls
	{
		public ClothingCurrentControls(DynamicItemsUI.Controls c)
			: base(c)
		{
			AddWidget(new VUI.ToolButton("Undress all", () => OnDressAll(false)));
			AddWidget(new VUI.ToolButton("Re-dress all", () => OnDressAll(true)));
		}

		private void OnDressAll(bool b)
		{
			var items = Controls.AtomUI.GetItems();

			for (int i = 0; i < items.Length; ++i)
			{
				if (items[i].active)
				{
					var csc = items[i].GetComponentsInChildren<ClothSimControl>();

					for (int j = 0; j < csc.Length; ++j)
					{
						var allowDetach = csc[j].GetBoolJSONParam("allowDetach");
						if (allowDetach != null)
							allowDetach.val = !b;

						if (b)
						{
							var r = csc[j].GetAction("Reset");
							r?.actionCallback?.Invoke();
						}
					}
				}
			}
		}
	}


	class ClothingPanel : DynamicItemsUI.ItemPanel
	{
		private readonly VUI.Button adjustments_ = null;
		private readonly VUI.Button physics_ = null;

		public ClothingPanel(ClothingAtomInfo parent)
			: base(parent)
		{
			adjustments_ = AddWidget(new VUI.ToolButton("A", OpenAdjustments, "Adjustments"));
			physics_ = AddWidget(new VUI.ToolButton("P", OpenPhysics, "Physics"));

			Update();
		}

		public DAZClothingItem ClothingItem
		{
			get { return Item as DAZClothingItem; }
		}

		public void OpenPhysics()
		{
			ClothingUI.OpenPhysics(ClothingItem);
		}

		public void OpenAdjustments()
		{
			ClothingUI.OpenAdjustments(ClothingItem);
		}

		protected override void DoActiveChanged(bool b)
		{
			physics_.Enabled = b;
			adjustments_.Enabled = b;
		}
	}


	class ClothingAtomInfo : DynamicItemsUI.AtomUI
	{
		public ClothingAtomInfo(AtomClothingUIModifier uiMod, Atom a)
			: base("clothing", uiMod, a)
		{
		}

		protected override void DoSetActive(DAZDynamicItem item, bool b)
		{
			var ci = item as DAZClothingItem;
			ci.characterSelector.SetActiveClothingItem(ci, b);
		}

		protected override GenerateDAZDynamicSelectorUI DoGetSelectorUI()
		{
			return CharacterSelector?.clothingSelectorUI;
		}

		protected override DynamicItemsUI.ItemPanel DoCreateItemPanel()
		{
			return new ClothingPanel(this);
		}

		protected override DynamicItemsUI.CurrentControls DoCreateCurrentControls(
			DynamicItemsUI.Controls c)
		{
			return new ClothingCurrentControls(c);
		}

		protected override DynamicItemsUI.CurrentControls.ItemPanel DoCreateCurrentItemPanel(
			DynamicItemsUI.Controls c)
		{
			return new CurrentClothingPanel(c);
		}

		protected override DAZDynamicItem[] DoGetItems()
		{
			return CharacterSelector.clothingItems;
		}

		protected override void DoRescan()
		{
			CharacterSelector.RefreshDynamicClothes();
		}
	}


	class AtomClothingUIModifier : AtomUIModifier
	{
		public AtomClothingUIModifier(ClothingUI parent)
			: base("aui.clothing")
		{
		}

		protected override BasicAtomUIInfo CreateAtomInfo(Atom a)
		{
			return new ClothingAtomInfo(this, a);
		}

		protected override bool ValidAtom(Atom a)
		{
			return (a.category == "People");
		}
	}


	class ClothingUI : UIReplacementFeature
	{
		private readonly AtomClothingUIModifier uiMod_;

		public ClothingUI()
			: base("clothing", "Clothing UI", true)
		{
			uiMod_ = new AtomClothingUIModifier(this);
		}

		public override string Description
		{
			get
			{
				return
					"Complete overhaul of the clothing panel.";
			}
		}


		public static void OpenPhysics(DAZClothingItem item)
		{
			DynamicItemsUI.AtomUI.OpenUI(item, "Physics");
		}

		public static void OpenAdjustments(DAZClothingItem item)
		{
			DynamicItemsUI.AtomUI.OpenUI(item, "Adjustments");
		}


		protected override void DoEnable()
		{
			uiMod_.Enable();
		}

		protected override void DoDisable()
		{
			uiMod_.Disable();
		}

		protected override void DoUpdate(float s)
		{
			base.DoUpdate(s);
			uiMod_.Update(s);
		}
	}
}
