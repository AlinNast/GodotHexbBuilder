using Godot;
using System;
using System.Collections.Generic;

[Tool]
public partial class HexBuilderUi : Control
{
	// reference to the tool
	public HexBuilder hexBuilder;

	// UI needs to be Manually referenced
	[Export]
	Button testButton;
	[Export]
	SpinBox gridLengthSpinBox;
	[Export]
	SpinBox gridHeightSpinBox;
	[Export]
	Button generateGridButton;
	[Export]
	Button resetButton;
	[Export]
	VBoxContainer tileButtonContainer;


	private string hexTileFolderPath = "res://tiles/demo";

	private Dictionary<string, PackedScene> hexTileScenes = new Dictionary<string, PackedScene>();
	public PackedScene selectedTileScene;
	

	////////////////       Tool UI Life Cycle
	public override void _EnterTree(){
		resetButton.Pressed += StopTool; // add event to Pressed test button
		generateGridButton.Pressed += GenerateGrid; // add event to Pressed generate grid button
		resetButton.Visible = false; // Hide the reset button initially
		testButton.Pressed += testButtonPressed; // add event to Pressed test button
	}

	public void Init(){
		hexBuilder.Init();  // Initializes the functionality of the tool
	}

	public void GenerateGrid(){
		int length = (int)gridLengthSpinBox.Value;
		int height = (int)gridHeightSpinBox.Value;
		hexBuilder.SetGridSize(length, height);
		generateGridButton.GetParent<Control>().Visible = false; // Hide the generate grid button after generating the grid
		resetButton.Visible = true; // Hide the reset button initially
		PopulateTilePickerUI(); // Populate the tile picker UI with available hex tile scenes
		hexBuilder.Init();  // Initializes the functionality of the tool
	}

	public void StopTool(){
		foreach (Node child in tileButtonContainer.GetChildren())
        {
            child.QueueFree();
        }
        hexTileScenes.Clear();
		resetButton.Visible = false;
		generateGridButton.GetParent<Control>().Visible = true;
		hexBuilder.StopTool();  // Stops the functionality of the tool
	}

	public void PopulateTilePickerUI()
	{
		// Clear existing UI buttons and previous scene map
        foreach (Node child in tileButtonContainer.GetChildren())
        {
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
		else
		{
			dir.ListDirBegin();
			string fileName = dir.GetNext();

			// ButtonGroup forces CheckBox/Button nodes to act as single-choice radio options
			ButtonGroup tileButtonGroup = new ButtonGroup();
			bool isFirstButton = true;
			
			while (fileName != "")
			{
				if (fileName.EndsWith(".tscn"))
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

						// Create a CheckBox with radio button behavior
						CheckBox tileButton = new CheckBox();
						tileButton.Text = cleanName;
						tileButton.ButtonGroup = tileButtonGroup;

						// Capture local reference for delegate callback
						PackedScene currentScene = tileScene;
						tileButton.Pressed += () => SetTileToPaint(currentScene);

						// Auto-select the first tile found in the directory as default
						if (isFirstButton)
						{
							SetTileToPaint(currentScene);
							isFirstButton = false;
						}

						tileButtonContainer.AddChild(tileButton);
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
	}

	public void SetTileToPaint(PackedScene tileScene)
	{
		selectedTileScene = tileScene;
		hexBuilder.SetSelectedTileScene(tileScene); // Update the HexBuilder with the selected tile scene
		GD.Print($"Selected tile scene set to: {tileScene.ResourcePath}");
	}

	public void testButtonPressed(){
		hexBuilder.toolActive = !hexBuilder.toolActive; // Toggle the toolActive state
	}
}
