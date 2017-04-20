﻿using UnityEngine;

/// <summary>
/// A VR-Focused drop-in replacement for the Line Renderer
/// This renderer draws fixed-width lines with simulated volume and glow.
/// This has many of the advantages of the traditional Line Renderer, old-school system-level line rendering functions, 
/// and volumetric (a linked series of capsules or cubes) rendering
/// </summary>
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
[ExecuteInEditMode]
public class VRLineRenderer : MonoBehaviour
{
    // Stored Line Data
    [SerializeField]
    Vector3[] m_Positions;

    [SerializeField]
    Color m_ColorStart = Color.white;
    [SerializeField]
    Color m_ColorEnd = Color.white;

    [SerializeField]
    float m_WidthStart = 1.0f;
    [SerializeField]
    float m_WidthEnd = 1.0f;

    [SerializeField]
    bool m_WorldSpaceData;

    // Cached Data
    public VRMeshChain MeshData;
    bool m_MeshNeedsRefreshing;

    // LineRenderer Interface
    /// <summary>
    /// If enabled, the lines are defined in world space.
    /// This means the object's position is ignored and the lines are rendered around the world origin.
    /// </summary>
    public bool useWorldSpace
    {
        get
        {
            return m_WorldSpaceData;
        }
        set
        {
            m_WorldSpaceData = value;
        }
    }

    public float widthStart { get { return m_WidthStart; } }
    public float widthEnd { get { return m_WidthEnd; } }

    /// <summary>
    /// Ensures the lines have all their data precached upon loading
    /// </summary>
    void Awake()
    {
        Initialize();
    }

    /// <summary>
    /// Does the actual internal mesh updating as late as possible so nothing ends up a frame behind
    /// </summary>
    void LateUpdate()
    {
        if (m_MeshNeedsRefreshing == true)
        {
            MeshData.RefreshMesh();
            m_MeshNeedsRefreshing = false;
        }
    }

    /// <summary>
    /// Allows the component to be referenced as a renderer, forwarding the MeshRenderer ahead
    /// </summary>
    public static implicit operator Renderer(VRLineRenderer lr)
    {
        return lr.GetComponent<MeshRenderer>();
    }

    //////////////////
    /// Editor Usage
    //////////////////
    public void EditorCheckForUpdate()
    {
        // If we did not initialize, refresh all the properties instead
        Initialize(true);
    }

    /// <summary>
    /// Set the line color at the start and at the end
    /// </summary>
    public void SetColors(Color start, Color end)
    {
        // Worth detecting a no op consideirng how much this function can potentially do
        if (start == m_ColorStart && end == m_ColorEnd)
        {
            return;
        }

        // Update internal data
        m_ColorStart = start;
        m_ColorEnd = end;

        // See if the data needs initializing
        if (Initialize())
        {
            return;
        }

        // If it doesn't, go through each point and set the data
        var pointCounter = 0;
        var elementCounter = 0;

        MeshData.SetElementColor(elementCounter, ref m_ColorStart);
        elementCounter++;
        pointCounter++;

        var stepSize = 1.0f / Mathf.Max((m_Positions.Length - 1.0f), 1.0f);
        var stepPercent = stepSize;
        var lastColor = m_ColorStart;

        while (pointCounter < m_Positions.Length)
        {
            var currentColor = Color.Lerp(m_ColorStart, m_ColorEnd, stepPercent);
            MeshData.SetElementColor(elementCounter, ref lastColor, ref currentColor);
            elementCounter++;

            MeshData.SetElementColor(elementCounter, ref currentColor);

            lastColor = currentColor;
            elementCounter++;
            pointCounter++;
            stepPercent += stepSize;
        }

        // Dirty the color meshChain flags so the mesh gets new data
        MeshData.SetMeshDataDirty(VRMeshChain.MeshRefreshFlag.Colors);
        m_MeshNeedsRefreshing = true;
    }

