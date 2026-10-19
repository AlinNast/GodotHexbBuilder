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


	///////////////  PAINT STATE
	[Export]
	LineEdit tileFolderLineEdit;
	[Export]
	Button tileFolderBrowseButton;



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


	private string hexTileFolderPath = "res://addons/hex_builder/tiles";

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

		if (tileFolderLineEdit != null)
		{
			tileFolderLineEdit.Text = hexTileFolderPath;
		}

		// --- NEW: Connect the browse button to trigger path parsing ---
		if (tileFolderBrowseButton != null)
		{
			tileFolderBrowseButton.Pressed += OnBrowseButtonPressed;
		}
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


	////////////// Paint State UI Functions
	
	private void OnBrowseButtonPressed()
	{
		string newPath = tileFolderLineEdit.Text;

		// Basic validation to ensure Godot directory path formatting
		string trimmedPath = newPath.Trim();
		if (!trimmedPath.StartsWith("res://"))
		{
			GD.PrintErr("Invalid path. Folder path must start with 'res://'");
			// Reset text back to current valid path
			tileFolderLineEdit.Text = hexTileFolderPath;
			return;
		}

		// Check if the directory actually exists
		if (!DirAccess.DirExistsAbsolute(trimmedPath))
		{
			GD.PrintErr($"Directory does not exist: {trimmedPath}");
			tileFolderLineEdit.Text = hexTileFolderPath;
			return;
		}

		// Update path and repopulate the scroll container UI with the new tiles
		hexTileFolderPath = trimmedPath;
		PopulateTilePickerUI();
		GD.Print($"Tile folder updated to: {hexTileFolderPath}");
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

					// Use actual scene filename (without .tscn extension)
					string sceneName = fileName.Replace(".tscn", "");

					TileItemUi tileButton = paletteItemPrefab.Instantiate<TileItemUi>();
					tileButtonContainer.AddChild(tileButton);

					PackedScene currentScene = tileScene;

					// Setup button using the actual filename and group
					tileButton.Setup(sceneName, tileButtonGroup, isFirstButton);
					tileButton.OnSelected += () => SetTileToPaint(currentScene);

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
		hexBuilder.isPainitngEnabled = false; // Disable painting mode
		
		TilePickerContainer.Visible = false;
		GeneratorContainer.Visible = false;
		ReadyContainer.Visible = true;
	}
	



}
