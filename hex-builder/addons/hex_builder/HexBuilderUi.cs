using Godot;
using System;
using System.Collections.Generic;

[Tool]
public partial class HexBuilderUi : Control
{
	// reference to the tool
	public HexBuilder hexBuilder;

	///////// READY STATE
	[Export]
	Button InitializeButton;
	[Export]
	VBoxContainer ReadyContainer;
	[Export]
	ConfirmationDialog ConfirmDialog;


	////////// FEEDFORWARD STATE
	[Export]
	Button CancelPreviewButton;
	[Export]
	SpinBox gridLengthSpinBox;
	[Export]
	SpinBox gridHeightSpinBox;
	[Export]
	SpinBox gridScaleSpinBox;
	[Export]
	Button generateGridButton;

	// UI needs to be Manually referenced
	[Export]
	Button testButton;
	[Export]
	Button resetButton;
	[Export]
	Button confirmButton;
	[Export]
	VBoxContainer tileButtonContainer;
	[Export]
	VBoxContainer GeneratorContainer;
	[Export]
	VBoxContainer TilePickerContainer;
	[Export]
	PackedScene paletteItemPrefab; // Drag tile_item_UI.tscn here in Inspector


	private string hexTileFolderPath = "res://tiles/demo";

	private Dictionary<string, PackedScene> hexTileScenes = new Dictionary<string, PackedScene>();
	public PackedScene selectedTileScene;
	

	////////////////       Tool UI Life Cycle
	public override void _EnterTree(){
		ReadyContainer.Visible = true; // Show the ready container when the tool becomes active
		GeneratorContainer.Visible = false; // Show the generator container when the tool becomes active
		TilePickerContainer.Visible = false; // Hide the tile picker container when the tool becomes active
		
		resetButton.Pressed += StopTool; // add event to Pressed test button
		generateGridButton.Pressed += GenerateGrid; // add event to Pressed generate grid button
		confirmButton.Pressed += confirmButtonPressed; // add event to Pressed confirm button
		InitializeButton.Pressed += InitPopUp; // add event to Pressed initialize button
		CancelPreviewButton.Pressed += StopTool; // add event to Pressed cancel preview button

		if (ConfirmDialog != null)
		{
			ConfirmDialog.Confirmed += InitConfirmed; 
		}

		gridLengthSpinBox.ValueChanged += OnGridSizeChanged;
		gridHeightSpinBox.ValueChanged += OnGridSizeChanged;
		gridScaleSpinBox.ValueChanged += OnGridSizeChanged;
	}

	public override void _ExitTree(){
		resetButton.Pressed -= StopTool; // remove event to Pressed test button
		generateGridButton.Pressed -= GenerateGrid; // remove event to Pressed generate grid button
		confirmButton.Pressed -= confirmButtonPressed; // remove event to Pressed confirm button
		InitializeButton.Pressed -= InitPopUp; // remove event to Pressed initialize button
		CancelPreviewButton.Pressed -= StopTool; // remove event to Pressed cancel preview button

		if (ConfirmDialog != null)
		{
			ConfirmDialog.Confirmed -= InitConfirmed; 
		}

		gridLengthSpinBox.ValueChanged -= OnGridSizeChanged;
		gridHeightSpinBox.ValueChanged -= OnGridSizeChanged;
		gridScaleSpinBox.ValueChanged -= OnGridSizeChanged;
	}

	public void Init(){
		hexBuilder.Init();  // Initializes the functionality of the tool
	}

	public void StopTool(){
		foreach (Node child in tileButtonContainer.GetChildren())
        {
            child.QueueFree();
        }
        
		hexTileScenes.Clear();
		hexBuilder.StopTool();  // Stops the functionality of the tool
		
		TilePickerContainer.Visible = false;
		GeneratorContainer.Visible = false;
		ReadyContainer.Visible = true; 
	}


	/////////// Ready State UI Functions
	/// 
	public void InitPopUp()
	{
		if (ConfirmDialog != null)
		{

			ConfirmDialog.PopupCentered();
		}
		else
		{
			GD.PrintErr("ConfirmDialog is not assigned in the inspector.");
		}
	}

	public void InitConfirmed()
	{
		ReadyContainer.Visible = false; // Hide the ready container after confirming initialization
		GeneratorContainer.Visible = true; // Show the generator container after confirming initialization
		Init(); // Call the Init function to initialize the tool
	}


    ////////////// Feedforward State UI Functions
	
