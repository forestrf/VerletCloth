using Ashkatchap.Shared;
using System;
using System.Collections.Generic;
using UnityEngine;

public class VerletCloth : MonoBehaviour {
	[SerializeField] int sizeX = 5;
	[SerializeField] int sizeY = 5;
	[SerializeField] Vector3 gravity = new Vector3(0, -9.8f, 0);
	[SerializeField] Color slackColor;
	[SerializeField] Color stretchColor;
	[SerializeField] float colorSensitivity;
	[SerializeField] public VerletPoint[] pointsA;
	[SerializeField] public VerletPoint[] pointsB;
	private VerletPoint[] pointsNow = null; // points to pointsA or pointsB
	private VerletPoint[] pointsBefore = null; // points to pointsA or pointsB
	[SerializeField] public VerletStick[] sticks;
	[SerializeField] List<VerletControlPoint> controlPoints;

	void Awake() {
		GenerateCloth();
	}

	void FixedUpdate() {
		if (pointsNow == null || pointsNow.Length == 0) {
			return;
		}

		// Alternate double buffer
		if (pointsNow == pointsA) {
			pointsNow = pointsB;
			pointsBefore = pointsA;
		} else {
			pointsNow = pointsA;
			pointsBefore = pointsB;
		}

		UpdateControlPoints();
		UpdatePoints();
		UpdateSticks();
	}

	private void UpdateControlPoints() {
		foreach (var cp in controlPoints) {
			pointsBefore[cp.pointIndex].pos = 
				pointsNow[cp.pointIndex].pos = 
				cp.transform.position;
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
		pointsA = new VerletPoint[sizeX * sizeY];
		for (int i = 0, y = 0; y < sizeY; y++) {
			for (int x = 0; x < sizeX; x++, i++) {
				var p = new VerletPoint(new Vector3(x, y, 0f));
				pointsA[x + sizeX * y] = p;
			}
		}
		pointsB = new VerletPoint[pointsA.Length];
		pointsA.CopyTo(pointsB, 0);
		pointsNow = pointsA;
		pointsBefore = pointsB;
	}
	void GenerateSticks() {
		sticks = new VerletStick[2 * sizeX * sizeY - sizeX - sizeY];
		int stickCounter = 0;
		for (int y = 0; y < sizeY; y++) {
			for (int x = 0; x < sizeX; x++) {
				// for each point, we connect to the one to the right, and the one above
				if (x < sizeX - 1) {
					int p0 = x + sizeX * y;
					int p1 = x + 1 + sizeX * y;
					float length = FastMath.Distance(ref pointsNow[p0].pos, ref pointsNow[p1].pos);
					sticks[stickCounter++] = new VerletStick(p0, p1, length);
				}
				if (y < sizeY - 1) {
					int p0 = x + sizeX * y;
					int p1 = x + sizeX * (y + 1);
					float length = FastMath.Distance(ref pointsNow[p0].pos, ref pointsNow[p1].pos);
					sticks[stickCounter++] = new VerletStick(p0, p1, length);
				}
			}
		}
	}
	
	[Range(0, 1)]
	public float friction = 1;
	
	void UpdatePoints() {
		// we dont care about the grid x/y here so we can just foreach
		for (int i = 0; i < pointsNow.Length; i++) {
			// pointsNow has the value t - 2, set its value to t - 1 (that is, pointsBefore)
			pointsNow[i].pos = pointsBefore[i].pos;

			// other things might have changed the position of the point, 
			// if this point is  immovable then we can undo this with the previous position
			// this is why UpdatePoints should happen last
			if (pointsNow[i].immovable) {
				continue;
			}

			Vector3 movement = (pointsNow[i].pos - pointsBefore[i].pos) * (1 - friction)
				+ gravity * Time.deltaTime;
			
			pointsNow[i].pos += movement;
		}

	}
	public bool debug;
	void UpdateSticks() {
		foreach (var s in sticks) {
			Vector3 absoulteOffset = pointsBefore[s.p1].pos - pointsBefore[s.p0].pos;
			float distance = FastMath.Magnitude(ref absoulteOffset);
			Vector3 dir = distance > 0 ? absoulteOffset / distance : Vector3.zero;
			float differenceToTargetLength = s.length - distance;
			Vector3 halfOffset = dir * differenceToTargetLength / 2;

			if (debug) Debug.DrawRay(pointsNow[s.p0].pos, -halfOffset);
			pointsNow[s.p0].pos -= halfOffset;
			if (debug) Debug.DrawRay(pointsNow[s.p1].pos, halfOffset);
			pointsNow[s.p1].pos += halfOffset;
		}
	}
	void OnDrawGizmos() {
		if (pointsNow == null || pointsNow.Length == 0) return;

		for (int i = 0, y = 0; y < sizeY; y++, i++) {
			for (int x = 0; x < sizeX; x++) {
				Gizmos.DrawCube(pointsNow[x + sizeX * y].pos, Vector3.one * 0.1f);
			}
		}

		foreach (var s in sticks) {
			Vector3 a = pointsNow[s.p0].pos;
			Vector3 b = pointsNow[s.p1].pos;

			float difference = Vector3.Distance(a, b) - s.length;
			difference = Math.Abs(difference);
			difference *= colorSensitivity;
			difference = Mathf.Clamp01(difference);
			Color color = Kp.Color.LerpViaHSV(slackColor, stretchColor, difference);
			Gizmos.color = color;
			Gizmos.DrawLine(a, b);
		}
	}
}

[System.Serializable]
public struct VerletPoint {
	public Vector3 pos;
	public bool immovable;

	public VerletPoint(Vector3 pos) {
		this.pos = pos;
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
