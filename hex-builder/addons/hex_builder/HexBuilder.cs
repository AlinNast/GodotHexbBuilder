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
	private Dictionary<Vector3I, Node3D> hexGrid = new Dictionary<Vector3I, Node3D>();

	private Node3D mapContainer; // Node3D that will hold all the hex tiles in the scene

	/// <summary>
	/// Track where the mouse is hovering in the grid
	/// </summary>
	Vector3 pointerGridPos = Vector3.Zero;
	private EditorDock dock;

	/// <summary>
	/// Stores a reference to the active root node of the scene currently being edited in Godot so spawned tiles can be parented to it.
	/// </summary>
	private Node activeRoot;

	/// <summary>
	/// Stores a reference to the HexBuilderUi instance that is added to the dock.
	/// </summary>
	private HexBuilderUi dockUI;

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

		//ConstructDebugGrid();
		
		DebugDraw3D.DrawSphere(new Vector3(pointerGridPos.X, 0, pointerGridPos.Z), 0.04f, Colors.Yellow, 0.1f);
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

	public void SetCurrentScene()
	{
		activeRoot = EditorInterface.Singleton.GetEditedSceneRoot();
	}


	public override int _Forward3DGuiInput(Camera3D camera, InputEvent @event)
	{
		if (!toolActive)
		{
			return (int)EditorPlugin.AfterGuiInput.Pass;
		}

		if (@event is InputEventMouseMotion motion)
		{
			UpdatePointerPosition(camera, motion.Position);
			return (int)EditorPlugin.AfterGuiInput.Pass;
		}

		if (@event is InputEventMouseButton mouse &&
			mouse.Pressed &&
			(mouse.ButtonIndex == MouseButton.Left || mouse.ButtonIndex == MouseButton.Right))
		{
			Vector2I gridPoint = RayToGridPoint(camera, mouse.Position);
			// if (CheckIfValidPrimaryGrid(gridPoint))
			// {
			// 	SetBlockState(gridPoint, mouse.ButtonIndex == MouseButton.Left);
			// 	pointerGridPos = new Vector3(gridPoint.X, 0, gridPoint.Y);
			// }

			return (int)EditorPlugin.AfterGuiInput.Stop;
		}

		return (int)EditorPlugin.AfterGuiInput.Pass;
	}

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

	private void UpdatePointerPosition(Camera3D camera, Vector2 screenPos)
	{
		Vector3 rayOrigin = camera.ProjectRayOrigin(screenPos);
		Vector3 rayDir = camera.ProjectRayNormal(screenPos);

		Plane plane = new Plane(Vector3.Up, 0);
		Vector3? hit = plane.IntersectsRay(rayOrigin, rayDir);
		if (hit == null)
		{
			return;
		}

		pointerGridPos = new Vector3(
			Mathf.RoundToInt(hit.Value.X),
			0,
			Mathf.RoundToInt(hit.Value.Z)
		);
	}

	public void SetBlockState(Vector2I pos, bool state)
	{
		// // if (!CheckIfValidPrimaryGrid(pos))
		// {
		// 	return;
		// }

		// primaryBlockGrid[pos.X, pos.Y] = state;
		// CalculateBitMask(pos);
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



            /////////////////////////////////  UTILITY FUNCTIONS  //////////////////////////////////////
	


	private void ConstructDebugGrid()
	{
		// Unit dimensions of the grid
		float unitL = Mathf.Sqrt(3) * hex_size; // Width of a hexagon
		float unitH = 2 * hex_size; // Height of a hexagon
		float unitVSpacing = 1.5f * hex_size; // Vertical spacing between hexagon centers

		float L = gridLength * unitL;
    	float H = (gridHeight > 1) ? ((gridHeight - 1) * unitVSpacing + unitH) : unitH;
		// Radius / Half-dimensions
		float halfL = L / 2.0f;
		float halfH = H / 2.0f;

		float sideZ = halfH - hex_size; // Z-coordinate for the side vertices of the hexagon

		if (L <= H)
		{
			// --- POINTED-TOP HEXAGON ---
			// Side vertices sit at Z = +/- (H / 4) to ensure equal side lengths when L == H
			

			Vector3 topPoint         = new Vector3(0,       0,  halfH);
			Vector3 topRightPoint    = new Vector3(halfL,   0,  sideZ);
			Vector3 bottomRightPoint = new Vector3(halfL,   0, -sideZ);
			Vector3 bottomPoint      = new Vector3(0,       0, -halfH);
			Vector3 bottomLeftPoint  = new Vector3(-halfL,  0, -sideZ);
			Vector3 topLeftPoint     = new Vector3(-halfL,  0,  sideZ);

			DebugDraw3D.DrawLine(topPoint, topRightPoint, Colors.Red);
			DebugDraw3D.DrawLine(topRightPoint, bottomRightPoint, Colors.Red);
			DebugDraw3D.DrawLine(bottomRightPoint, bottomPoint, Colors.Red);
			DebugDraw3D.DrawLine(bottomPoint, bottomLeftPoint, Colors.Red);
			DebugDraw3D.DrawLine(bottomLeftPoint, topLeftPoint, Colors.Red);
			DebugDraw3D.DrawLine(topLeftPoint, topPoint, Colors.Red);
		}
		else
		{
			// --- OCTAGON (Elongated Length) ---
			// Top and bottom points split horizontally by (L - H)
			float topFlatHalfWidth = halfL - (unitL / 2.0f);
	
			Vector3 topLeftFlat     = new Vector3(-topFlatHalfWidth, 0,  halfH);
			Vector3 topRightFlat    = new Vector3( topFlatHalfWidth, 0,  halfH);
			Vector3 rightTop        = new Vector3( halfL,            0,  sideZ);
			Vector3 rightBottom     = new Vector3( halfL,            0, -sideZ);
			Vector3 bottomRightFlat = new Vector3( topFlatHalfWidth, 0, -halfH);
			Vector3 bottomLeftFlat  = new Vector3(-topFlatHalfWidth, 0, -halfH);
			Vector3 leftBottom      = new Vector3(-halfL,            0, -sideZ);
			Vector3 leftTop         = new Vector3(-halfL,            0,  sideZ);

			DebugDraw3D.DrawLine(topLeftFlat, topRightFlat, Colors.Red);
			DebugDraw3D.DrawLine(topRightFlat, rightTop, Colors.Red);
			DebugDraw3D.DrawLine(rightTop, rightBottom, Colors.Red);
			DebugDraw3D.DrawLine(rightBottom, bottomRightFlat, Colors.Red);
			DebugDraw3D.DrawLine(bottomRightFlat, bottomLeftFlat, Colors.Red);
			DebugDraw3D.DrawLine(bottomLeftFlat, leftBottom, Colors.Red);
			DebugDraw3D.DrawLine(leftBottom, leftTop, Colors.Red);
			DebugDraw3D.DrawLine(leftTop, topLeftFlat, Colors.Red);
		}

	}
	
	private void ConstructGrid()
	{
		ClearGrid(); // Clear any existing hexes in the grid

		mapContainer = new Node3D();
		mapContainer.Name = "Map";
		AddChild(mapContainer);

		// Node sceneRoot = GetTree().activeScene?.Root;
		// if (sceneRoot != null)
		// {
		// 	mapContainer.Owner = sceneRoot;
		// }

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
				
				// Add the hex tile instance to the map container
				mapContainer.AddChild(hexTileInstance);

				// Set the position of the hex tile instance based on its grid coordinates
				hexTileInstance.GlobalPosition = new Vector3(xPos, 0, zPos);

				// Store the hex tile instance in the dictionary with its grid position as the key
				Vector3I gridPos = new Vector3I(col, 0, row);
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


	// private bool CheckIfValidPrimaryGrid(Vector2I gridPos)
	// {
	// 	return gridPos.X >= 0 && gridPos.X < gridSize.X && gridPos.Y >= 0 && gridPos.Y < gridSize.Y;
	// }

	// private bool CheckIfValidDualGrid(Vector2I gridPos)
	// {
	// 	return gridPos.X >= 0 && gridPos.X < gridSize.X - 1 && gridPos.Y >= 0 && gridPos.Y < gridSize.Y - 1;
	// }
}

