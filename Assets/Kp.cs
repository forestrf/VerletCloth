using UnityEngine;

namespace Kp {
	public static class Color {
		public static UnityEngine.Color LerpViaHSV(UnityEngine.Color color0, UnityEngine.Color color1, float t) {
			float h0, s0, v0, h1, s1, v1;
			UnityEngine.Color.RGBToHSV(color0, out h0, out s0, out v0);
			UnityEngine.Color.RGBToHSV(color1, out h1, out s1, out v1);
			float h = Mathf.Repeat(Mathf.LerpAngle(h0 * 360f, h1 * 360f, t), 360f) / 360f;
			float s = Mathf.LerpUnclamped(s0, s1, t);
			float v = Mathf.LerpUnclamped(v0, v1, t);
			return UnityEngine.Color.HSVToRGB(h, s, v);
		}
	}
}
