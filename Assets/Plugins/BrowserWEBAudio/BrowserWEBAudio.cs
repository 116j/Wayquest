using UnityEngine;
using System.Runtime.InteropServices;

public class BrowserWEBAudio : MonoBehaviour
{
	[DllImport("__Internal")]
	private static extern void InitBrowserWEBAudio();

	private void Start()
	{
#if UNITY_WEBGL && !UNITY_EDITOR
        InitBrowserWEBAudio();
#endif
	}
}