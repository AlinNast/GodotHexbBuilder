using Godot;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

[Tool]
public partial class HexBuilder : EditorPlugin
{
	// [ExportToolButton("Hex Tool")]
	// public Callable InitToolButton => Callable.From(Init);

	/// <summary>
	/// Toggles to the inspector allowing to paint hexes in the scene view. 
	/// </summary>
	[Export]
	public bool toolActive = true;

	[Export]
	public PackedScene tileScene; // Packed scene of the hex tile to paint with

	public int gridLength = 5;
	public int gridHeight = 5;
	// TODO make this customazible

	
	/// ////// Private variables for the tool's internal state
	
	/// <summary>
	/// The unit of one hex
	/// </summary>
	int hex_size = 1; // Size of the hexagon tiles
	
	/// <summary>
	/// Ensures that Process doesent run before initialization
	/// </summary>
	bool init = false;

    /// <summary>
	/// Stores the Hexes as value and they are accessible by their grid position as key. 
	/// </summary>
	private Dictionary<Vector2I, Node3D> hexGrid = new Dictionary<Vector2I, Node3D>();

	/// <summary>
	/// Gets created when map is generated and deleted on reset. It is the parent of all hexes
	/// </summary>
	private Node3D mapContainer; // Node3D that will hold all the hex tiles in the scene

	/// <summary>
	/// Track where the mouse is hovering in the grid
	/// </summary>
	Vector3? hoveredTilePosition = null;


	/// <summary>
	/// Stores a reference to the EditorDock that will hold the HexBuilderUi instance. 
	/// </summary>
	private EditorDock dock;

	/// <summary>
	/// Stores a reference to the active root node of the scene currently being edited in Godot so spawned tiles can be parented to it.
	/// </summary>
	private Node activeRoot;

	/// <summary>
	/// Stores a reference to the HexBuilderUi instance that is added to the dock.
	/// </summary>
	private HexBuilderUi dockUI;

	/// <summary>
	/// // Packed scene of the hex tile to paint with, selected from the UI
	/// </summary>
	private PackedScene selectedPaintTileScene; 


/// /////////////////////////////// Plugin Life Cycle Functions////////////////////////////////////
	public override void _EnterTree()
	{
		/// Create a new dock for the Hex Builder tool and add it to the editor's interface
		dock = new EditorDock();
		dock.Title = "Hex builder";
		dock.DefaultSlot = EditorDock.DockSlot.RightUl;

		/// Load the HexBuilderUi scene, instantiate it, and inject the reference to this HexBuilder
		dockUI = GD.Load<PackedScene>("res://addons/hex_builder/hex_builder_ui.tscn").Instantiate<HexBuilderUi>();
		dockUI.hexBuilder = this;

		/// Add the HexBuilderUi instance to the dock and add the dock to the editor
		dock.AddChild(dockUI);
		AddDock(dock);

		///
		SetInputEventForwardingAlwaysEnabled(); // Required for raycasting in Scene.
		GD.Print("Hex tool Plugin enabled");
	}

	public override void _ExitTree()
	{
		RemoveDock(dock);
		dock.QueueFree();
		GD.Print("Plugin disabled");
	}

	public override void _Process(double delta)
	{
		if (!init)
		{
			return;
		}

		ConstructDebugGrid();
		
	}

	public void Init()
	{
		GD.Print("Hex tool Plugin initialized");
		SetCurrentScene();

		// To be replaced with a dynamic loading of the hex tile scenes from the specified folder path
		tileScene = GD.Load<PackedScene>("res://tiles/demo/hex_tile_base.tscn");
		//DirContents(tileFolderPath);

		ConstructGrid();
		
		init = true;
		toolActive = true;
	}


/// ////////////////////////////////////  Input Handling Functions  //////////////////////////////////////

/// //  This is a built-in Godot EditorPlugin method. It intercepts raw mouse and keyboard events directly inside the 3D Editor Viewport before Godot handles them.
	public override int _Forward3DGuiInput(Camera3D camera, InputEvent @event)
	{
		// If the tool is not active, pass the input event to Godot for normal processing
		if (!toolActive)
		{
			return (int)EditorPlugin.AfterGuiInput.Pass;
		}

		// Handle mouse motion events to update the pointer position in the grid
		if (@event is InputEventMouseMotion motion)
		{
			HandleMouseHover(camera, motion.Position);
			return (int)EditorPlugin.AfterGuiInput.Pass;
		}

		// Handle mouse button events to set the state of the hex tile at the pointer position
		if (@event is InputEventMouseButton mouse && mouse.Pressed && mouse.ButtonIndex == MouseButton.Left)
		{

			// Case where you left click the hex in the viewport
			Vector3? hitWorldPos = GetWorldHitPosition(camera, mouse.Position);
			if (hitWorldPos.HasValue)
			{
				Vector3I gridCoord3D = WorldToGridCoord(hitWorldPos.Value);
				Vector2I gridCoord = new Vector2I(gridCoord3D.X, gridCoord3D.Z);

				PaintTileAt(gridCoord, hitWorldPos.Value);
			}
			

			return (int)EditorPlugin.AfterGuiInput.Pass;
		}
		return (int)EditorPlugin.AfterGuiInput.Pass;
	}

