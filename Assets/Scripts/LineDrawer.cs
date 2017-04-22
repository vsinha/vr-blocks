using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxDraw;
using VoxDraw.Outputs;
using VRTK;
using System.Linq;
using System;

public class LineDrawer : MonoBehaviour
{
    private VRTK_ControllerActions vrtkControllerActions;
    private VRTK_ControllerEvents vrtkControllerEvents;

    private GameObject linePrefab;
    private GameObject currentLineObject;
    private PathDrawingContext currentContext = null;

    private VRLineRenderer currentLineRenderer;
    private GameObject controllerInstance;

    private Color lineColor = Color.red;    

    private IPathOutputService output = null;// new BodyOutput();
    private bool inRecoMode = false;



    // Use this for initialization
    void Start()
    {
        this.vrtkControllerActions = this.GetComponent<VRTK_ControllerActions>();
        this.vrtkControllerEvents = this.GetComponent<VRTK_ControllerEvents>();
        this.linePrefab = Resources.Load<GameObject>("VRLinePreview");

        vrtkControllerEvents.TriggerPressed += OnTriggerClicked;
        vrtkControllerEvents.TriggerReleased += OnTriggerReleased;
        vrtkControllerEvents.TouchpadReleased += OnTouchpadReleased;

        this.SetLineMode();
        
    }

    private void OnTouchpadReleased(object sender, ControllerInteractionEventArgs e)
    {
        this.ToggleRecoMode();
    }

    private void SetLineMode()
    {
        this.output = new BodyOutput();
        lineColor = Color.red;
    }


    private void SetRecoMode()
    {
        this.output = new PRecoOutput();
        lineColor = Color.blue;
    }

    private void ToggleRecoMode()
    {
        this.inRecoMode = !this.inRecoMode;

        if (this.inRecoMode)
        {
            SetRecoMode();
        }
        else
        {
            SetLineMode();
        }
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
        var rend = this.currentLineRenderer.GetComponent<Renderer>();
        rend.material.SetColor("_Color", this.lineColor);
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
