using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class UnitController : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────────
    public float moveSpeed = 5f;
    public GameManager.Team team;

    [Header("Path Preview Colours")]
    public Color previewColour   = new Color(1f, 1f, 0f, 0.6f);              // yellow line
    public Color lockedColour    = new Color(0f, 1f, 0f, 0.9f);              // green line
    public Color tileHighlight   = new Color(0.2f, 0.5f, 1f, 0.25f);        // blue tile wash
    public float lineWidth       = 0.08f;                                    // world-units

    // ── State machine ──────────────────────────────────────────────────────────
    private enum InputState { Idle, Locked }
    private InputState inputState = InputState.Idle;

    // ── Path data ──────────────────────────────────────────────────────────────
    private List<Vector3> previewPath;   // shown while hovering
    private List<Vector3> lockedPath;    // locked in after first click
    private List<Vector3> activePath;    // path currently being walked
    private int targetIndex;
    private bool isMoving = false;

    // ── Cached refs ───────────────────────────────────────────────────────────
    private Pathfinding pathfinding;
    private SpriteRenderer spriteRenderer;
    private Camera mainCam;

    // Track last hovered cell so we only re-run pathfinding when the cell changes
    private Vector3Int lastHoveredCell = new Vector3Int(int.MinValue, int.MinValue, 0);

    // ── GL line material (created once) ───────────────────────────────────────
    private static Material lineMat;

    // ─────────────────────────────────────────────────────────────────────────
    void Start()
    {
        pathfinding = FindObjectOfType<Pathfinding>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCam = Camera.main;

        if (team == GameManager.Team.Blue)
            GameManager.Instance.blueTeamUnits.Add(this);
        else
            GameManager.Instance.redTeamUnits.Add(this);

        EnsureLineMaterial();
        UpdateVisuals();
    }

    void OnDestroy()
    {
        if (team == GameManager.Team.Blue)
            GameManager.Instance.blueTeamUnits.Remove(this);
        else
            GameManager.Instance.redTeamUnits.Remove(this);
    }

    // ─────────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (!GameManager.Instance.IsUnitsTurn(this))
        {
            ClearPreviews();
            UpdateVisuals();
            return;
        }

        if (isMoving)
        {
            UpdateVisuals();
            return;
        }

        bool overUI = IsPointerOverUI();
        Vector3 mouseWorld = mainCam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        // ── Hover: refresh preview path only when the hovered cell changes ─────
        if (!overUI && inputState == InputState.Idle)
        {
            Vector3Int hoveredCell = pathfinding.WorldToCell(mouseWorld);
            if (hoveredCell != lastHoveredCell)
            {
                lastHoveredCell = hoveredCell;
                previewPath = pathfinding.FindPath(transform.position, mouseWorld);
            }
        }

        // ── Click handling ───────────────────────────────────────────────────
        if (Input.GetMouseButtonDown(0) && !overUI)
        {
            if (inputState == InputState.Idle)
            {
                // First click: lock whatever is being previewed
                if (previewPath != null && previewPath.Count > 0)
                {
                    lockedPath  = new List<Vector3>(previewPath);
                    previewPath = null;
                    inputState  = InputState.Locked;
                }
            }
            else // InputState.Locked
            {
                // Second click: execute the locked path
                StartMove(lockedPath);
                lockedPath = null;
                inputState = InputState.Idle;
            }
        }

        // Right-click cancels a lock
        if (Input.GetMouseButtonDown(1) && inputState == InputState.Locked)
        {
            lockedPath = null;
            inputState = InputState.Idle;
        }

        UpdateVisuals();
    }

    // ─────────────────────────────────────────────────────────────────────────
    private void StartMove(List<Vector3> path)
    {
        if (path == null || path.Count == 0) return;
        StopCoroutine("FollowPath");
        activePath  = path;
        targetIndex = 0;
        isMoving    = true;
        ClearPreviews();
        StartCoroutine("FollowPath");
    }

    IEnumerator FollowPath()
    {
        Vector3 currentWaypoint = activePath[0];

        while (true)
        {
            // Use a small distance threshold instead of exact float equality,
            // which can cause the unit to never reach the waypoint and loop forever.
            if (Vector3.Distance(transform.position, currentWaypoint) < 0.001f)
            {
                transform.position = currentWaypoint; // snap cleanly onto the node
                targetIndex++;
                if (targetIndex >= activePath.Count)
                {
                    isMoving = false;
                    yield break;
                }
                currentWaypoint = activePath[targetIndex];
            }

            transform.position = Vector3.MoveTowards(
                transform.position, currentWaypoint, moveSpeed * Time.deltaTime);
            yield return null;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    private void ClearPreviews()
    {
        previewPath = null;
        lockedPath  = null;
        inputState  = InputState.Idle;
    }

    // ── Visuals ───────────────────────────────────────────────────────────────
    void UpdateVisuals()
    {
        if (spriteRenderer == null) return;
        Color c = spriteRenderer.color;
        c.a = GameManager.Instance.IsUnitsTurn(this) ? 1f : 0.5f;
        spriteRenderer.color = c;
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null &&
               EventSystem.current.IsPointerOverGameObject();
    }

    // ── GL path drawing ───────────────────────────────────────────────────────
    // OnRenderObject is called after the camera renders the scene for every
    // camera; we guard against non-main cameras.
    void OnRenderObject()
    {
        if (Camera.current != mainCam) return;
        if (!GameManager.Instance.IsUnitsTurn(this)) return;

        List<Vector3> pathToDraw = null;

        if (inputState == InputState.Locked && lockedPath != null && lockedPath.Count > 0)
            pathToDraw = lockedPath;
        else if (inputState == InputState.Idle && previewPath != null && previewPath.Count > 0)
            pathToDraw = previewPath;

        if (pathToDraw == null) return;

        Color lineCol = (inputState == InputState.Locked) ? lockedColour : previewColour;

        // 1. Tile highlights (drawn first so the line sits on top)
        DrawTileHighlights(pathToDraw);

        // 2. Line ribbon + destination marker
        if (pathToDraw.Count > 1)
            DrawPath(pathToDraw, lineCol, transform.position);
    }

    /// <summary>
    /// Draws a full-tile quad under every cell in the path.
    /// </summary>
    private void DrawTileHighlights(List<Vector3> path)
    {
        if (lineMat == null) EnsureLineMaterial();

        Vector3 cellSize = pathfinding.CellSize;
        float hw = cellSize.x * 0.5f;   // half-width
        float hh = cellSize.y * 0.5f;   // half-height

        lineMat.SetPass(0);
        GL.PushMatrix();
        GL.Begin(GL.QUADS);
        GL.Color(tileHighlight);

        foreach (Vector3 centre in path)
        {
            float x = centre.x;
            float y = centre.y;
            float z = -0.05f; // just below the line ribbon

            GL.Vertex3(x - hw, y - hh, z);
            GL.Vertex3(x - hw, y + hh, z);
            GL.Vertex3(x + hw, y + hh, z);
            GL.Vertex3(x + hw, y - hh, z);
        }

        GL.End();
        GL.PopMatrix();
    }

    /// <summary>
    /// Draws a filled ribbon along <paramref name="path"/> using GL quads.
    /// The ribbon is slightly raised above z=0 so it sits on top of tiles.
    /// </summary>
    private void DrawPath(List<Vector3> path, Color colour, Vector3 startPos)
    {
        if (lineMat == null) EnsureLineMaterial();

        lineMat.SetPass(0);
        GL.PushMatrix();
        // Work in world space – no extra matrix needed
        GL.Begin(GL.QUADS);
        GL.Color(colour);

        float half = lineWidth * 0.5f;

        // Draw from unit position to first waypoint, then along all waypoints
        Vector3 prev = startPos;
        prev.z = -0.1f;

        for (int i = 0; i < path.Count; i++)
        {
            Vector3 curr = path[i];
            curr.z = -0.1f;

            Vector3 dir = (curr - prev).normalized;
            if (dir == Vector3.zero) { prev = curr; continue; }

            // Perpendicular in 2-D
            Vector3 perp = new Vector3(-dir.y, dir.x, 0f) * half;

            // Two quads per segment so the ribbon ends are capped
            GL.Vertex(prev - perp);
            GL.Vertex(prev + perp);
            GL.Vertex(curr + perp);
            GL.Vertex(curr - perp);

            prev = curr;
        }

        GL.End();
        GL.PopMatrix();

        // Draw a small square at the destination
        if (path.Count > 0)
        {
            Vector3 dest = path[path.Count - 1];
            dest.z = -0.1f;
            DrawSquare(dest, lineWidth * 1.5f, colour);
        }
    }

    private void DrawSquare(Vector3 centre, float size, Color colour)
    {
        float h = size * 0.5f;
        lineMat.SetPass(0);
        GL.PushMatrix();
        GL.Begin(GL.QUADS);
        GL.Color(colour);
        GL.Vertex(centre + new Vector3(-h, -h, 0f));
        GL.Vertex(centre + new Vector3(-h,  h, 0f));
        GL.Vertex(centre + new Vector3( h,  h, 0f));
        GL.Vertex(centre + new Vector3( h, -h, 0f));
        GL.End();
        GL.PopMatrix();
    }

    private static void EnsureLineMaterial()
    {
        if (lineMat != null) return;
        // Built-in unlit, vertex-colour shader – always available in Unity
        Shader shader = Shader.Find("Hidden/Internal-Colored");
        lineMat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        lineMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        lineMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        lineMat.SetInt("_Cull",     (int)UnityEngine.Rendering.CullMode.Off);
        lineMat.SetInt("_ZWrite",   0);
        lineMat.SetInt("_ZTest",    (int)UnityEngine.Rendering.CompareFunction.Always);
    }
}