	// 	
	private Vector2I RayToGridPoint(Camera3D camera, Vector2 screenPos)
	{
		Vector3 rayOrigin = camera.ProjectRayOrigin(screenPos);
		Vector3 rayDir = camera.ProjectRayNormal(screenPos);

		Plane plane = new Plane(Vector3.Up, 0);
		Vector3? hit = plane.IntersectsRay(rayOrigin, rayDir);
		if (hit == null)
		{
			return new Vector2I(-1, -1);
		}

		return new Vector2I(
			Mathf.RoundToInt(hit.Value.X),
			Mathf.RoundToInt(hit.Value.Z)
		);
	}

	/// Handle mouse hover to update the hovered tile position
	private void HandleMouseHover(Camera3D camera, Vector2 screenPos)
	{
		Vector3? hitWorldPos = GetWorldHitPosition(camera, screenPos);

		if (hitWorldPos.HasValue)
		{
			Vector3I gridCoord = WorldToGridCoord(hitWorldPos.Value);
			Node3D hoveredTile = GetHexAt(new Vector2I(gridCoord.X, gridCoord.Z));

			if (hoveredTile != null)
			{
				// Store the global position of the hovered tile
				hoveredTilePosition = hoveredTile.GlobalPosition;
				return;
			}
		}

		// Clear hover state if mouse moves off the grid
		hoveredTilePosition = null;
	}


	public void DirContents(string path)
	{
		GD.Print(path);
		using var dir = DirAccess.Open(path);
		if (dir != null)
		{
			dir.ListDirBegin();
			string fileName = dir.GetNext();
			while (fileName != "")
			{
				if (!dir.CurrentIsDir())
				{
					if (!fileName.Contains("import"))
					{
						string result = Regex.Replace(fileName, "\\.glb$", "", RegexOptions.IgnoreCase);
						//fileReference.Add(result, path + fileName);
						GD.Print(result + "  " + path + fileName);
					}
				}
				fileName = dir.GetNext();
			}
		}
		else
		{
			GD.Print("An error occurred when trying to access the path.");
		}
	}

	 /// ////////////////////////////////////  UI COM FUNCTIONS  //////////////////////////////////////
	public void SetGridSize(int length, int height)
	{
		gridLength = length;
		gridHeight = height;
	}

	public void StopTool()
	{
		ClearGrid();
		toolActive = false;
		init = false;
	}


	public void SetSelectedTileScene(PackedScene scene)
	{
		selectedPaintTileScene = scene;
	}


            /////////////////////////////////  UTILITY FUNCTIONS  //////////////////////////////////////
	


	private void ConstructDebugGrid()
	{
		if (hoveredTilePosition.HasValue)
		{
			// Slightly lift outline (Y + 0.05f) to avoid z-fighting with the tile mesh
			Vector3 center = hoveredTilePosition.Value + new Vector3(0, 0.2f, 0);

			// Get 6 corner positions for unit hex_size
			Vector3[] corners = GetHexCorners(center, hex_size);

			// Draw the 6 boundary edges using DebugDraw3D
			DebugDraw3D.DrawLine(corners[0], corners[1], Colors.Yellow);
			DebugDraw3D.DrawLine(corners[1], corners[2], Colors.Yellow);
			DebugDraw3D.DrawLine(corners[2], corners[3], Colors.Yellow);
			DebugDraw3D.DrawLine(corners[3], corners[4], Colors.Yellow);
			DebugDraw3D.DrawLine(corners[4], corners[5], Colors.Yellow);
			DebugDraw3D.DrawLine(corners[5], corners[0], Colors.Yellow);
		}

	}
	
	private void ConstructGrid()
	{
		ClearGrid(); // Clear any existing hexes in the grid

		// Create a new Node3D to hold the hex tiles
		mapContainer = new Node3D();
		mapContainer.Name = "Map";
		if (activeRoot != null)
		{
			activeRoot.AddChild(mapContainer);
			mapContainer.Owner = activeRoot; // Set owner AFTER AddChild
		}
		else
		{
			AddChild(mapContainer);
		}

	

		float unitL = Mathf.Sqrt(3) * hex_size; // Width of a hexagon
		float unitVSpacing = 1.5f * hex_size; // Vertical spacing between Rows

		// loop through the rows
		for (int row = 0; row < gridHeight; row++)
		{
			float zPos = row * unitVSpacing; // spread the columns

			// loop through the columns
			for (int col = 0; col < gridLength; col++)
			{
				float xPos = col * unitL;

				// Stagger every other row
				if (row % 2 != 0)
				{
					xPos += unitL / 2.0f;
				}

				// Create a new instance of the hex tile for this grid position
				Node3D hexTileInstance = tileScene.Instantiate<Node3D>();

				// Give each tile a readable unique name in the scene tree
		        hexTileInstance.Name = $"Hex_{col}_{row}";
				
				// Add the hex tile instance to the map container
				mapContainer.AddChild(hexTileInstance, forceReadableName: true);

				// Assign the scene root owner so it shows in the editor Scene Dock
				if (activeRoot != null)
				{
					hexTileInstance.Owner = activeRoot;
				}

				// Set the position of the hex tile instance based on its grid coordinates
				hexTileInstance.GlobalPosition = new Vector3(xPos, 0, zPos);

				// Store the hex tile instance in the dictionary with its grid position as the key
				Vector2I gridPos = new Vector2I(col, row);
				hexGrid[gridPos] = hexTileInstance;
			}
		}

	}
	
