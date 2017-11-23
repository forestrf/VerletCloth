using Ashkatchap.Shared;
using System.Collections.Generic;
using UnityEngine;

public class VerletCloth : MonoBehaviour {
	[SerializeField] int sizeX = 5;
	[SerializeField] int sizeY = 5;
	[SerializeField] Vector3 gravity = new Vector3(0, -1, 0);
	[SerializeField] float resistance = 2;
	[SerializeField] Color slackColor;
	[SerializeField] Color stretchColor;
	[SerializeField] float colorSensitivity;
	[SerializeField] public VerletPoint[] points;
	[SerializeField] public VerletStick[] sticks;
	[SerializeField] List<VerletControlPoint> controlPoints;

	void Awake() {
		GenerateCloth();
	}

	void FixedUpdate() {
		if (points == null || points.Length == 0) {
			return;
		}
		UpdateSticks();
		UpdateControlPoints();
		UpdatePoints();
		RenderSticks();
	}

	private void UpdateControlPoints() {
		foreach (var cp in controlPoints) {
			points[cp.pointIndex].pos = cp.transform.position;
		}
	}

	void RenderSticks() {
		foreach (var s in sticks) {
			Vector3 a = points[s.p0].pos;
			Vector3 b = points[s.p1].pos;
			// a += Kp.Rng.InCircle( 0.1f );
			// b += Kp.Rng.InCircle( 0.1f );

			float difference = s.length - Vector3.Distance(a, b);
			difference *= colorSensitivity;
			difference += 0.5f; // difference should be give or take 0.5 now, better for lerping
			difference = Mathf.Clamp01(difference);
			Color color = Kp.Color.LerpViaHSV(stretchColor, slackColor, difference);
			Debug.DrawLine(a, b, color);
		}

	}

	void GenerateCloth() {
		GeneratePoints();
		GenerateSticks();
		controlPoints[0].pointIndex = 0;
		controlPoints[1].pointIndex = 0 + sizeX * (sizeY - 1);
		controlPoints[2].pointIndex = sizeX - 1 + sizeX * (0);
		controlPoints[3].pointIndex = sizeX - 1 + sizeX * (sizeY - 1);
	}



	void GeneratePoints() {
		points = new VerletPoint[sizeX * sizeY];
		for (int i = 0, y = 0; y < sizeY; y++) {
			for (int x = 0; x < sizeX; x++, i++) {
				// Vector3 np = new Vector3( (float)x / sizeX, (float)y / sizeY, 0 );
				// Vector3 pointAlongTop = Vector3.Lerp( topLeftControlPoint.position, topRightControlPoint.position, np.x );
				// Vector3 pointAlongBottom = Vector3.Lerp( bottomLeftControlPoint.position, bottomRightControlPoint.position, np.x );
				// Vector3 pos = Vector3.Lerp( pointAlongBottom, pointAlongTop, np.y );
				// var p = new VerletPoint( new Vector3( pos.x, pos.y, pos.z ) );
				var p = new VerletPoint(new Vector3(x, y, 0f));
				points[x + sizeX * y] = p;
			}
		}
	}
	void GenerateSticks() {
		sticks = new VerletStick[sizeY * sizeX * 2 - (sizeX + sizeX - 1)];
		int stickCounter = 0;
		for (int i = 0, y = 0; y < sizeY; y++) {
			for (int x = 0; x < sizeX; x++, i++) {
				// for each point, we connect to the one to the right, and the one above
				if (x < sizeX - 1) {
					int p0 = x + sizeX * y;
					int p1 = x + 1 + sizeX * y;
					float length = FastMath.Distance(ref points[p0].pos, ref points[p1].pos);
					sticks[stickCounter++] = new VerletStick(p0, p1, length);
				}
				if (y < sizeY - 1) {
					int p0 = x + sizeX * y;
					int p1 = x + sizeX * (y + 1);
					float length = FastMath.Distance(ref points[p0].pos, ref points[p1].pos);
					sticks[stickCounter++] = new VerletStick(p0, p1, length);
				}
			}
		}
	}



	void UpdatePoints() {
		// we dont care about the grid x/y here so we can just foreach
		for (int i = 0; i < points.Length; i++) {
			// other things might have changed the position of the point, 
			// if this point is  immovable then we can undo this with the previous position
			// this is why UpdatePoints should happen last
			if (points[i].immovable) {
				points[i].pos = points[i].prePos;
				continue;
			}

			points[i].vel = (points[i].pos - points[i].prePos) 
				+ (gravity - points[i].vel * resistance) * Time.fixedDeltaTime;

			points[i].prePos = points[i].pos;
			points[i].pos += points[i].vel;
		}

	}
	void UpdateSticks() {
		foreach (var s in sticks) {
			Vector3 d = points[s.p1].pos - points[s.p0].pos;
			float distance = FastMath.Distance(ref points[s.p0].pos, ref points[s.p1].pos);
			float differenceToTargetLength = s.length - distance;
			Vector3 offset = d * (differenceToTargetLength / distance / 2);

			points[s.p0].pos -= offset;
			points[s.p1].pos += offset;
		}
	}
	void OnDrawGizmos() {
		if (points == null || points.Length == 0) return;

		for (int i = 0, y = 0; y < sizeY; y++, i++) {
			for (int x = 0; x < sizeX; x++) {
				Gizmos.DrawCube(points[x + sizeX * y].pos, Vector3.one * 0.1f);
			}
		}
	}


}

[System.Serializable]
public struct VerletPoint {
	public Vector3 pos;
	public Vector3 prePos;
	public Vector3 vel;
	public bool immovable;

	public VerletPoint(Vector3 pos) {
		this.pos = pos;
		prePos = pos;
		vel = Vector3.zero;
		immovable = false;
	}
}
[System.Serializable]
public struct VerletStick {
	public readonly int p0, p1;
	public readonly float length;
	public VerletStick(int p0, int p1, float length) {
		this.p0 = p0;
		this.p1 = p1;
		this.length = length;
	}
}
