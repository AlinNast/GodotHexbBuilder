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
	/// toggles to the inspector allowing to paint hexes in the scene view. 
	/// </summary>
	[Export]
	public bool toolActive = true;

	[Export]
	public string tileFolderPath = "tiles/demo/"; // deffault parth to the folder with hexes to paint with
	// TODO make the tiles packed scenes

	public Vector2I gridSize = new Vector2I(10, 10); // the size of the initial grid
	// TODO make this customazible

	//// this was used to store the state of the tiles
	// private bool[,] primaryBlockGrid;
	// private Node3D[,] meshGrid;

	// Dictionary<string, string> fileReference = new Dictionary<string, string>();
	// Maps tile key names (e.g., "0", "1", "3") to their full file paths (e.g., "res://tiles/demo/tile_1.glb"). It gets filled dynamically when DirContents() scans your folder.

	/// ////// Private variables for the tool's internal state
	
	/// <summary>
	/// Ensures that Process doesent run before initialization
	/// </summary>
	bool init = false;

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

		for (int x = 0; x < gridSize.X; x++)
		{
			for (int y = 0; y < gridSize.Y; y++)
			{
				GD.Print("draw 3d is disabled");

			}
		}
		//DebugDraw3D.DrawSphere(new Vector3(pointerGridPos.X, 0, pointerGridPos.Z), 0.04f, Colors.Yellow, 0.1f);
	}

	public void Init()
	{
		GD.Print("Hex tool Plugin initialized");
		//fileReference.Clear();
		SetCurrentScene();
		//DirContents(tileFolderPath);
		//primaryBlockGrid = new bool[gridSize.X, gridSize.Y];
		//meshGrid = new Node3D[gridSize.X - 1, gridSize.Y - 1];
		// for (int y = 0; y < gridSize.Y - 1; y++)
		// {
		// 	for (int x = 0; x < gridSize.X - 1; x++)
		// 	{
		// 		primaryBlockGrid[x, y] = false;
		// 		SetTileMesh("0", x, y, 0);
		// 	}
		// }
		init = true;
		toolActive = true;
	}

	public void SetCurrentScene()
	{
		activeRoot = EditorInterface.Singleton.GetEditedSceneRoot();
	}

	public void CommitTiles()
	{
		// Loop all tileInstance.Owner = activeRoot;
		// Move all tile instances to current active root as well.
	}

	public void DiscardTiles()
	{
	}

	// public void SetTileMesh(string tile, int x, int y, int rotation)
	// {
	// 	if (meshGrid[x, y] != null)
	// 	{
	// 		meshGrid[x, y].QueueFree();
	// 		meshGrid[x, y] = null;
	// 	}

	// 	string resourcePath = fileReference[tile];
	// 	var asset = GD.Load<PackedScene>(resourcePath);
	// 	var tileInstance = (Node3D)asset.Instantiate();
	// 	activeRoot.AddChild(tileInstance);
	// 	tileInstance.Owner = activeRoot;
	// 	tileInstance.GlobalPosition = new Vector3(x + 0.5f, 0, y + 0.5f);
	// 	tileInstance.Scale = Vector3.One;
	// 	tileInstance.RotateY(Mathf.DegToRad(rotation));
	// 	meshGrid[x, y] = tileInstance;
	// 	tileInstance.Name = $"tile_x{x}y{y}";
	// }

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
			if (CheckIfValidPrimaryGrid(gridPoint))
			{
				SetBlockState(gridPoint, mouse.ButtonIndex == MouseButton.Left);
				pointerGridPos = new Vector3(gridPoint.X, 0, gridPoint.Y);
			}

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

	private bool CheckIfValidPrimaryGrid(Vector2I gridPos)
	{
		return gridPos.X >= 0 && gridPos.X < gridSize.X && gridPos.Y >= 0 && gridPos.Y < gridSize.Y;
	}

	private bool CheckIfValidDualGrid(Vector2I gridPos)
	{
		return gridPos.X >= 0 && gridPos.X < gridSize.X - 1 && gridPos.Y >= 0 && gridPos.Y < gridSize.Y - 1;
	}
}

