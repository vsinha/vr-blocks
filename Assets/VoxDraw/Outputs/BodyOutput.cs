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

            var collider = instance.GetComponent<BoxCollider>();
            collider.size = context.LocalBounds.size;
            collider.center = context.LocalCenter;
            collider.isTrigger = false;

			var positions = context.Positions.ToArray (); 

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


				var endCollider = endSnapPoint.GetComponent<SphereCollider> ();



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

				var startCollider = startSnapPoint.GetComponent<SphereCollider> ();


			}
        }

    }
}