	private void OnGridSizeChanged(double value)
	{
		int length = (int)gridLengthSpinBox.Value;
		int height = (int)gridHeightSpinBox.Value;
		float scale = (float)gridScaleSpinBox.Value;
		hexBuilder.RebuildGrid(length, height, scale); // Update the grid preview whenever the grid size changes
	}


	public void GenerateGrid(){
		int length = (int)gridLengthSpinBox.Value;
		int height = (int)gridHeightSpinBox.Value;
		float scale = (float)gridScaleSpinBox.Value;
		hexBuilder.RebuildGrid(length, height, scale);

		PopulateTilePickerUI(); // Populate the tile picker UI with available hex tile scenes

		GeneratorContainer.Visible = false; // hide the generator container after generating the grid
		TilePickerContainer.Visible = true; // Show the tile picker container after generating the grid
		hexBuilder.isPainitngEnabled = true; // Enable painting mode
	}


	public void PopulateTilePickerUI()
	{
		// Check if the paletteItemPrefab and tileButtonContainer are assigned
		if (paletteItemPrefab == null)
		{
			GD.PrintErr("Palette item prefab is not assigned in the inspector.");
			return;
		}

		if (tileButtonContainer == null)
		{
			GD.PrintErr("tileButtonContainer is not assigned in the inspector.");
			return;
		}

		// Clear existing UI buttons and previous scene map
        foreach (Node child in tileButtonContainer.GetChildren())
        {
			tileButtonContainer.RemoveChild(child);
            child.QueueFree();
        }
        hexTileScenes.Clear();

		// Scan the directory
		using var dir = DirAccess.Open(hexTileFolderPath);
		if (dir == null)
		{
			GD.PrintErr($"Failed to open directory: {hexTileFolderPath}");
			return;
		}
		
		dir.ListDirBegin();
		string fileName = dir.GetNext();

		// ButtonGroup forces CheckBox/Button nodes to act as single-choice radio options
		ButtonGroup tileButtonGroup = new ButtonGroup();
		bool isFirstButton = true;
			
		// loop through all files in the directory
		while (fileName != "")
		{
			// Ignore directories, hidden files, and Godot remap files
			if (!dir.CurrentIsDir() && fileName.EndsWith(".tscn") && !fileName.StartsWith("."))	
			{
				string scenePath = $"{hexTileFolderPath}/{fileName}";
				PackedScene tileScene = GD.Load<PackedScene>(scenePath);

				if (tileScene != null)
				{
					// Store the scene in the dictionary
					hexTileScenes[fileName] = tileScene;

					// Format clean name for UI (e.g. "grass_hex.tscn" -> "Grass Hex")
					string cleanName = fileName.Replace(".tscn", "").Replace("_", " ");
					cleanName = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(cleanName);

					// Load icon matching the scene filename (e.g. hex_tile_base.png)
                    string pngPath = $"{hexTileFolderPath}/{fileName.Replace(".tscn", ".png")}";
                    Texture2D tileIcon = null;
                    if (FileAccess.FileExists(pngPath))
                    {
                        tileIcon = GD.Load<Texture2D>(pngPath);
                    }

					// Create a new TileItemUi instance for the tile
					TileItemUi tileButton = paletteItemPrefab.Instantiate<TileItemUi>();
					tileButtonContainer.AddChild(tileButton);

					// Capture local reference for delegate callback
					PackedScene currentScene = tileScene;

					
					tileButton.Setup(cleanName, tileIcon, tileButtonGroup, isFirstButton);
					tileButton.OnSelected += () => SetTileToPaint(currentScene);

					// Auto-select the first tile found in the directory as default
					if (isFirstButton)
					{
						SetTileToPaint(currentScene);
						isFirstButton = false;
					}

					}
					else
					{
						GD.PrintErr($"Failed to load scene: {scenePath}");
					}
				}
			fileName = dir.GetNext();
		}
		dir.ListDirEnd();
	}
	

	public void SetTileToPaint(PackedScene tileScene)
	{
		selectedTileScene = tileScene;
		hexBuilder.SetSelectedTileScene(tileScene); // Update the HexBuilder with the selected tile scene
		GD.Print($"Selected tile scene set to: {tileScene.ResourcePath}");
	}

	public void confirmButtonPressed()
	{
		//hexTileScenes.Clear();
		hexBuilder.toolActive = false;
		hexBuilder.init = false; // Stops the functionality of the tool
		
		TilePickerContainer.Visible = false;
		GeneratorContainer.Visible = true;
	}
	



}