	private void ClearGrid()
    {
        mapContainer?.QueueFree(); // Remove the map container and all its children from the scene
		mapContainer = null; // Reset the map container reference
        hexGrid.Clear();
    }

	public void SetCurrentScene()
	{
		activeRoot = EditorInterface.Singleton.GetEditedSceneRoot();
	}

	private void PaintTileAt(Vector2I gridCoord, Vector3 worldPos)
	{
		if (selectedPaintTileScene == null)
		{
			GD.PrintErr("No tile scene selected for painting.");
			return;
		}


		// If a tile exists there, remove it first
		Node3D existingTile = GetHexAt(gridCoord);
		if (existingTile != null)
		{
			existingTile.QueueFree();
			hexGrid.Remove(gridCoord);
		}

		// Create a new instance of the selected tile scene
		Node3D newTileInstance = selectedPaintTileScene.Instantiate<Node3D>();
		newTileInstance.Name = $"Hex_{gridCoord.X}_{gridCoord.Y}";
		hexGrid[gridCoord] = newTileInstance; // Store the new tile in the dictionary

		// Add the hex tile instance to the map container
				mapContainer.AddChild(newTileInstance, forceReadableName: true);

				// Assign the scene root owner so it shows in the editor Scene Dock
				if (activeRoot != null)
				{
					newTileInstance.Owner = activeRoot;
				}


		// Set the position of the new tile instance based on its grid coordinates
		Vector3 tileWorldPos = GridCoordToWorld(gridCoord);
		newTileInstance.GlobalPosition = tileWorldPos;
	}

	private Vector3? GetWorldHitPosition(Camera3D camera, Vector2 screenPos)
	{
		Vector3 rayOrigin = camera.ProjectRayOrigin(screenPos);
		Vector3 rayDir = camera.ProjectRayNormal(screenPos);

		Plane plane = new Plane(Vector3.Up, 0);
		return plane.IntersectsRay(rayOrigin, rayDir);
	}

	private Vector3I WorldToGridCoord(Vector3 worldPos)
	{
		float unitWidth = Mathf.Sqrt(3.0f) * hex_size;
		float rowStep = 1.5f * hex_size;

		// Calculate nearest row index
		int row = Mathf.RoundToInt(worldPos.Z / rowStep);

		// Un-stagger X position for odd rows
		float xPos = worldPos.X;
		if (row % 2 != 0)
		{
			xPos -= unitWidth / 2.0f;
		}

		// Calculate nearest column index
		int col = Mathf.RoundToInt(xPos / unitWidth);

		return new Vector3I(col, 0, row);
	}

	// Convert Vector2I (col, row) back to exact 3D World Position
	private Vector3 GridCoordToWorld(Vector2I gridCoord)
	{
		float unitWidth = Mathf.Sqrt(3.0f) * hex_size;
		float rowStep = 1.5f * hex_size;

		float xPos = gridCoord.X * unitWidth;
		if (gridCoord.Y % 2 != 0)
		{
			xPos += unitWidth / 2.0f;
		}

		float zPos = gridCoord.Y * rowStep;

		return new Vector3(xPos, 0, zPos);
	}

	public Node3D GetHexAt(Vector2I gridCoord)
    {
        if (hexGrid.TryGetValue(gridCoord, out Node3D hex))
        {
            return hex;
        }
        return null;
    }

	private Vector3[] GetHexCorners(Vector3 center, float size)
	{
		float halfWidth = (Mathf.Sqrt(3.0f) / 2.0f) * size;
		float halfHeight = size;
		float sideZ = size / 2.0f;

		return new Vector3[]
		{
			center + new Vector3(0, 0, halfHeight),           // 0: Top Peak
			center + new Vector3(halfWidth, 0, sideZ),        // 1: Top Right
			center + new Vector3(halfWidth, 0, -sideZ),       // 2: Bottom Right
			center + new Vector3(0, 0, -halfHeight),          // 3: Bottom Peak
			center + new Vector3(-halfWidth, 0, -sideZ),      // 4: Bottom Left
			center + new Vector3(-halfWidth, 0, sideZ)        // 5: Top Left
		};
	}

	

}

