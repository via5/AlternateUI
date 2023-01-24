using UnityEngine;
using UnityEngine.UI;

namespace AUI.LogUI
{
	class LogUI : BasicAlternateUI
	{
		private Text logText_ = null;
		private int oldLogFontSize_ = -1;
		private Font oldLogFont_ = null;

		public LogUI()
			: base("log", "Monospace log", true)
		{
		}

		public override string Description
		{
			get
			{
				return
					"Changes the error log panel to use Consolas with a " +
					"smaller font size.";
			}
		}

		protected override void DoInit()
		{
			var vp = VUI.Utilities.FindChildRecursive(
				SuperController.singleton.errorLogPanel, "Viewport");

			var textObject = VUI.Utilities.FindChildRecursive(vp, "Text");

			logText_ = textObject.GetComponent<UnityEngine.UI.Text>();
		}

		protected override void DoEnable()
		{
			oldLogFontSize_ = logText_.fontSize;
			oldLogFont_ = logText_.font;

			var f = VUI.Style.Theme.MonospaceFont;

			logText_.resizeTextForBestFit = false;
			logText_.font = f;
			logText_.fontSize = f.fontSize;
		}

		protected override void DoDisable()
		{
			logText_.resizeTextForBestFit = true;
			logText_.font = oldLogFont_;
			logText_.fontSize = oldLogFontSize_;
		}
	}
}
