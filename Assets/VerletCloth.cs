using Ashkatchap.Shared;
using Ashkatchap.Shared.Collections;
using System.Collections.Generic;
using UnityEngine;

sealed public class VerletCloth : MonoBehaviour {
	[SerializeField] int sizeX = 5;
	[SerializeField] int sizeY = 5;
	[SerializeField] Vector3 gravity = new Vector3(0, -9.8f, 0);
	[SerializeField] List<VerletControlPoint> controlPoints;

	public int physicIterations = 3;
	public float spacing = 0.01f;
	public float tearDistance = 1000;

	public bool springsStructural = true;
	public bool springsShear = false;

	public int startRow = 0;
	
	VerletPoint[] points;
	UnorderedList<Constraint> constraints = new UnorderedList<Constraint>(32, 32);
	void Start() {
		int lengthX = sizeX + 1;
		int lengthY = sizeY + 1;
		points = new VerletPoint[lengthX * lengthY];

		for (int y = 0; y < lengthY; y++) {
			for (var x = 0; x < lengthX; x++) {
				points[x + y * lengthX] = new VerletPoint(transform.position + new Vector3(x * spacing, y * spacing));
			}
		}
		
		for (var i = startRow * lengthX; i < startRow * lengthX + lengthX; i++) {
			var p = points[i];
			
			var att = GameObject.CreatePrimitive(PrimitiveType.Cube);
			att.transform.localScale = new Vector3(spacing, spacing, spacing);
			att.transform.parent = transform;
			att.transform.position = p.pos;
			VerletControlPoint vcp = att.AddComponent<VerletControlPoint>();
			vcp.pointIndex = i;
			controlPoints.Add(vcp);
			p.pinned = true;
		}
		
		for (int y = 0; y < lengthY; y++) {
			for (var x = 0; x < lengthX; x++) {
				if (springsStructural) {
					if (x > 0) AddConstraint(new Constraint(points[x + y * lengthX], points[(x - 1) + y * lengthX]));
					if (y > 0) AddConstraint(new Constraint(points[x + y * lengthX], points[x + (y - 1) * lengthX]));
				}
				if (springsShear) {
					if (x > 0 && y > 0) {
						AddConstraint(new Constraint(points[(x) + (y) * lengthX], points[(x - 1) + (y - 1) * lengthX]));
						AddConstraint(new Constraint(points[(x) + (y - 1) * lengthX], points[(x - 1) + (y) * lengthX]));
					}
				}
			}
		}
	}

	void AddConstraint(Constraint c) {
		constraints.Add(c);
		c.p1.constraints.Add(c);
	}

	void FixedUpdate() {
		// Move attached points
		for (int i = 0; i < controlPoints.Count; i++) {
			if (null != controlPoints[i]) {
				points[controlPoints[i].pointIndex].pos = controlPoints[i].transform.position;
			}
		}
		/*
		// Resolve constraints
		for (int i = 0; i < physicIterations; i++) {
			for (int c = 0; c < constraints.Size; c++) {
				if (constraints.elements[c].resolve(tearDistance)) {
					constraints.RemoveAt(c);
					c--; // We do this because of the UnorderedList works (removing equals replacing the element with the latest one in the list)
				}
			}
		}
		*/
		
		for (int i = 0; i < physicIterations; i++)
			for (int p = 0; p < points.Length; p++)
				points[p].resolve_constraints(tearDistance);
		
		// Physic properties for the points
		for (int i = 0; i < points.Length; i++)
			points[i].Update(Time.deltaTime, gravity);
	}

	public bool printDistances = false;
	void OnDrawGizmos() {
		for (int i = 0; i < constraints.Size; i++) {
			var p1 = constraints[i].p1;
			var p2 = constraints[i].p2;
			Gizmos.DrawLine(p1.pos, p2.pos);
			if (printDistances) {
				UnityEditor.Handles.Label((p1.pos + p2.pos) / 2, System.Math.Round(constraints[i].length, 2).ToString());
			}
		}
	}
}

// Consider using a point with mass and physic properties
sealed class VerletPoint {
	public Vector3 pos;
	public Vector3 oldPos;
	public Vector3 velocity;
	public bool pinned = false;

	public List<Constraint> constraints;

	public VerletPoint(Vector3 pos) {
		this.pos = pos;
		constraints = new List<Constraint>();
	}

	public void Update(float delta, Vector3 gravity) {
		if (pinned) return;

		add_force(gravity);

		delta *= delta;
		Vector3 newPos = pos + ((pos - oldPos) * .99f) + ((velocity / 2) * delta);

		oldPos = pos;
		pos = newPos;
		velocity = Vector3.zero;
	}

	public void resolve_constraints(float tearDistance) {
		for (int i = 0; i < constraints.Count; i++)
			if (constraints[constraints.Count - 1 - i].resolve(tearDistance))
				constraints.RemoveAt(i--);
		// Collisions (TO DO)
	}
	public void add_force(Vector3 velocity) {
		this.velocity += velocity;
	}
}

sealed class Constraint {
	public VerletPoint p1;
	public VerletPoint p2;
	public float length;

	public Constraint(VerletPoint p1, VerletPoint p2) : this(p1, p2, FastMath.Distance(ref p1.pos, ref p2.pos)) { }
	public Constraint(VerletPoint p1, VerletPoint p2, float length) {
		this.p1 = p1;
		this.p2 = p2;
		this.length = length;
	}

	public bool resolve(float tearDistance) {
		Vector3 offset = p1.pos - p2.pos;
		float dist = FastMath.Magnitude(ref offset);
		float diff = (length - dist) / dist;

		if (dist > tearDistance) {
			return true;
		}

		Vector3 movement = offset * diff;

		float p1Mult = p1.pinned ? 0 : p2.pinned ? 1 : 0.5f;
		float p2Mult = p2.pinned ? 0 : p1.pinned ? 1 : 0.5f;

		p1.pos += movement * p1Mult;
		p2.pos -= movement * p2Mult;

		return false;
	}
}
