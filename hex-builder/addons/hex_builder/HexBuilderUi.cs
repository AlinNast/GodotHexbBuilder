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


	////////////////       Tool UI Life Cycle
	public override void _EnterTree(){
		testButton.Pressed += Init; // add event to Pressed test button
	}

	public void Init(){
		hexBuilder.Init();  // Initializes the functionality of the tool
	}
}
