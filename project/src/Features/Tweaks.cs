using System;
using System.Collections;
using uFileBrowser;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AUI.Tweaks
{
	class EditMode : BasicFeature
	{
		public EditMode()
			: base("editMode", "Edit mode on load", false)
		{
		}

		public override string Description
		{
			get
			{
				return
					"Switch to Edit mode when loading a scene.";
			}
		}

		protected override void DoEnable()
		{
			SuperController.singleton.onSceneLoadedHandlers += OnSceneLoaded;
			OnSceneLoaded();
		}

		protected override void DoDisable()
		{
			SuperController.singleton.onSceneLoadedHandlers -= OnSceneLoaded;
		}

		private void OnSceneLoaded()
		{
			SuperController.singleton.gameMode = SuperController.GameMode.Edit;
			SuperController.singleton.ShowMainHUD();
		}
	}


	class FocusHead : BasicFeature
	{
		public FocusHead()
			: base("focusHead", "Focus head on load", false)
		{
		}

		public override string Description
		{
			get
			{
				return
					"Always focus the head of the first Person atom when " +
					"loading a scene. Works best with Disable Load Position " +
					"enabled.";
			}
		}

		protected override void DoEnable()
		{
			SuperController.singleton.onSceneLoadedHandlers += OnSceneLoaded;
			OnSceneLoaded();
		}

		protected override void DoDisable()
		{
			SuperController.singleton.onSceneLoadedHandlers -= OnSceneLoaded;
		}

		private void OnSceneLoaded()
		{
			var atoms = SuperController.singleton.GetAtoms();

			foreach (var a in atoms)
			{
				if (a.type != "Person")
					continue;

				foreach (var fc in a.freeControllers)
				{
					if (fc.name == "headControl")
					{
						SuperController.singleton.FocusOnController(fc);
						return;
					}
				}
			}
		}
	}


	class DisableLoadPosition : BasicFeature
	{
		public DisableLoadPosition()
			: base("disableLoadPosition", "Disable load position", false)
		{
		}

		public override string Description
		{
			get
			{
				return
					"Never use the scene's load position, remember the last " +
					"camera position.";
			}
		}

		protected override void DoEnable()
		{
			SuperController.singleton.onSceneLoadedHandlers += OnSceneLoaded;
			OnSceneLoaded();
		}

		protected override void DoDisable()
		{
			SuperController.singleton.onSceneLoadedHandlers -= OnSceneLoaded;
		}

		private void OnSceneLoaded()
		{
			SuperController.singleton.useSceneLoadPosition = false;
		}
	}


	class MoveNewLight : BasicFeature
	{
		public MoveNewLight()
			: base("moveNewLight", "Move new lights", false)
		{
		}

		public override string Description
		{
			get
			{
				return
					"Slightly move new InvisibleLight atoms forwards so they " +
					"better illuminate atoms in the center.";
			}
		}

		protected override void DoEnable()
		{
			SuperController.singleton.onAtomAddedHandlers += OnAtomAdded;
		}

		protected override void DoDisable()
		{
			SuperController.singleton.onAtomAddedHandlers -= OnAtomAdded;
		}

		private void OnAtomAdded(Atom a)
		{
			if (a.type == "InvisibleLight")
			{
				a.mainController.MoveControl(new Vector3(
					a.mainController.transform.position.x,
					a.mainController.transform.position.y,
					0.6f));
			}
		}
	}


	class EscapeDialogs : BasicFeature
	{
		private UnityEngine.UI.Button closePackageManager_;
		private UnityEngine.UI.Button closePackageBuilder_;
		private UnityEngine.UI.Button closeErrorLog_;
		private UnityEngine.UI.Button closeMessageLog_;

		public EscapeDialogs()
			: base("escapeDialogs", "Escape closes dialogs", true)
		{
		}

		public override string Description
		{
			get
			{
				return
					"Close dialogs with the escape key.";
			}
		}

		protected override void DoUpdate(float s)
		{
			if (Input.GetKey(KeyCode.Escape))
			{
				var sc = SuperController.singleton;

				TryClose(sc.fileBrowserUI);
				TryClose(sc.fileBrowserWorldUI);
				TryClose(sc.templatesFileBrowserWorldUI);
				TryClose(sc.mediaFileBrowserUI);
				TryClose(sc.directoryBrowserUI);

				TryCloseDialog(sc.packageManagerUI, ref closePackageManager_);
				TryCloseDialog(sc.packageBuilderUI, ref closePackageBuilder_);

				TryCloseLog(sc.errorLogPanel, ref closeErrorLog_);
				TryCloseLog(sc.msgLogPanel, ref closeMessageLog_);
			}
		}

		private void TryClose(FileBrowser b)
		{
			if (b?.isActiveAndEnabled ?? false)
				b.CancelButtonClicked();
		}

		private void TryCloseDialog(Transform dlg, ref UnityEngine.UI.Button b)
		{
			if (dlg.gameObject.activeInHierarchy)
			{
				if (b == null)
				{
					var cb = VUI.Utilities.FindChildRecursive(dlg, "CloseMainUIButton");
					b = cb.GetComponent<UnityEngine.UI.Button>();
				}

				b?.onClick?.Invoke();
			}
		}

		private void TryCloseLog(Transform dlg, ref UnityEngine.UI.Button b)
		{
			if (dlg.gameObject.activeInHierarchy)
			{
				if (b == null)
				{
					var cb = VUI.Utilities.FindChildRecursive(dlg, "OKButton");
					b = cb.GetComponent<UnityEngine.UI.Button>();
				}

				b?.onClick?.Invoke();
			}
		}
	}


	class RightClickPackagesReload : BasicFeature
	{
		private const string ButtonName = "ButtonOpenPackageManager";
		private UnityEngine.UI.Button button_ = null;

		class RightClickPackagesReloadMouseCallbacks : MonoBehaviour, IPointerClickHandler
		{
			private RightClickPackagesReload parent_ = null;

			public void Set(RightClickPackagesReload p)
			{
				parent_ = p;
			}

			public void OnPointerClick(PointerEventData d)
			{
				try
				{
					if (d.button == PointerEventData.InputButton.Right)
					{
						SuperController.LogMessage("rescanning packages");
						StartCoroutine(Rescan());
					}
				}
				catch (Exception e)
				{
					SuperController.LogError(e.ToString());
				}
			}

			private IEnumerator Rescan()
			{
				yield return new UnityEngine.WaitForEndOfFrame();
				SuperController.singleton.RescanPackages();
			}
		}


		public RightClickPackagesReload()
			: base("reloadPackages", "Right-click packages reload", true)
		{
		}

		public override string Description
		{
			get
			{
				return
					"Right-click the package manager button to reload all " +
					"packages.";
			}
		}

		protected override void DoInit()
		{
			button_ = VUI.Utilities.FindChildRecursive(
				SuperController.singleton, ButtonName)
					?.GetComponent<UnityEngine.UI.Button>();

			if (button_ == null)
			{
				SuperController.LogError($"{ButtonName} not found");
				return;
			}

			RemoveComponents();
		}

		protected override void DoEnable()
		{
			if (button_ == null)
				return;

			var c = button_.gameObject
				.AddComponent<RightClickPackagesReloadMouseCallbacks>();

			if (c == null)
			{
				SuperController.LogError("can't add component");
				return;
			}

			c.Set(this);
			c.enabled = true;
		}

		protected override void DoDisable()
		{
			RemoveComponents();
		}

		private void RemoveComponents()
		{
			foreach (Component c in button_.GetComponents<Component>())
			{
				if (c.ToString().Contains("RightClickPackagesReloadMouseCallbacks"))
					UnityEngine.Object.Destroy(c);
			}
		}
	}
}