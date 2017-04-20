using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace VoxDraw.Outputs
{
    class BodyOutput : IPathOutputService
    {

        public void Process(PathDrawingContext context)
        {
			var parentPrefab = Resources.Load<GameObject> ("Prefabs/SnapParent2");
			var linePrefab = Resources.Load<GameObject>("PhysicsLine");

			var parent = GameObject.Instantiate<GameObject> (parentPrefab, context.Root.position, context.Root.rotation); 
			var instance = GameObject.Instantiate<GameObject>(linePrefab, parent.transform);

            var lineRenderer = instance.GetComponent<VRLineRenderer>();
            lineRenderer.SetPositions(context.Positions.ToArray(), true);

            // TODO: This is only for demos
			// Create interpolated sphere coliders along the path of the line
			var positions = context.Positions.ToArray (); 
			Vector3 bbStart = positions [0];
			Vector3 runningSum = bbStart;
			int runningCount = 1;
			int generated = 0;

			const float threshold = 0.05f;

			for (var i = 1; i < positions.Length; i++) { 
				Vector3 bbCurrent = positions [i];
				float distance = Vector3.Distance (bbStart, bbCurrent);

				if (distance < threshold) {
					runningCount++;
					runningSum += bbCurrent;
					continue;
				}

				var centroid = runningSum / runningCount; 
				var angle = Vector3.Angle (bbStart, bbCurrent);

				GameObject childBox = new GameObject ("Line Collision Point");
				childBox.AddComponent<SphereCollider> ();
				childBox.transform.SetParent (instance.transform);
				childBox.transform.localScale = Vector3.one * 0.025f;
				childBox.transform.localPosition = centroid; 
				generated++;

				runningSum = bbCurrent;
				runningCount = 1; 
				bbStart = bbCurrent;
			}

			if (generated == 0) { 
				// Add just one 
				GameObject childBox = new GameObject ();
				childBox.AddComponent<SphereCollider> ();
				childBox.transform.SetParent (instance.transform);
				childBox.transform.localScale = Vector3.one * 0.025f;
			}

			// Create start and end snap points
			if (context.NumberPoints >= 4) { 
				// Take the end
				var end = positions[positions.Length - 1];
				var prior = positions[positions.Length - 2];

				var direction = prior - end;

				var endSnapPoint = GameObject.Instantiate<GameObject> (Resources.Load<GameObject> ("Prefabs/SnapPoint2"), instance.transform); 
				endSnapPoint.name = "End of line";
				endSnapPoint.transform.localPosition = end;
				endSnapPoint.transform.localScale = Vector3.one * 0.1f;
				endSnapPoint.transform.rotation = Quaternion.LookRotation (direction) * Quaternion.Euler(0, 90, 0); 
			}

			if (context.NumberPoints >= 2) { 
				// Take the start 

				var start = positions[0];
				var next = positions[1];

				var direction = next - start;

				var startSnapPoint = GameObject.Instantiate<GameObject> (Resources.Load<GameObject> ("Prefabs/SnapPoint2"), instance.transform); 
				startSnapPoint.name = "Start of line";
				startSnapPoint.transform.localPosition = start;
				startSnapPoint.transform.localScale = Vector3.one * 0.1f;
				startSnapPoint.transform.rotation = Quaternion.LookRotation (direction) * Quaternion.Euler(0, 90, 0); 
			}
        }

    }
}
