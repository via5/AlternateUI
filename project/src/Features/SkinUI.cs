using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AUI.SkinUI
{
	class MouseCallbacks : MonoBehaviour, IPointerDownHandler
	{
		private RightClickSkinReload ui_ = null;
		private JSONStorableUrl url_ = null;

		public void Set(RightClickSkinReload ui, JSONStorableUrl url)
		{
			ui_ = ui;
			url_ = url;
		}

		public void OnPointerDown(PointerEventData d)
		{
			try
			{
				if (d != null && d.button == PointerEventData.InputButton.Right)
				{
					if (url_ != null)
					{
						ui_.Log.Info($"reloading {url_.name}");
						url_.Reload();
					}
				}
			}
			catch (Exception e)
			{
				ui_.Log.Error(e.ToString());
			}
		}
	}


	class RightClickSkinReload : BasicFeature
	{
		private SuperController sc_ = SuperController.singleton;

		public RightClickSkinReload()
			: base("skin", "Right-click skin texture reload", true)
		{
		}

		public override string Description
		{
			get
			{
				return
					"Right-click the Select button in the Skin Textures " +
					"panel to reload the texture.";
			}
		}

		protected override void DoInit()
		{
			RefreshCallbacks();
		}

		private void RefreshCallbacks()
		{
			foreach (var a in SuperController.singleton.GetAtoms())
				RefreshCallbacks(a);
		}

		private void RefreshCallbacks(Atom a)
		{
			RemoveCallbacks(a);
			AddCallbacks(a);
		}

		protected override void DoEnable()
		{
			sc_.onAtomAddedHandlers += AtomAdded;
			sc_.onAtomRemovedHandlers += AtomRemoved;
			sc_.onSceneLoadedHandlers += OnSceneLoaded;
		}

		protected override void DoDisable()
		{
			sc_.onAtomAddedHandlers -= AtomAdded;
			sc_.onAtomRemovedHandlers -= AtomRemoved;
			sc_.onSceneLoadedHandlers -= OnSceneLoaded;
		}

		private void AtomAdded(Atom a)
		{
			AlternateUI.Instance.StartCoroutine(RefreshCallbacksCo(a));
		}

		private void AtomRemoved(Atom a)
		{
			RemoveCallbacks(a);
		}

		private void OnSceneLoaded()
		{
			AlternateUI.Instance.StartCoroutine(RefreshCallbacksCo());
		}

		private IEnumerator RefreshCallbacksCo()
		{
			yield return new WaitForSeconds(1);
			RefreshCallbacks();
		}

		private IEnumerator RefreshCallbacksCo(Atom a)
		{
			yield return new WaitForSeconds(1);
			RefreshCallbacks(a);
		}

		private void AddCallbacks(Atom a)
		{
			var c = a.GetComponentInChildren<DAZCharacterSelector>();
			if (c == null)
				return;

			AddCallbacks(a,
				c.characterTextureUI?.faceDiffuseFileBrowseButton,
				"faceDiffuseUrl");

			AddCallbacks(a,
				c.characterTextureUI?.torsoDiffuseFileBrowseButton,
				"torsoDiffuseUrl");

			AddCallbacks(a,
				c.characterTextureUI?.limbsDiffuseFileBrowseButton,
				"limbsDiffuseUrl");

			AddCallbacks(a,
				c.characterTextureUI?.genitalsDiffuseFileBrowseButton,
				"genitalsDiffuseUrl");


			AddCallbacks(a,
				c.characterTextureUI?.faceDecalFileBrowseButton,
				"faceDecalUrl");

			AddCallbacks(a,
				c.characterTextureUI?.torsoDecalFileBrowseButton,
				"torsoDecalUrl");

			AddCallbacks(a,
				c.characterTextureUI?.limbsDecalFileBrowseButton,
				"limbsDecalUrl");

			AddCallbacks(a,
				c.characterTextureUI?.genitalsDecalFileBrowseButton,
				"genitalsDecalUrl");


			AddCallbacks(a,
				c.characterTextureUI?.faceSpecularFileBrowseButton,
				"faceSpecularUrl");

			AddCallbacks(a,
				c.characterTextureUI?.torsoSpecularFileBrowseButton,
				"torsoSpecularUrl");

			AddCallbacks(a,
				c.characterTextureUI?.limbsSpecularFileBrowseButton,
				"limbsSpecularUrl");

			AddCallbacks(a,
				c.characterTextureUI?.genitalsSpecularFileBrowseButton,
				"genitalsSpecularUrl");


			AddCallbacks(a,
				c.characterTextureUI?.faceNormalFileBrowseButton,
				"faceNormalUrl");

			AddCallbacks(a,
				c.characterTextureUI?.torsoNormalFileBrowseButton,
				"torsoNormalUrl");

			AddCallbacks(a,
				c.characterTextureUI?.limbsNormalFileBrowseButton,
				"limbsNormalUrl");

			AddCallbacks(a,
				c.characterTextureUI?.genitalsNormalFileBrowseButton,
				"genitalsNormalUrl");


			AddCallbacks(a,
				c.characterTextureUI?.faceGlossFileBrowseButton,
				"faceGlossUrl");

			AddCallbacks(a,
				c.characterTextureUI?.torsoGlossFileBrowseButton,
				"torsoGlossUrl");

			AddCallbacks(a,
				c.characterTextureUI?.limbsGlossFileBrowseButton,
				"limbsGlossUrl");

			AddCallbacks(a,
				c.characterTextureUI?.genitalsGlossFileBrowseButton,
				"genitalsGlossUrl");
		}

		private void AddCallbacks(Atom a, UnityEngine.UI.Button b, string param)
		{
			//Log.Info($"adding callback for atom {a?.uid}, button {b?.name}, param {param}");

			if (b == null)
			{
				//Log.Error("button is null");
				return;
			}

			var st = a.GetStorableByID("textures");
			if (st == null)
			{
				//Log.Error("textures storable is null");
				return;
			}

			var url = st.GetUrlJSONParam(param);
			if (url == null)
			{
				//Log.Error("url is null");
				return;
			}

			//Log.Info($"url is {url.val}");

			RemoveCallbacks(b);
			AddCallbacks(b, url);
		}

		private void AddCallbacks(UnityEngine.UI.Button bt, JSONStorableUrl url)
		{
			var mc = bt.GetComponent<MouseCallbacks>();

			if (mc != null)
				UnityEngine.Object.Destroy(mc);

			mc = bt.gameObject.AddComponent<MouseCallbacks>();

			mc.enabled = true;
			mc.Set(this, url);

			//Log.Info($"added MouseCallbacks to button {bt.name}, url={url.val}");
		}


		private void RemoveCallbacks(Atom a)
		{
			var c = a.GetComponentInChildren<DAZCharacterSelector>();
			if (c == null)
				return;

			var tui = c.characterTextureUI;
			if (tui == null)
				return;

			RemoveCallbacks(tui.faceDiffuseFileBrowseButton);
			RemoveCallbacks(tui.torsoDiffuseFileBrowseButton);
			RemoveCallbacks(tui.limbsDiffuseFileBrowseButton);
			RemoveCallbacks(tui.genitalsDiffuseFileBrowseButton);

			RemoveCallbacks(tui.faceDecalFileBrowseButton);
			RemoveCallbacks(tui.torsoDecalFileBrowseButton);
			RemoveCallbacks(tui.limbsDecalFileBrowseButton);
			RemoveCallbacks(tui.genitalsDecalFileBrowseButton);

			RemoveCallbacks(tui.faceSpecularFileBrowseButton);
			RemoveCallbacks(tui.torsoSpecularFileBrowseButton);
			RemoveCallbacks(tui.limbsSpecularFileBrowseButton);
			RemoveCallbacks(tui.genitalsSpecularFileBrowseButton);

			RemoveCallbacks(tui.faceNormalFileBrowseButton);
			RemoveCallbacks(tui.torsoNormalFileBrowseButton);
			RemoveCallbacks(tui.limbsNormalFileBrowseButton);
			RemoveCallbacks(tui.genitalsNormalFileBrowseButton);

			RemoveCallbacks(tui.faceGlossFileBrowseButton);
			RemoveCallbacks(tui.torsoGlossFileBrowseButton);
			RemoveCallbacks(tui.limbsGlossFileBrowseButton);
			RemoveCallbacks(tui.genitalsGlossFileBrowseButton);
		}

		private void RemoveCallbacks(UnityEngine.UI.Button bt)
		{
			if (bt == null)
				return;

			foreach (var c in bt.GetComponents<Component>())
			{
				if (c.GetType().ToString().Contains("SkinUI.MouseCallbacks"))
					UnityEngine.Object.Destroy(c);
			}
		}
	}


	class SkinMaterialsReset : BasicFeature
	{
		private const string ButtonName = "aui.skinui.resetmaterials";
		private const float DeferredCheckInterval = 2;

		private readonly List<Atom> deferred_ = new List<Atom>();
		private float deferredElapsed_ = 0;

		public SkinMaterialsReset()
			: base("skinReset", "Skin materials reset", true)
		{
		}

		public override string Description
		{
			get
			{
				return
					"Adds a reset button to the Skin Materials 2 UI.";
			}
		}

		protected override void DoInit()
		{
			Remove();
		}

		protected override void DoEnable()
		{
			SuperController.singleton.onAtomAddedHandlers += OnAtomAdded;
			SuperController.singleton.onAtomRemovedHandlers += OnAtomRemoved;
			SuperController.singleton.onSceneLoadedHandlers += OnSceneLoaded;

			Remove();
			Add();
		}

		protected override void DoDisable()
		{
			SuperController.singleton.onAtomAddedHandlers -= OnAtomAdded;
			SuperController.singleton.onAtomRemovedHandlers -= OnAtomRemoved;
			SuperController.singleton.onSceneLoadedHandlers -= OnSceneLoaded;

			Remove();
		}

		protected override void DoUpdate(float s)
		{
			if (deferred_.Count == 0)
				return;

			deferredElapsed_ += s;
			if (deferredElapsed_ >= DeferredCheckInterval)
			{
				deferredElapsed_ = 0;

				bool okay = true;

				for (int i = 0; i < deferred_.Count; ++i)
				{
					if (deferred_[i] == null)
						continue;

					if (Add(deferred_[i]))
					{
						Log.Verbose($"deferred atom {deferred_[i].uid} now okay");
						deferred_[i] = null;
					}
					else
					{
						okay = false;
					}
				}

				if (okay)
					deferred_.Clear();
			}
		}

		private void OnAtomAdded(Atom a)
		{
			Add(a);
		}

		private void OnAtomRemoved(Atom a)
		{
			deferred_.Remove(a);
			Remove(a);
		}

		private void OnSceneLoaded()
		{
			CheckScene();
		}

		private void CheckScene()
		{
			Remove();
			Add();
		}

		private void Remove()
		{
			foreach (var a in SuperController.singleton.GetAtoms())
			{
				if (!ValidAtom(a))
					continue;

				Remove(a);
			}

			deferred_.Clear();
		}

		private void Add()
		{
			foreach (var a in SuperController.singleton.GetAtoms())
			{
				if (!ValidAtom(a))
					continue;

				Add(a);
			}
		}

		private bool ValidAtom(Atom a)
		{
			if (a.category != "People")
				return false;

			if (a.uid == "[CameraRig]")
				return false;

			return true;
		}

		DAZCharacterMaterialOptions GetMO(Atom a)
		{
			var c = a.GetComponentInChildren<DAZCharacterSelector>();
			if (c == null)
			{
				Log.Verbose($"{a.uid} has no DAZCharacterSelector");
				return null;
			}

			if (c.selectedCharacter == null)
			{
				Log.Verbose($"{a.uid} has no selectedCharacter");
				return null;
			}

			return c.selectedCharacter
				.GetComponentInChildren<DAZCharacterMaterialOptions>();
		}

		Transform GetRef(Atom a)
		{
			var mo = GetMO(a);
			if (mo == null)
				return null;

			var parent = mo.param2Slider?.transform?.parent;
			Transform sm = null;

			while (parent != null)
			{
				if (parent.name == "Skin Materials 2")
				{
					sm = parent;
					break;
				}

				if (parent == parent.parent)
					break;

				if (parent.name == "Content")
					break;

				parent = parent.parent;
			}

			if (sm == null)
			{
				Log.Verbose($"{a.uid}: can't find Skin Materials 2");
				return null;
			}

			foreach (var sl in sm.GetComponentsInChildren<UIDynamicSlider>())
			{
				if (sl.labelText.text == "Global Illumination Filter")
					return sl.transform;
			}

			Log.Verbose($"{a.uid} has no Global Illumination Filter slider");

			return null;
		}

		private void Remove(Atom a)
		{
			var r = GetRef(a);
			if (r == null)
				return;

			foreach (Transform t in r.parent)
			{
				if (t.name.StartsWith(ButtonName))
					UnityEngine.Object.Destroy(t.gameObject);
			}
		}

		private bool Add(Atom a)
		{
			var r = GetRef(a);
			if (r == null)
			{
				if (!deferred_.Contains(a))
				{
					Log.Verbose($"{a.uid}: deferring");
					deferred_.Add(a);
				}

				return false;
			}

			var o = UnityEngine.Object.Instantiate(
				SuperController.singleton.dynamicButtonPrefab);

			o.name = ButtonName;

			var b = o.GetComponent<UIDynamicButton>();
			b.button.onClick.AddListener(() => OnResetAll(a));
			b.buttonText.text = "Reset all";

			o.transform.SetParent(r.parent, false);

			var ce = r.GetComponent<RectTransform>();
			var rt = o.GetComponent<RectTransform>();

			rt.offsetMax = ce.offsetMax;
			rt.offsetMin = ce.offsetMin;
			rt.anchorMin = ce.anchorMin;
			rt.anchorMax = ce.anchorMax;
			rt.anchoredPosition = ce.anchoredPosition;
			rt.pivot = ce.pivot;

			rt.offsetMin = new Vector2(rt.offsetMin.x, rt.offsetMin.y - 100);
			rt.offsetMax = new Vector2(rt.offsetMax.x, rt.offsetMax.y - 150);

			return true;
		}

		private void OnResetAll(Atom a)
		{
			var skin = a.GetStorableByID("skin");
			if (skin == null)
			{
				Log.Error($"{a.uid} has no skin storable");
				return;
			}

			Reset(skin, "Specular Texture Offset");
			Reset(skin, "Specular Intensity");
			Reset(skin, "Gloss");
			Reset(skin, "Specular Fresnel");
			Reset(skin, "Gloss Texture Offset");
			Reset(skin, "Diffuse Texture Offset");
			Reset(skin, "Diffuse Bumpiness");
			Reset(skin, "Specular Bumpiness");
			Reset(skin, "Global Illumination Filter");
		}

		private void Reset(JSONStorable skin, string param)
		{
			var f = skin.GetFloatJSONParam(param);
			if (f == null)
			{
				Log.Error(
					$"skin storable {skin.containingAtom.uid} " +
					$"has no param '{param}'");

				return;
			}

			f.SetValToDefault();
		}
	}
}
