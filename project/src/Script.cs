using System;
using System.Collections.Generic;
using System.Text;

namespace AUI
{
	public interface IScript
	{
	}

#if !MOCK
	public class AlternateUIScript : MVRScript, IScript
	{
		public AlternateUIScript()
		{
			new AlternateUI(new VamSys(this));
		}

		public void Update()
		{
			AlternateUI.Instance.Update();
		}

		public void OnEnable()
		{
			AlternateUI.Instance.OnEnable();
		}

		private void OnDisable()
		{
			AlternateUI.Instance.OnDisable();
		}
	}
#else
	public class MockAlternateUIScript : IScript
	{
		public MockAlternateUIScript(string root, string packages)
		{
			new AlternateUI(new FSSys(root, packages));
		}
	}
#endif
}
