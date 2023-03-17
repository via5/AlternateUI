namespace AUI.HairUI
{
	class CurrentHairPanel : DynamicItemsUI.CurrentControls.ItemPanel
	{
		private readonly VUI.Button physics_;
		private readonly VUI.Button lighting_;
		private readonly VUI.Button scalp_;

		public CurrentHairPanel(DynamicItemsUI.Controls c)
			: base(c)
		{
			physics_ = AddWidget(new VUI.ToolButton("P", OpenPhysics, "Physics"));
			lighting_ = AddWidget(new VUI.ToolButton("L", OpenLighting, "Lighting"));
			scalp_ = AddWidget(new VUI.ToolButton("S", OpenScalp, "Scalp"));
		}

		public DAZHairGroup HairItem
		{
			get { return Item as DAZHairGroup; }
		}

		protected override void DoUpdate()
		{
		}

		protected override void DoActiveChanged(bool b)
		{
			physics_.Enabled = b;
			lighting_.Enabled = b;
			scalp_.Enabled = b;
		}

		public void OpenPhysics()
		{
			HairUI.OpenPhysics(HairItem);
		}

		public void OpenLighting()
		{
			HairUI.OpenLighting(HairItem);
		}

		public void OpenScalp()
		{
			HairUI.OpenScalp(HairItem);
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
		private readonly VUI.Button physics_;
		private readonly VUI.Button lighting_;
		private readonly VUI.Button scalp_;

		public HairPanel(HairAtomInfo parent)
			: base(parent)
		{
			physics_ = AddWidget(new VUI.ToolButton("P", OpenPhysics, "Physics"));
			lighting_ = AddWidget(new VUI.ToolButton("L", OpenLighting, "Lighting"));
			scalp_ = AddWidget(new VUI.ToolButton("S", OpenScalp, "Scalp"));

			Update();
		}

		public DAZHairGroup HairItem
		{
			get { return Item as DAZHairGroup; }
		}

		public void OpenPhysics()
		{
			HairUI.OpenPhysics(HairItem);
		}

		public void OpenLighting()
		{
			HairUI.OpenLighting(HairItem);
		}

		public void OpenScalp()
		{
			HairUI.OpenScalp(HairItem);
		}

		protected override void DoActiveChanged(bool b)
		{
			physics_.Enabled = b;
			lighting_.Enabled = b;
			scalp_.Enabled = b;
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


		public static void OpenPhysics(DAZHairGroup item)
		{
			DynamicItemsUI.AtomUI.OpenUI(item, "Physics");
		}

		public static void OpenLighting(DAZHairGroup item)
		{
			DynamicItemsUI.AtomUI.OpenUI(item, "Lighting");
		}

		public static void OpenScalp(DAZHairGroup item)
		{
			DynamicItemsUI.AtomUI.OpenUI(item, "Scalp");
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
