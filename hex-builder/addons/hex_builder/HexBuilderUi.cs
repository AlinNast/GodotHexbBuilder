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


	////////////////       Tool UI Life Cycle
	public override void _EnterTree(){
		testButton.Pressed += StopTool; // add event to Pressed test button
		generateGridButton.Pressed += GenerateGrid; // add event to Pressed generate grid button
	}

	public void Init(){
		hexBuilder.Init();  // Initializes the functionality of the tool
	}

	public void GenerateGrid(){
		int length = (int)gridLengthSpinBox.Value;
		int height = (int)gridHeightSpinBox.Value;
		hexBuilder.SetGridSize(length, height);
		hexBuilder.Init();  // Initializes the functionality of the tool
	}

	public void StopTool(){
		hexBuilder.StopTool();  // Stops the functionality of the tool
	}
}
