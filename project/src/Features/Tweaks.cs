using System;
using System.Collections;
using System.Collections.Generic;
using uFileBrowser;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AUI.Tweaks
{
	class QuickSaveScreenshot : TweakFeature
	{
		public QuickSaveScreenshot()
			: base("quickSaveScreenshot", "Quick save with SS (Shift+F2)", false)
		{
		}

		public override string Description
		{
			get
			{
				return
					"Saves the current scene without confirmation but asks " +
					"for new screenshot.";
			}
		}

		protected override void DoUpdate(float s)
		{
			if (Input.GetKeyDown(KeyCode.F2) && Input.GetKey(KeyCode.LeftShift))
			{
				var sc = SuperController.singleton;

				try
				{
					SuperController.LogError($"saving {sc.LoadedSceneName}");
					sc.SaveConfirm("Overwrite Current");
				}
				catch (Exception e)
				{
					SuperController.LogError(e.ToString());
				}
			}
		}
	}


	class QuickSave : TweakFeature
	{
		public QuickSave()
			: base("quickSave", "Quick save no SS (Shift+F3)", false)
		{
		}

		public override string Description
		{
			get
			{
				return
					"Saves the current scene without confirmation and keeps " +
					"old screenshot.";
			}
		}

		protected override void DoUpdate(float s)
		{
			if (Input.GetKeyDown(KeyCode.F3) && Input.GetKey(KeyCode.LeftShift))
			{
				var sc = SuperController.singleton;

				var oldCamera = sc.screenshotCamera;
				var oldActiveUI = sc.activeUI;

				try
				{
					sc.screenshotCamera = null;
					SuperController.LogError($"saving {sc.LoadedSceneName}");
					sc.SaveConfirm("Overwrite Current");
					sc.activeUI = oldActiveUI;
				}
				catch (Exception e)
				{
					SuperController.LogError(e.ToString());
				}
				finally
				{
					sc.screenshotCamera = oldCamera;
				}
			}
		}
	}


	class EditMode : TweakFeature
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


	class SpaceBarFreeze : TweakFeature
	{
		public SpaceBarFreeze()
			: base("spaceBarFreeze", "Spacebar freeze", false)
		{
		}

		public override string Description
		{
			get
			{
				return
					"Toggle freeze with spacebar.";
			}
		}

		protected override void DoUpdate(float s)
		{
			if (Input.GetKeyUp(KeyCode.Space))
			{
				SuperController.singleton.SetFreezeAnimation(
					!SuperController.singleton.freezeAnimation);
			}
		}
	}


	class HideTargetsInVR : TweakFeature
	{
		private class AtomInfo
		{
			public readonly Atom atom;
			public bool hidden = false;
			public bool ignore = false;
			public Collider collider = null;

			public AtomInfo(Atom a)
			{
				atom = a;
			}
		}

		private const float UpdateInterval = 2;

		private AtomInfo[] atoms_ = new AtomInfo[0];
		private float elapsed_ = 0;

		public HideTargetsInVR()
			: base("hideInVR", "Disable selecting hidden targets", false)
		{
		}

		public override string Description
		{
			get
			{
				return
					"Targets that are Hidden and Not Interactable in Play " +
					"Mode will never be targetable.";
			}
		}

		protected override void DoEnable()
		{
			SuperController.singleton.onSceneLoadedHandlers += UpdateAtoms;
			SuperController.singleton.onAtomAddedHandlers += (a) => UpdateAtoms();
			SuperController.singleton.onAtomRemovedHandlers += (a) => UpdateAtoms();

			UpdateAtoms();
			CheckAll();
		}

		protected override void DoDisable()
		{
			UnsetAll();

			SuperController.singleton.onSceneLoadedHandlers -= UpdateAtoms;
			SuperController.singleton.onAtomAddedHandlers -= (a) => UpdateAtoms();
			SuperController.singleton.onAtomRemovedHandlers -= (a) => UpdateAtoms();
		}

		protected override void DoUpdate(float s)
		{
			elapsed_ += s;
			if (elapsed_ >= UpdateInterval)
			{
				elapsed_ = 0;
				CheckAll();
			}
		}

		private void CheckAll()
		{
			for (int i = 0; i < atoms_.Length; ++i)
			{
				var a = atoms_[i];

				if (ShouldHide(a.atom))
				{
					if (!a.hidden)
					{
						a.hidden = true;
						SetHidden(a, true);
					}
				}
				else
				{
					if (a.hidden)
					{
						a.hidden = false;
						SetHidden(a, false);
					}
				}
			}
		}

		private bool ShouldHide(Atom a)
		{
			if (a?.mainController == null)
				return false;

			return (a.hidden && !a.mainController.interactableInPlayMode);
		}

		private void UpdateAtoms()
		{
			UnsetAll();

			var list = new List<AtomInfo>();
			var atoms = SuperController.singleton.GetAtoms();

			for (int i = 0; i < atoms.Count; ++i)
				list.Add(new AtomInfo(atoms[i]));

			atoms_ = list.ToArray();
		}

		private void UnsetAll()
		{
			for (int i = 0; i < atoms_.Length; ++i)
			{
				if (atoms_[i].hidden)
				{
					atoms_[i].hidden = false;
					SetHidden(atoms_[i], false);
				}
			}
		}

		private void SetHidden(AtomInfo a, bool b)
		{
			if (a.ignore)
				return;

			Log.Verbose($"set hidden {a.atom.uid} {b}");

			if (a.collider == null)
			{
				a.collider = a.atom?.mainController?.GetComponent<Collider>();

				if (a.collider == null)
				{
					Log.Error("no collider in mainController");
					a.ignore = true;
				}
			}

			if (a.collider != null)
				a.collider.enabled = !b;
		}
	}


	class FocusHead : TweakFeature
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


	class DisableLoadPosition : TweakFeature
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


	class MoveNewLight : TweakFeature
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


	class DisableCuaCollision : TweakFeature
	{
		public DisableCuaCollision()
			: base("disableCuaCollision", "Disable collison on new CUAs", false)
		{
		}

		public override string Description
		{
			get
			{
				return
					"Disable collision on new CustomUnityAssets.";
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
			if (a.type == "CustomUnityAsset")
				a.collisionEnabled = false;
		}
	}


	class EscapeDialogs : TweakFeature
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

				TryCloseOverwrite();
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

		private void TryCloseOverwrite()
		{
			var p = SuperController.singleton.overwriteConfirmPanel;

			if (p.gameObject.activeInHierarchy)
			{
				var cancel = VUI.Utilities.FindChildRecursive(p, "CancelButton");
				var button = cancel?.GetComponent<UnityEngine.UI.Button>();
				button?.onClick?.Invoke();
			}
		}
	}


	class RightClickPackagesReload : TweakFeature
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
				yield return new UnityEngine.WaitForSeconds(0.5f);
				SuperController.singleton.RescanPackages();
				SuperController.LogMessage("done");
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


	class DoubleClickFocus : TweakFeature
	{
		private float lastDown_ = 0;
		private UnityEngine.UI.Text text_ = null;

		public DoubleClickFocus()
			: base("doubleClickFocus", "Double-click focus", true)
		{
		}

		public override string Description
		{
			get
			{
				return
					"Double-click the middle mouse button to focus the " +
					"controller.";
			}
		}

		public void Focus()
		{
			if (text_ == null)
			{
				var mm = SuperController.singleton.MonitorModeAuxUI;
				var h = VUI.Utilities.FindChildRecursive(mm, "HighlightText2");
				text_ = h.GetComponent<UnityEngine.UI.Text>();
			}

			if (text_ != null && text_.gameObject.activeInHierarchy)
			{
				var fc = SuperController.singleton.FreeControllerNameToFreeController(text_.text);
				if (fc != null)
					AlternateUI.Instance.StartCoroutine(CoFocus(fc));
			}
		}

		private IEnumerator CoFocus(FreeControllerV3 fc)
		{
			yield return new WaitForEndOfFrame();
			SuperController.singleton.FocusOnController(fc);
		}

		protected override void DoInit()
		{
		}

		protected override void DoEnable()
		{
		}

		protected override void DoDisable()
		{
		}

		protected override void DoUpdate(float s)
		{
			if (!EventSystem.current.IsPointerOverGameObject())
			{
				if (Input.GetMouseButtonDown(2))
				{
					var now = Time.unscaledTime;
					if (now - lastDown_ < 0.3f)
					{
						Focus();
					}

					lastDown_ = now;
				}
			}
		}
	}
}
