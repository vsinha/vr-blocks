using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxDraw;
using VoxDraw.Outputs;
using VRTK;
using System.Linq;

public class LineDrawer : MonoBehaviour
{
    private VRTK_ControllerActions vrtkControllerActions;
    private VRTK_ControllerEvents vrtkControllerEvents;

    private GameObject linePrefab;
    private GameObject currentLineObject;
    private PathDrawingContext currentContext = null;

    private VRLineRenderer currentLineRenderer;
    private GameObject controllerInstance;

    private IPathOutputService output = null;// new BodyOutput();
    
    

    // Use this for initialization
    void Start()
    {
        this.vrtkControllerActions = this.GetComponent<VRTK_ControllerActions>();
        this.vrtkControllerEvents = this.GetComponent<VRTK_ControllerEvents>();
        this.linePrefab = Resources.Load<GameObject>("VRLinePreview");

        vrtkControllerEvents.TriggerPressed += OnTriggerClicked;
        vrtkControllerEvents.TriggerReleased += OnTriggerReleased;

        this.output = new BodyOutput();
    }




    // Update is called once per frame
    void Update()
    {
        if (!this.vrtkControllerEvents.triggerPressed) return;

        currentContext.AddPoint(this.controllerInstance.transform);
        
        this.currentLineRenderer.SetPositions(currentContext.Positions.ToArray(), true);
    }

    private void OnTriggerClicked(object sender, ControllerInteractionEventArgs e)
    {
        this.controllerInstance = VRTK_SDK_Bridge.GetControllerByIndex(e.controllerIndex, true);

        this.currentLineObject = Instantiate(linePrefab, controllerInstance.transform.position, Quaternion.identity);
        this.currentLineRenderer = this.currentLineObject.GetComponent<VRLineRenderer>();

        this.currentContext = new PathDrawingContext(this.currentLineObject.transform);
    }

    void OnDrawGizmos()
    {
        if(this.currentContext == null)
        {
            return; 
        }
        

        foreach(var p in this.currentContext.Positions)
        {
            var world = this.currentContext.Root.TransformPoint(p);
            Gizmos.DrawWireSphere(world, 0.001f);
        }

        //Debug.Log(this.currentContext.WorldBounds);
        Gizmos.DrawWireCube(this.currentContext.WorldBounds.center, this.currentContext.WorldBounds.size);
    }


    private void OnTriggerReleased(object sender, ControllerInteractionEventArgs e)
    {

        output.Process(this.currentContext);

        Destroy(this.currentLineObject);
        Destroy(this.currentLineRenderer);

        this.currentLineObject = null;
        this.currentLineRenderer = null;
        this.currentContext = null;
    }
}
