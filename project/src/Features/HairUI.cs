using UnityEngine;
using System.Collections;
using AUI.ClothingUI;

namespace AUI.HairUI
{
	class CurrentHairPanel : DynamicItemsUI.CurrentControls.ItemPanel
	{
		public CurrentHairPanel(DynamicItemsUI.Controls c)
			: base(c)
		{
		}

		public DAZHairGroup ClothingItem
		{
			get { return Item as DAZHairGroup; }
		}

		protected override void DoUpdate()
		{
		}
	}


	class HairCurrentControls : DynamicItemsUI.CurrentControls
	{
		public HairCurrentControls(DynamicItemsUI.Controls c)
			: base(c)
		{
		}
	}


	class HairPanel : DynamicItemsUI.ItemPanel
	{
		public HairPanel(HairAtomInfo parent)
			: base(parent)
		{
			Update();
		}

		public DAZHairGroup ClothingItem
		{
			get { return Item as DAZHairGroup; }
		}

		protected override void DoActiveChanged(bool b)
		{
		}
	}


	class HairAtomInfo : DynamicItemsUI.AtomUI
	{
		public HairAtomInfo(AtomHairUIModifier uiMod, Atom a)
			: base("hair", uiMod, a)
		{
		}

		protected override void DoSetActive(DAZDynamicItem item, bool b)
		{
			var hi = item as DAZHairGroup;
			hi.characterSelector.SetActiveHairItem(hi, b);
		}

		protected override GenerateDAZDynamicSelectorUI DoGetSelectorUI()
		{
			return CharacterSelector?.hairSelectorUI;
		}

		protected override DynamicItemsUI.ItemPanel DoCreateItemPanel()
		{
			return new HairPanel(this);
		}

		protected override DynamicItemsUI.CurrentControls DoCreateCurrentControls(
			DynamicItemsUI.Controls c)
		{
			return new HairCurrentControls(c);
		}

		protected override DynamicItemsUI.CurrentControls.ItemPanel DoCreateCurrentItemPanel(
			DynamicItemsUI.Controls c)
		{
			return new CurrentHairPanel(c);
		}

		protected override DAZDynamicItem[] DoGetItems()
		{
			return CharacterSelector.hairItems;
		}
	}


	class AtomHairUIModifier : AtomUIModifier
	{
		public AtomHairUIModifier(HairUI parent)
			: base("aui.hair")
		{
		}

		protected override BasicAtomUIInfo CreateAtomInfo(Atom a)
		{
			return new HairAtomInfo(this, a);
		}

		protected override bool ValidAtom(Atom a)
		{
			return (a.category == "People");
		}
	}


	class HairUI : BasicFeature
	{
		private readonly AtomHairUIModifier uiMod_;

		public HairUI()
			: base("hair", "Hair UI", true)
		{
			uiMod_ = new AtomHairUIModifier(this);
		}

		public override string Description
		{
			get
			{
				return
					"Complete overhaul of the hair panel.";
			}
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
