using System;
using System.Collections;
using System.Collections.Generic;
using uFileBrowser;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
			try
			{
				SuperController.singleton.gameMode = SuperController.GameMode.Edit;
				SuperController.singleton.ShowMainHUD();
			}
			catch (Exception e)
			{
				Log.Error($"exception in OnSceneLoaded:");
				Log.Error(e.ToString());
			}
		}
	}


	class SelectAtomOnAdd : TweakFeature
	{
		public SelectAtomOnAdd()
			: base("selectAtomOnAdd", "Always select atom on Add", false)
		{
		}

		public override string Description
		{
			get
			{
				return
					"Activates the 'Select Atom On Add' checkbox on startup.";
			}
		}

		protected override void DoEnable()
		{
			if (SuperController.singleton?.selectAtomOnAddToggle != null)
				SuperController.singleton.selectAtomOnAddToggle.isOn = true;
		}

		protected override void DoDisable()
		{
		}
	}


	class FocusAtomOnAdd : TweakFeature
	{
		public FocusAtomOnAdd()
			: base("focusAtomOnAdd", "Always focus atom on Add", false)
		{
		}

		public override string Description
		{
			get
			{
				return
					"Activates the 'Focus Atom On Add' checkbox on startup.";
			}
		}

		protected override void DoEnable()
		{
			if (SuperController.singleton?.focusAtomOnAddToggle != null)
				SuperController.singleton.focusAtomOnAddToggle.isOn = true;
		}

		protected override void DoDisable()
		{
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
				if (!InInputField())
				{
					SuperController.singleton.SetFreezeAnimation(
						!SuperController.singleton.freezeAnimation);
				}
			}
		}

		private bool InInputField()
		{
			var s = EventSystem.current.currentSelectedGameObject
				?.GetComponent<InputField>();

			return s?.isFocused ?? false;
		}
	}


	class HideTargets : TweakFeature
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

		public HideTargets()
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
			try
			{
				UnsetAll();

				var list = new List<AtomInfo>();
				var atoms = SuperController.singleton.GetAtoms();

				for (int i = 0; i < atoms.Count; ++i)
					list.Add(new AtomInfo(atoms[i]));

				atoms_ = list.ToArray();
			}
			catch (Exception e)
			{
				Log.Error($"exception in UpdateAtoms:");
				Log.Error(e.ToString());
			}
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


	class HideControllers : TweakFeature
	{
		private class ControllerInfo
		{
			public readonly FreeControllerV3 controller;
			public bool hidden = false;
			public bool ignore = false;
			public Collider collider = null;

			public ControllerInfo(FreeControllerV3 c)
			{
				controller = c;
			}

			public override string ToString()
			{
				return $"{controller?.containingAtom?.uid}.{controller.name}";
			}
		}

		private class ControllerLinkInfo
		{
			public readonly FreeControllerV3Link link;
			public bool hidden = false;
			public bool ignore = false;
			public Collider collider = null;

			public ControllerLinkInfo(FreeControllerV3Link ln)
			{
				link = ln;
			}

			public override string ToString()
			{
				return $"{link?.linkedController?.containingAtom?.uid}.{link?.linkedController.name}.{link.name}";
			}
		}


		private class AtomInfo
		{
			public readonly Atom atom;
			public ControllerInfo[] controllers;
			public ControllerLinkInfo[] links;

			public AtomInfo(Atom a)
			{
				atom = a;

				var list = new List<ControllerInfo>();
				foreach (var fc in a.freeControllers)
					list.Add(new ControllerInfo(fc));

				var list2 = new List<ControllerLinkInfo>();
				foreach (var ln in a.GetComponentsInChildren<FreeControllerV3Link>())
					list2.Add(new ControllerLinkInfo(ln));

				controllers = list.ToArray();
				links = list2.ToArray();
			}
		}

		private const float UpdateInterval = 2;

		private AtomInfo[] atoms_ = new AtomInfo[0];
		private float elapsed_ = 0;

		public HideControllers()
			: base("hideControllers", "Disable selecting off controllers", false)
		{
		}

		public override string Description
		{
			get
			{
				return
					"Controllers of Person atoms that are off for both " +
					"position and rotation will never be targetable.";
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

				for (int j = 0; j < a.controllers.Length; ++j)
				{
					var c = a.controllers[j];

					if (ShouldHide(c.controller))
					{
						if (!c.hidden)
						{
							c.hidden = true;
							SetHidden(c, true);
						}
					}
					else
					{
						if (c.hidden)
						{
							c.hidden = false;
							SetHidden(c, false);
						}
					}
				}

				for (int j = 0; j < a.links.Length; ++j)
				{
					var ln = a.links[j];

					if (ShouldHide(ln.link.linkedController))
					{
						if (!ln.hidden)
						{
							ln.hidden = true;
							SetHidden(ln, true);
						}
					}
					else
					{
						if (ln.hidden)
						{
							ln.hidden = false;
							SetHidden(ln, false);
						}
					}
				}
			}
		}

		private bool ShouldHide(FreeControllerV3 a)
		{
			if (a.name.Contains("lHand"))
				return false;

			if (a.name.Contains("rHand"))
				return false;

			return
				a.currentPositionState == FreeControllerV3.PositionState.Off &&
				a.currentRotationState == FreeControllerV3.RotationState.Off;
		}

		private void UpdateAtoms()
		{
			try
			{
				UnsetAll();

				var list = new List<AtomInfo>();
				var atoms = SuperController.singleton.GetAtoms();

				for (int i = 0; i < atoms.Count; ++i)
					list.Add(new AtomInfo(atoms[i]));

				atoms_ = list.ToArray();
			}
			catch (Exception e)
			{
				Log.Error($"exception in UpdateAtoms:");
				Log.Error(e.ToString());
			}
		}

		private void UnsetAll()
		{
			for (int i = 0; i < atoms_.Length; ++i)
			{
				for (int j = 0; j < atoms_[i].controllers.Length; ++j)
				{
					var c = atoms_[i].controllers[j];

					if (c.hidden)
					{
						c.hidden = false;
						SetHidden(c, false);
					}
				}
			}
		}

		private void SetHidden(ControllerInfo c, bool b)
		{
			if (c.ignore)
				return;

			Log.Verbose($"set hidden {c} {b}");

			if (c.collider == null)
			{
				c.collider = c.controller?.control?.GetComponent<Collider>();

				if (c.collider == null)
				{
					Log.Error($"no collider in {c}");
					c.ignore = true;
				}
			}

			if (c.collider != null)
				c.collider.enabled = !b;
		}

		private void SetHidden(ControllerLinkInfo ln, bool b)
		{
			if (ln.ignore)
				return;

			Log.Verbose($"set hidden {ln} {b}");

			if (ln.collider == null)
			{
				ln.collider = ln.link.GetComponent<Collider>();

				if (ln.collider == null)
				{
					Log.Error($"no collider in {ln}");
					ln.ignore = true;
				}
			}

			if (ln.collider != null)
				ln.collider.enabled = !b;
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
			try
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
			catch (Exception e)
			{
				Log.Error($"exception in OnSceneLoaded:");
				Log.Error(e.ToString());
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
			try
			{
				SuperController.singleton.useSceneLoadPosition = false;
			}
			catch (Exception e)
			{
				Log.Error($"exception in OnSceneLoaded:");
				Log.Error(e.ToString());
			}
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
			try
			{
				if (a.type == "InvisibleLight")
				{
					a.mainController.MoveControl(new Vector3(
						a.mainController.transform.position.x,
						a.mainController.transform.position.y,
						0.6f));
				}
			}
			catch (Exception e)
			{
				Log.Error($"exception in OnAtomAdded for {a}:");
				Log.Error(e.ToString());
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
			try
			{
				if (a.type == "CustomUnityAsset")
					a.collisionEnabled = false;
			}
			catch (Exception e)
			{
				Log.Error($"exception in OnAtomAdded for {a}:");
				Log.Error(e.ToString());
			}
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


		private GameObject selectedObject_ = null;
		private InputField inputField_ = null;
		private bool inputFieldWasFocused_ = false;

		protected override void DoUpdate(float s)
		{
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				var sc = SuperController.singleton;

				// ignore escape if the focus is on a textfield, it resets it
				if (CancelledTextField())
					return;

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

			CheckSelectedObject();
		}

		private void CheckSelectedObject()
		{
			var s = EventSystem.current.currentSelectedGameObject;

			if (s != selectedObject_)
			{
				selectedObject_ = s;
				inputField_ = s?.GetComponent<InputField>();
			}

			if (inputField_ != null)
			{
				if (inputFieldWasFocused_ != inputField_.isFocused)
					inputFieldWasFocused_ = inputField_.isFocused;
			}
		}

		private bool CancelledTextField()
		{
			if (inputField_ != null)
			{
				if (inputFieldWasFocused_ && !inputField_.isFocused)
				{
					inputFieldWasFocused_ = false;
					if (inputField_.wasCanceled)
						return true;
				}

				inputFieldWasFocused_ = inputField_.isFocused;
			}

			return false;
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
			: base("doubleClickFocus", "Double-click focus", false)
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

			Log.Verbose($"focusing on {fc}");
			SuperController.singleton.FocusOnController(fc);
		}

		protected override void DoUpdate(float s)
		{
			if (Input.GetMouseButtonDown(2))
			{
				if (!EventSystem.current.IsPointerOverGameObject())
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


	class LoadingIndicator : TweakFeature
	{
		class Icon
		{
			public GameObject o;
			public Transform icon;
		}

		private GameObject root_ = null;
		private Icon left_ = null;
		private Icon right_ = null;
		private bool wasLoading_ = false;
		private Transform imageLoading_ = null;


		public LoadingIndicator()
			: base("loadingIndicator", "Scene loading indicator", true)
		{
		}

		public override string Description
		{
			get
			{
				return
					"Adds a spinning icon in the top left while a scene is " +
					"loading.";
			}
		}


		protected override void DoEnable()
		{
			if (imageLoading_ == null)
			{
				imageLoading_ = VUI.Utilities.FindChildRecursive(
					SuperController.singleton,
					"ImageLoadingHUD")?.transform;
			}
		}

		private Icon CreateIcon(bool left)
		{
			if (root_ == null)
			{
				root_ = new GameObject("aui.LoadingIcon");
				root_.transform.SetParent(SuperController.singleton.transform.root, false);
			}

			var o = new GameObject($"aui.LoadingIcon.{(left ? "Left" : "Right")}");
			o.transform.SetParent(root_.transform);

			var canvas = o.AddComponent<Canvas>();
			o.AddComponent<CanvasRenderer>();
			o.AddComponent<CanvasScaler>();

			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.gameObject.AddComponent<GraphicRaycaster>();
			canvas.scaleFactor = 0.5f;
			canvas.pixelPerfect = true;

			var icon = new GameObject("OverlayRootSupportUI");
			icon.transform.SetParent(o.transform, false);
			var rt = icon.AddComponent<RectTransform>();

			var bg = icon.AddComponent<UnityEngine.UI.Image>();
			bg.color = new Color(1, 0, 0, 1);
			bg.raycastTarget = true;

			if (left)
			{
				VUI.Utilities.SetRectTransform(rt, VUI.Rectangle.FromPoints(
					20, 20, 100, 100));
			}
			else
			{
				var r = VUI.Rectangle.FromPoints(20, 20, 100, 100);

				var center = new VUI.Point(
					Mathf.Round(r.Left) + Mathf.Round((r.Right - r.Left)) / 2,
					Mathf.Round(r.Top) + Mathf.Round((r.Bottom - r.Top)) / 2);

				rt.offsetMin = new Vector2(Mathf.Round(-r.Right), Mathf.Round(-r.Bottom));
				rt.offsetMax = new Vector2(Mathf.Round(-r.Left), Mathf.Round(-r.Top));
				rt.anchorMin = new Vector2(1, 1);
				rt.anchorMax = new Vector2(1, 1);
				rt.anchoredPosition = new Vector2(-center.X, -center.Y);
			}

			var i = new Icon();
			i.o = o;
			i.icon = icon.transform;

			return i;
		}

		protected override void DoDisable()
		{
			if (left_ != null)
			{
				UnityEngine.Object.Destroy(left_.o);
				left_ = null;
			}

			if (right_ != null)
			{
				UnityEngine.Object.Destroy(right_.o);
				right_ = null;
			}
		}

		float a = 0;

		protected override void DoUpdate(float s)
		{
			if (IsVR())
			{
				root_?.SetActive(false);
				return;
			}

			var sc = SuperController.singleton;
			var isLoading = IsLoading();

			if (isLoading != wasLoading_)
			{
				if (left_ == null)
					left_ = CreateIcon(true);

				if (right_ == null)
					right_ = CreateIcon(false);

				left_.o.SetActive(isLoading);
				right_.o.SetActive(isLoading);

				wasLoading_ = isLoading;
			}

			if (isLoading)
			{
				if (left_ != null)
					left_.icon.transform.localRotation = Quaternion.Euler(0, 0, a);

				if (right_ != null)
					right_.icon.transform.localRotation = Quaternion.Euler(0, 0, a);

				a += 1;
				if (a >= 360)
					a = 0;
			}
		}

		private bool IsLoading()
		{
			var sc = SuperController.singleton;

			return
				(sc.isLoading) ||
				(sc.loadingUI?.gameObject?.activeInHierarchy ?? false) ||
				(sc.loadingGeometry?.gameObject?.activeInHierarchy ?? false) ||
				(sc.loadingIcon?.gameObject?.activeInHierarchy ?? false) ||
				(sc.loadingProgressSlider?.gameObject?.activeInHierarchy ?? false) ||
				(sc.loadingTextStatus?.gameObject?.activeInHierarchy ?? false) ||
				(imageLoading_.gameObject?.activeInHierarchy ?? false);
		}

		public bool IsVR()
		{
			return
				!SuperController.singleton.MonitorCenterCamera.isActiveAndEnabled &&
				!SuperController.singleton.IsMonitorRigActive;
		}
	}
}
