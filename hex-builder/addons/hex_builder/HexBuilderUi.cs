using Godot;
using System;

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
		hexBuilder.Init();  // Initializes the functionality of the tool
	}

	public void StopTool(){
		resetButton.Visible = false;
		generateGridButton.GetParent<Control>().Visible = true;
		hexBuilder.StopTool();  // Stops the functionality of the tool
	}

	public void testButtonPressed(){
		hexBuilder.toolActive = !hexBuilder.toolActive; // Toggle the toolActive state
	}
}
