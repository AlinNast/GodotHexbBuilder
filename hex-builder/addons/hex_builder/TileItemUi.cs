using Godot;
using System;

[Tool]
public partial class TileItemUi : Button
{

	[Export]
	public TextureRect IconTextureRect;

	[Export]
	public Label TileNameLabel;


	public System.Action OnSelected;

	public override void _Ready()
	{
		ToggleMode = true; // Enable toggle mode for the button
		Pressed += HandlePressed; // Connect the Pressed signal to the handler

		FocusMode = FocusModeEnum.None; // Disable focus for the button to prevent unwanted focus behavior
	}

    public void Setup(string title, Texture2D icon, ButtonGroup group, bool isSelected)
    {
        

		if (TileNameLabel != null)
		{
			TileNameLabel.Text = title;
		}

        if (icon != null)
        {
            IconTextureRect.Texture = icon;
        }

        ButtonGroup = group;
		ButtonPressed = isSelected; // Set the initial selection state of the button
    }

	private void HandlePressed()
	{
		OnSelected?.Invoke(); // Invoke the OnSelected action if it's not null
	}
}	

