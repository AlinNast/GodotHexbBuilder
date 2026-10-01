using Godot;
using System;

[Tool]
public partial class TileItemUi : PanelContainer
{

	[Export]
	public TextureRect IconTextureRect;

	[Export]
	public Label TileNameLabel;

	[Export]
	public CheckBox TileSelectCheckBox;

	public System.Action OnSelected;

    public void Setup(string title, Texture2D icon, ButtonGroup group, bool isSelected)
    {
        TileNameLabel.Text = title;
        TileSelectCheckBox.ButtonGroup = group;
        TileSelectCheckBox.ButtonPressed = isSelected;

        if (icon != null)
        {
            IconTextureRect.Texture = icon;
        }

        // Trigger selection callback when pressed
        TileSelectCheckBox.Pressed += () => OnSelected?.Invoke();
    }
}

