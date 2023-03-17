using UnityEngine;
using System.Collections;

namespace AUI.ClothingUI
{
	class CurrentClothingPanel : DynamicItemsUI.CurrentControls.ItemPanel
	{
		private readonly VUI.Button customize_;
		private readonly VUI.Button adjustments_;
		private readonly VUI.Button physics_;
		private readonly VUI.CheckBox visible_;
		private bool ignore_ = false;

		public CurrentClothingPanel(DynamicItemsUI.Controls c)
			: base(c)
		{
			customize_ = AddWidget(new VUI.ToolButton("...", OpenCustomize, "Customize"));
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
				customize_.Enabled = true;
				adjustments_.Enabled = true;
				physics_.Enabled = HasSim();
				visible_.Checked = IsClothingVisible();
			}
			finally
			{
				ignore_ = false;
			}
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
						j.val = !b;
				}
			}
		}

		public void OpenCustomize()
		{
			ClothingUI.OpenCustomizeClothingUI(ClothingItem);
		}

		public void OpenPhysics()
		{
			ClothingUI.OpenPhysicsClothingUI(ClothingItem);
		}

		public void OpenAdjustments()
		{
			ClothingUI.OpenAdjustmentsClothingUI(ClothingItem);
		}
	}



	class ClothingPanel : DynamicItemsUI.ItemPanel
	{
		private readonly ClothingAtomInfo parent_;
		private readonly VUI.Button customize_ = null;
		private readonly VUI.Button adjustments_ = null;
		private readonly VUI.Button physics_ = null;

		public ClothingPanel(ClothingAtomInfo parent)
			: base(parent)
		{
			parent_ = parent;
			customize_ = AddButton(new VUI.ToolButton("...", OpenCustomize, "Customize"));
			adjustments_ = AddButton(new VUI.ToolButton("A", OpenAdjustments, "Adjustments"));
			physics_ = AddButton(new VUI.ToolButton("P", OpenPhysics, "Physics"));

			Update();
		}

		public DAZClothingItem ClothingItem
		{
			get { return Item as DAZClothingItem; }
		}

		public void OpenCustomize()
		{
			ClothingUI.OpenCustomizeClothingUI(ClothingItem);
		}

		public void OpenPhysics()
		{
			ClothingUI.OpenPhysicsClothingUI(ClothingItem);
		}

		public void OpenAdjustments()
		{
			ClothingUI.OpenAdjustmentsClothingUI(ClothingItem);
		}

		protected override void DoActiveChanged(bool b)
		{
			customize_.Enabled = b;
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

		protected override DynamicItemsUI.CurrentControls.ItemPanel DoCreateCurrentItemPanel(
			DynamicItemsUI.Controls c)
		{
			return new CurrentClothingPanel(c);
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


	class ClothingUI : BasicFeature
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


		public static void OpenCustomizeClothingUI(DAZClothingItem item)
		{
			OpenClothingUI(item);
		}

		public static void OpenPhysicsClothingUI(DAZClothingItem item)
		{
			OpenClothingUI(item, "Physics");
		}

		public static void OpenAdjustmentsClothingUI(DAZClothingItem item)
		{
			OpenClothingUI(item, "Adjustments");
		}

		private static void OpenClothingUI(DAZClothingItem ci, string tab = null)
		{
			if (ci == null)
				return;

			ci.OpenUI();

			if (!string.IsNullOrEmpty(tab))
				SetTab(ci, tab);
		}

		private static void SetTab(DAZClothingItem ci, string name)
		{
			DoSetTab(ci, name);
			AlternateUI.Instance.StartCoroutine(CoSetTab(ci, name));
		}

		private static IEnumerator CoSetTab(DAZClothingItem ci, string name)
		{
			yield return new WaitForEndOfFrame();
			yield return new WaitForEndOfFrame();
			DoSetTab(ci, name);
		}

		private static void DoSetTab(DAZClothingItem ci, string name)
		{
			var ts = ci.customizationUI.GetComponentInChildren<UITabSelector>();
			if (ts == null)
				return;

			if (ts.HasTab(name))
				ts.SetActiveTab(name);
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