    /// <summary>
    /// Sets the position of the vertex in the line.
    /// </summary>
    public void SetPosition(int index, Vector3 position)
    {
        // Update internal data
        m_Positions[index] = position;

        // See if the data needs initializing
        if (Initialize())
        {
            return;
        }

        // Otherwise, do fast setting
        if (index > 0)
        {
            MeshData.SetElementPipe((index * 2) - 1, ref m_Positions[index - 1], ref m_Positions[index]);
        }
        MeshData.SetElementPosition(index * 2, ref m_Positions[index]);
        if (index < (m_Positions.Length - 1))
        {
            MeshData.SetElementPipe((index * 2) + 1, ref m_Positions[index], ref m_Positions[index + 1]);
        }
        MeshData.SetMeshDataDirty(VRMeshChain.MeshRefreshFlag.Positions);
        m_MeshNeedsRefreshing = true;
    }

    /// <summary>
    /// Sets the positions of all vertices in the line
    /// This method is preferred to SetPosition for updating multiple points, as it is more efficient to set all positions using a single command than to set each position individually.
    /// </summary>
    public void SetPositions(Vector3[] newPositions, bool knownSizeChange = false)
    {
        // Update internal data
        if (m_Positions == null || newPositions.Length != m_Positions.Length)
        {
            if (knownSizeChange == false)
            {
                Debug.LogWarning("New positions does not match size of existing array.  Adjusting vertex count as well");
            }
            m_Positions = newPositions;
            Initialize(true);
            return;
        }
        m_Positions = newPositions;

        // See if the data needs initializing
        if (Initialize())
        {
            return;
        }

        // Otherwise, do fast setting
        var pointCounter = 0;
        var elementCounter = 0;
        MeshData.SetElementPosition(elementCounter, ref m_Positions[pointCounter]);
        elementCounter++;
        pointCounter++;
        while (pointCounter < m_Positions.Length)
        {
            MeshData.SetElementPipe(elementCounter, ref m_Positions[pointCounter - 1], ref m_Positions[pointCounter]);
            elementCounter++;
            MeshData.SetElementPosition(elementCounter, ref m_Positions[pointCounter]);

            elementCounter++;
            pointCounter++;
        }

        // Dirty all the VRMeshChain flags so everything gets refreshed
        MeshData.SetMeshDataDirty(VRMeshChain.MeshRefreshFlag.Positions);
        m_MeshNeedsRefreshing = true;
    }

    /// <summary>
    /// Sets the number of billboard-line chains. This function regenerates the point list if the
    /// number of vertex points changes, so use it sparingly.
    /// </summary>
    public void SetVertexCount(int count)
    {
        // See if anything needs updating
        if (m_Positions.Length == count)
        {
            return;
        }

        // Adjust this array
        var newPositions = new Vector3[count];
        var copyCount = Mathf.Min(m_Positions.Length, count);
        var copyIndex = 0;

        while (copyIndex < copyCount)
        {
            newPositions[copyIndex] = m_Positions[copyIndex];
            copyIndex++;
        }
        m_Positions = newPositions;

        // Do an initialization, this changes everything
        Initialize(true);
    }
    
    /// <summary>
    /// Get the number of billboard-line chains.
    /// <summary
    public int GetVertexCount()
    {
        return m_Positions.Length;
    }

    /// <summary>
    /// Sets the line width at the start and at the end.
    /// Note, varying line widths will have a segmented appearance vs. the smooth look one gets with the traditional linerenderer.
    /// </summary>
    public void SetWidth(float start, float end)
    {
        // Update internal data
        m_WidthStart = start;
        m_WidthEnd = end;

        // See if the data needs initializing
        if (Initialize())
        {
            return;
        }

        // Otherwise, do fast setting
        var pointCounter = 0;
        var elementCounter = 0;

        // We go through the element list, much like initialization, but only update the width part of the variables
        MeshData.SetElementSize(elementCounter, m_WidthStart);
        elementCounter++;
        pointCounter++;

        var stepSize = 1.0f / Mathf.Max((m_Positions.Length - 1.0f), 1.0f);
        var stepPercent = stepSize;
        var lastWidth = m_WidthStart;

        while (pointCounter < m_Positions.Length)
        {
            var currentWidth = Mathf.Lerp(m_WidthStart, m_WidthEnd, stepPercent);

            MeshData.SetElementSize(elementCounter, lastWidth, currentWidth);
            elementCounter++;
            MeshData.SetElementSize(elementCounter, currentWidth);
            lastWidth = currentWidth;
            elementCounter++;
            pointCounter++;
            stepPercent += stepSize;
        }

        // Dirty all the VRMeshChain flags so everything gets refreshed
        MeshData.SetMeshDataDirty(VRMeshChain.MeshRefreshFlag.Sizes);
        m_MeshNeedsRefreshing = true;
    }

