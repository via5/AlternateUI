using UnityEngine;
using UnityEngine.UI;

namespace AUI.LogUI
{
	class LogUI : IAlternateUI
	{
		private Text logText_ = null;
		private int oldLogFontSize_ = -1;
		private Font oldLogFont_ = null;

		public LogUI()
		{
			var vp = VUI.Utilities.FindChildRecursive(
				SuperController.singleton.errorLogPanel, "Viewport");

			var textObject = VUI.Utilities.FindChildRecursive(vp, "Text");

			logText_ = textObject.GetComponent<UnityEngine.UI.Text>();
		}

		public void Update(float s)
		{
		}

		public void OnPluginState(bool b)
		{
			if (b)
				Enable();
			else
				Disable();
		}

		private void Enable()
		{
			oldLogFontSize_ = logText_.fontSize;
			oldLogFont_ = logText_.font;

			var f = VUI.Style.Theme.MonospaceFont;

			logText_.resizeTextForBestFit = false;
			logText_.font = f;
			logText_.fontSize = f.fontSize;

			for (int i = 0; i < 10; ++i)
				SuperController.LogError($"test {i}");
		}

		private void Disable()
		{
			logText_.resizeTextForBestFit = true;
			logText_.font = oldLogFont_;
			logText_.fontSize = oldLogFontSize_;
		}
	}
}
