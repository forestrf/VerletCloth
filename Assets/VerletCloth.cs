using System.Collections.Generic;
using UnityEngine;

public class VerletCloth : MonoBehaviour {
	[SerializeField] int sizeX = 5;
	[SerializeField] int sizeY = 5;
	[SerializeField] float gravity = 1;
	[SerializeField] float resistance = 2;
	[SerializeField] Color slackColor;
	[SerializeField] Color stretchColor;
	[SerializeField] float colorSensitivity;
	[SerializeField] public VerletPoint[] points;
	[SerializeField] public List<VerletStick> sticks = new List<VerletStick>();
	[SerializeField] List<VerletControlPoint> controlPoints;

	void Awake() {
		GenerateCloth();
	}

	void FixedUpdate() {
		if (points == null) {
			return;
		}
		UpdateSticks();
		UpdateControlPoints();
		UpdatePoints();
		RenderSticks();
	}

	private void UpdateControlPoints() {
		foreach (var cp in controlPoints) {
			cp.point.pos = cp.transform.position;
		}
	}

	void RenderSticks() {
		foreach (var s in sticks) {
			Vector3 a = s.p0.pos;
			Vector3 b = s.p1.pos;
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
		controlPoints[0].point = points[0];
		controlPoints[1].point = points[0 + sizeX * (sizeY - 1)];
		controlPoints[2].point = points[sizeX - 1 + sizeX * (0)];
		controlPoints[3].point = points[sizeX - 1 + sizeX * (sizeY - 1)];
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
		for (int i = 0, y = 0; y < sizeY; y++) {
			for (int x = 0; x < sizeX; x++, i++) {
				VerletPoint p0 = points[x + sizeX * y];
				// for each point, we connect to the one to the right, and the one above
				if (x < sizeX - 1) {
					VerletStick rightStick = new VerletStick();
					rightStick.p0 = p0;
					rightStick.p1 = points[x + 1 + sizeX * y];
					rightStick.length = Vector3.Distance(p0.pos, rightStick.p1.pos);
					sticks.Add(rightStick);
				}
				if (y < sizeY - 1) {
					VerletStick upStick = new VerletStick();
					upStick.p0 = p0;
					upStick.p1 = points[x + sizeX * (y + 1)];
					upStick.length = Vector3.Distance(p0.pos, upStick.p1.pos);
					sticks.Add(upStick);
				}
			}
		}
	}



	void UpdatePoints() {
		// we dont care about the grid x/y here so we can just foreach
		foreach (var p in points) {

			// other things might have changed the position of the point, 
			// if this point is  immovable then we can undo this with the previous position
			// this is why UpdatePoints should happen last
			if (p.immovable) {
				p.pos = p.prePos;
				continue;
			}

			p.vel = p.pos - p.prePos;

			p.vel += gravity * Vector3.down * Time.fixedDeltaTime;
			p.vel += (-p.vel) * resistance * Time.fixedDeltaTime;

			p.prePos = p.pos;
			p.pos += p.vel;
		}

	}
	void UpdateSticks() {
		foreach (var s in sticks) {
			Vector3 d = s.p1.pos - s.p0.pos;
			float distance = Vector3.Distance(s.p0.pos, s.p1.pos);
			float differenceToTargetLength = s.length - distance;
			float asdfadsf = differenceToTargetLength / distance / 2;
			Vector3 offset = d * asdfadsf;

			s.p0.pos -= offset;
			s.p1.pos += offset;
		}
	}
	void OnDrawGizmos() {
		if (points == null) return;

		for (int i = 0, y = 0; y < sizeY; y++, i++) {
			for (int x = 0; x < sizeX; x++) {
				Gizmos.DrawCube(points[x + sizeX * y].pos, Vector3.one * 0.1f);
			}
		}
	}


}

[System.Serializable]
public class VerletPoint {
	public Vector3 pos;
	public Vector3 prePos;
	public Vector3 vel;
	public bool immovable;

	public VerletPoint(Vector3 pos) {
		this.pos = pos;
		prePos = pos;
		vel = Vector3.zero;
	}
}
[System.Serializable]
public class VerletStick {
	public VerletPoint p0;
	public VerletPoint p1;
	public float length;
}
