using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AUI.LightUI
{
	class LightUI : BasicAlternateUI
	{
		private const string ButtonName = "aui.lightui.resetall";
		private SkyshopLightControllerUI ui_ = null;

		public LightUI()
			: base("light", "Light UI", true)
		{
		}

		public override string Description
		{
			get
			{
				return
					"Adds a reset button to the Scene Lighting UI.";
			}
		}

		protected override void DoInit()
		{
			ui_ = SkyshopLightController.singleton.UITransform
				.GetComponentInChildren<SkyshopLightControllerUI>();

			if (ui_ == null)
			{
				Log.Error("SkyshopLightControllerUI not found");
				return;
			}

			Remove();
		}

		protected override void DoEnable()
		{
			Remove();
			Add();
		}

		protected override void DoDisable()
		{
			Remove();
		}

		private void Remove()
		{
			if (ui_ == null)
				return;

			var parent = ui_.camExposureSlider.transform.parent;

			foreach (Transform t in parent)
			{
				Log.Info(t.name);
				if (t.name.StartsWith(ButtonName))
					UnityEngine.Object.Destroy(t.gameObject);
			}
		}

		private void Add()
		{
			if (ui_ == null)
				return;

			var parent = ui_.camExposureSlider.transform.parent;

			var o = UnityEngine.Object.Instantiate(
				SuperController.singleton.dynamicButtonPrefab);

			o.name = ButtonName;

			var b = o.GetComponent<UIDynamicButton>();
			b.button.onClick.AddListener(OnResetAll);
			b.buttonText.text = "Reset all";

			o.transform.SetParent(parent, false);

			var ce = ui_.camExposureSlider.GetComponent<RectTransform>();
			var rt = o.GetComponent<RectTransform>();

			rt.offsetMax = ce.offsetMax;
			rt.offsetMin = ce.offsetMin;
			rt.anchorMin = ce.anchorMin;
			rt.anchorMax = ce.anchorMax;
			rt.anchoredPosition = ce.anchoredPosition;
			rt.pivot = ce.pivot;

			rt.offsetMin = new Vector2(rt.offsetMin.x - 20, rt.offsetMin.y - 90);
			rt.offsetMax = new Vector2(rt.offsetMax.x - 100, rt.offsetMax.y - 80);
		}

		private void OnResetAll()
		{
			var c = SkyshopLightController.singleton;
			if (c == null)
				return;

			c.GetFloatJSONParam("masterIntensity")?.SetValToDefault();
			c.GetFloatJSONParam("diffuseIntensity")?.SetValToDefault();
			c.GetFloatJSONParam("specularIntensity")?.SetValToDefault();
			c.GetFloatJSONParam("unityAmbientIntensity")?.SetValToDefault();
			c.GetColorJSONParam("unityAmbientColor")?.SetValToDefault();
			c.GetFloatJSONParam("camExposure")?.SetValToDefault();
			c.GetFloatJSONParam("showSkybox")?.SetValToDefault();
			c.GetFloatJSONParam("skyboxIntensity")?.SetValToDefault();
			c.GetFloatJSONParam("skyboxYAngle")?.SetValToDefault();

			if (c.skies != null && c.skies.Length > 0)
				c.skyName = c.skies[0].name;
		}
	}
}