    /// <summary>
    /// Ensures the mesh data for the renderer is created, and updates it if neccessary
    /// </summary>
    /// <param name="force">Whether or not to force a full rebuild of the mesh data</param>
    /// <returns>True if an initialization occurred, false if it was skipped</returns>
    protected bool Initialize(bool force = false)
    {
        if (m_Positions == null)
        {
            return false;
        }
        var performFullInitialize = force;

        // For a line renderer we assume one big chain
        // We need a control point for each billboard and a control point for each pipe connecting them together
        // Except for the end, which must be capped with another billboard.  This gives us (positions * 2) - 1
        var neededPoints = Mathf.Max((m_Positions.Length * 2) - 1, 0);
        if (MeshData == null)
        {
            MeshData = new VRMeshChain();
        }
        if (MeshData.reservedElements != neededPoints)
        {
            MeshData.worldSpaceData = useWorldSpace;
            MeshData.GenerateMesh(gameObject, true, neededPoints);
            performFullInitialize = true;
        }
        if (performFullInitialize == false)
        {
            return false;
        }
        if (neededPoints == 0)
        {
            return true;
        }

        var pointCounter = 0;
        var elementCounter = 0;

        // Initialize the single starting point
        MeshData.SetElementSize(elementCounter, m_WidthStart);
        MeshData.SetElementPosition(elementCounter, ref m_Positions[pointCounter]);
        MeshData.SetElementColor(elementCounter, ref m_ColorStart);
        elementCounter++;
        pointCounter++;

        var stepSize = 1.0f / Mathf.Max((m_Positions.Length - 1.0f), 1.0f);
        var stepPercent = stepSize;
        var lastWidth = m_WidthStart;
        var lastColor = m_ColorStart;

        // Now do the chain
        while (pointCounter < m_Positions.Length)
        {
            var currentWidth = Mathf.Lerp(m_WidthStart, m_WidthEnd, stepPercent);
            var currentColor = Color.Lerp(m_ColorStart, m_ColorEnd, stepPercent);

            // Create a pipe from the previous point to here
            MeshData.SetElementSize(elementCounter, lastWidth, currentWidth);
            MeshData.SetElementPipe(elementCounter, ref m_Positions[pointCounter - 1], ref m_Positions[pointCounter]);
            MeshData.SetElementColor(elementCounter, ref lastColor, ref currentColor);
            elementCounter++;

            // Now record our own point data
            MeshData.SetElementSize(elementCounter, currentWidth);
            MeshData.SetElementPosition(elementCounter, ref m_Positions[pointCounter]);
            MeshData.SetElementColor(elementCounter, ref currentColor);

            // Go onto the next point while retaining previous values we might need to lerp between
            lastWidth = currentWidth;
            lastColor = currentColor;
            elementCounter++;
            pointCounter++;
            stepPercent += stepSize;
        }

        // Dirty all the VRMeshChain flags so everything gets refreshed
        MeshData.SetMeshDataDirty(VRMeshChain.MeshRefreshFlag.All);
        m_MeshNeedsRefreshing = true;
        return true;
    }

    /// <summary>
    /// Enables the internal mesh representing the line
    /// </summary>
    void OnEnable()
    {
        GetComponent<MeshRenderer>().enabled = true;
    }

    /// <summary>
    /// Disables the internal mesh representing the line
    /// </summary>
    void OnDisable()
    {
        GetComponent<MeshRenderer>().enabled = false;
    }
}