using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AUI.AddAtomUI
{
	class AddAtomUIMouseCallbacks : MonoBehaviour, IPointerClickHandler
	{
		private AddAtomUI ui_ = null;

		public void Set(AddAtomUI ui)
		{
			ui_ = ui;
		}

		public void OnPointerClick(PointerEventData d)
		{
			try
			{
				if (d.button == PointerEventData.InputButton.Left && d.clickCount == 2)
					ui_.OnDoubleClick(transform);
			}
			catch (Exception e)
			{
				ui_.Log.Error(e.ToString());
			}
		}
	}


	class AddAtomUI : TweakFeature
	{
		private bool wasVisible_ = false;
		private GameObject addAtomPanel_ = null;
		private UIPopup categoryPopup_ = null;
		private Delegate categoryChanged_ = null;
		private string lastCategory_ = null;

		public AddAtomUI()
			: base("addAtomDoubleClick", "Add atom double-click", true)
		{
		}

		public override string Description
		{
			get
			{
				return
					"Double click an entry in the Add Atom list to add it.";
			}
		}

		public void OnDoubleClick(Transform t)
		{
			Log.Verbose($"double clicked {t}");
			SuperController.singleton.AddAtomByPopupValue();
		}

		protected override void DoEnable()
		{
			if (addAtomPanel_ == null)
			{
				addAtomPanel_ = VUI.Utilities.FindChildRecursive(
					SuperController.singleton, "TabAddAtom");
			}

			if (categoryPopup_ == null)
				categoryPopup_ = SuperController.singleton.atomCategoryPopup;

			if (categoryPopup_ != null)
			{
				if (categoryChanged_ == null)
					categoryChanged_ = new UIPopup.OnValueChange(OnCategoryChanged);

				categoryPopup_.onValueChangeHandlers = (UIPopup.OnValueChange)Delegate.Combine(
					categoryPopup_.onValueChangeHandlers, categoryChanged_);
			}
		}

		protected override void DoDisable()
		{
			if (categoryPopup_ != null && categoryChanged_ != null)
			{
				categoryPopup_.onValueChangeHandlers = (UIPopup.OnValueChange)Delegate.Remove(
					categoryPopup_.onValueChangeHandlers, categoryChanged_);
			}

			RemoveCallbacks();
			base.DoDisable();
		}

		protected override void DoUpdate(float s)
		{
			if (addAtomPanel_ == null)
				return;

			if (addAtomPanel_.activeInHierarchy)
			{
				if (!wasVisible_)
				{
					wasVisible_ = true;
					lastCategory_ = SuperController.singleton.atomCategoryPopup.currentValue;
					SetCallbacks();
				}
				else
				{
					if (SuperController.singleton.atomCategoryPopup.currentValue != lastCategory_)
					{
						lastCategory_ = SuperController.singleton.atomCategoryPopup.currentValue;
						SetCallbacks();
					}
				}
			}
			else
			{
				wasVisible_ = false;
			}
		}

		private void OnCategoryChanged(string s)
		{
			Log.Verbose($"category changed {s}");
		}

		private void SetCallbacks()
		{
			Log.Verbose("setting callbacks");
			AlternateUI.Instance.StartCoroutine(CoSetCallbacks());
		}

		private IEnumerator CoSetCallbacks()
		{
			yield return new WaitForEndOfFrame();
			yield return new WaitForEndOfFrame();

			var buttons = GetButtons();
			RemoveCallbacks(buttons);
			AddCallbacks(buttons);
		}

		private void RemoveCallbacks(GameObject[] buttons = null)
		{
			if (buttons == null)
				buttons = GetButtons();

			foreach (var b in buttons)
			{
				foreach (var c in b.GetComponents<Component>())
				{
					if (c.ToString().Contains("AddAtomUIMouseCallbacks"))
					{
						Log.Verbose($"removing callback from {c}");
						UnityEngine.Object.Destroy(c);
					}
				}
			}
		}

		private void AddCallbacks(GameObject[] buttons = null)
		{
			if (buttons == null)
				buttons = GetButtons();

			foreach (var b in buttons)
			{
				Log.Verbose($"adding callback to {b}");

				var c = b.AddComponent<AddAtomUIMouseCallbacks>();

				c.Set(this);
			}
		}

		private GameObject[] GetButtons()
		{
			var list = new List<GameObject>();

			var popup = SuperController.singleton.atomPrefabPopup;
			var parent = popup?.buttonParent ?? popup?.popupPanel;

			if (parent != null)
			{
				foreach (var b in parent.GetComponentsInChildren<UnityEngine.UI.Button>())
				{
					list.Add(b.gameObject);
				}
			}

			return list.ToArray();
		}
	}
}
