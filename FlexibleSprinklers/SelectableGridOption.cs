using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Shockah.CommonModCode.GMCM;
using Shockah.Kokoro;
using Shockah.Kokoro.GMCM;
using Shockah.Kokoro.UI;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.FlexibleSprinklers;

public class SelectableGridOption
{
	private const int RowHeight = 60;
	private const float MaxCellSize = 36f;
	private const float CellSpacing = 12f;
	private const float Margin = 16f;
	private static readonly string ClickSoundName = "drumkit6";

	private readonly Func<IReadOnlySet<IntPoint>> GetValues;
	private readonly Action<IReadOnlySet<IntPoint>> SetValues;
	private readonly Func<string> Name;
	private readonly Func<string>? Tooltip;
	private readonly Action? AfterValuesUpdated;

	private readonly Lazy<TextureRectangle> CenterTexture = new(() => new(Game1.objectSpriteSheet, new(336, 400, 16, 16)));
	private readonly Lazy<TextureRectangle> CheckedTexture = new(() => new(Game1.mouseCursors, OptionsCheckbox.sourceRectChecked));
	private readonly Lazy<TextureRectangle> UncheckedTexture = new(() => new(Game1.mouseCursors, OptionsCheckbox.sourceRectUnchecked));

	private IReadOnlySet<IntPoint> OriginalValues = new HashSet<IntPoint>();
	private HashSet<IntPoint> CurrentValues = new();
	private bool? LastMouseLeftPressed;
	private int Length = 3;

	public SelectableGridOption(
		Func<IReadOnlySet<IntPoint>> getValues,
		Action<IReadOnlySet<IntPoint>> setValues,
		Func<string> name,
		Func<string>? tooltip = null,
		Action? afterValuesUpdated = null
	)
	{
		this.GetValues = getValues;
		this.SetValues = setValues;
		this.Name = name;
		this.Tooltip = tooltip;
		this.AfterValuesUpdated = afterValuesUpdated;
	}

	private void Initialize()
	{
		OriginalValues = GetValues();
		CurrentValues = OriginalValues.ToHashSet();

		Length = 3;
		UpdateLength();
		Length = Math.Max(Length, 3);
	}

	internal void AddToGMCM(IGenericModConfigMenuApi api, IManifest mod)
	{
		api.AddComplexOption(
			mod: mod,
			name: Name,
			tooltip: Tooltip,
			draw: Draw,
			height: () => GetHeight(),
			beforeMenuOpened: () =>
			{
				LastMouseLeftPressed = null;
				Initialize();
			},
			beforeMenuClosed: Initialize,
			afterReset: Initialize,
			beforeSave: BeforeSave
		);
	}

	private void BeforeSave()
	{
		SetValues(CurrentValues.ToHashSet());
		OriginalValues = CurrentValues.ToHashSet();
		AfterValuesUpdated?.Invoke();
	}

	private void UpdateLength()
	{
		if (CurrentValues.Count == 0)
			return;
		int max = CurrentValues.Max(p => Math.Max(Math.Abs(p.X), Math.Abs(p.Y)));
		int newLength = (max + 1) * 2 + 1;
		Length = Math.Max(Length, newLength);
	}

	private Vector2 GetGMCMSize()
		=> new(Math.Min(1200, Game1.uiViewport.Width - 200), Game1.uiViewport.Height - 128 - 116);

	private Vector2 GetGMCMPosition(Vector2? size = null)
	{
		Vector2 gmcmSize = size ?? GetGMCMSize();
		return new((Game1.uiViewport.Width - gmcmSize.X) / 2, (Game1.uiViewport.Height - gmcmSize.Y) / 2);
	}

	private float GetCellLength(Vector2? gmcmSize = null)
	{
		gmcmSize ??= GetGMCMSize();
		float possibleLength = (gmcmSize.Value.X - Margin - (Length - 1) * CellSpacing) / Length;
		return possibleLength > MaxCellSize ? MaxCellSize : possibleLength;
	}

	private int GetHeight(float? cellLength = null, Vector2? gmcmSize = null)
	{
		cellLength ??= GetCellLength(gmcmSize: gmcmSize);
		return (int)Math.Ceiling(cellLength.Value * Length + CellSpacing * (Length - 1)) + RowHeight; // extra row, we're not rendering inline
	}

	private void Draw(SpriteBatch b, Vector2 basePosition)
	{
		bool mouseLeftPressed = Game1.input.GetMouseState().LeftButton == ButtonState.Pressed;
		bool didClick = mouseLeftPressed && LastMouseLeftPressed == false;
		LastMouseLeftPressed = mouseLeftPressed;
		int mouseX = Constants.TargetPlatform == GamePlatform.Android ? Game1.getMouseX() : Game1.getOldMouseX();
		int mouseY = Constants.TargetPlatform == GamePlatform.Android ? Game1.getMouseY() : Game1.getOldMouseY();

		Vector2 gmcmSize = GetGMCMSize();
		Vector2 gmcmPosition = GetGMCMPosition(gmcmSize);
		float cellLength = GetCellLength(gmcmSize);
		bool hoverGMCM = mouseX >= gmcmPosition.X && mouseY >= gmcmPosition.Y && mouseX < gmcmPosition.X + gmcmSize.X && mouseY < gmcmPosition.Y + gmcmSize.Y;

		Vector2 startPosition = new(gmcmPosition.X + Margin, basePosition.Y + RowHeight);
		float widthLeft = gmcmSize.X - Margin;
		float totalCellWidth = cellLength * Length + CellSpacing * (Length - 1);
		startPosition.X += widthLeft / 2f - totalCellWidth / 2f;

		for (int drawY = 0; drawY < Length; drawY++)
		{
			int valueY = drawY - Length / 2;
			for (int drawX = 0; drawX < Length; drawX++)
			{
				int valueX = drawX - Length / 2;
				TextureRectangle texture = (valueX == 0 && valueY == 0 ? CenterTexture : (CurrentValues.Contains(new(valueX, valueY)) ? CheckedTexture : UncheckedTexture)).Value;

				float iconScale = 1f;
				if (texture.Rectangle.Width * iconScale < cellLength)
					iconScale = 1f * cellLength / texture.Rectangle.Width;
				if (texture.Rectangle.Height * iconScale < cellLength)
					iconScale = 1f * cellLength / texture.Rectangle.Height;
				if (texture.Rectangle.Width * iconScale > cellLength)
					iconScale = 1f * cellLength / texture.Rectangle.Width;
				if (texture.Rectangle.Height * iconScale > cellLength)
					iconScale = 1f * cellLength / texture.Rectangle.Height;

				Vector2 texturePosition = new(startPosition.X + drawX * (cellLength + CellSpacing), startPosition.Y + drawY * (cellLength + CellSpacing));
				Vector2 textureCenterPosition = new(texturePosition.X + cellLength / 2f, texturePosition.Y + cellLength / 2f);
				b.Draw(texture.Texture, textureCenterPosition, texture.Rectangle, Color.White, 0f, new Vector2(texture.Rectangle.Width / 2f, texture.Rectangle.Height / 2f), iconScale, SpriteEffects.None, 4f);

				if (hoverGMCM && didClick && (valueX != 0 || valueY != 0))
				{
					bool hoverTexture = mouseX >= texturePosition.X && mouseY >= texturePosition.Y && mouseX < texturePosition.X + cellLength && mouseY < texturePosition.Y + cellLength;
					if (hoverTexture)
					{
						CurrentValues.Toggle(new(valueX, valueY));
						UpdateLength();
						Game1.playSound(ClickSoundName);
					}
				}
			}
		}
	}
}

public static class SelectableGridOptionExtensions
{
	public static void AddSelectableGridOption(
		this IGenericModConfigMenuApi api,
		IManifest mod,
		Func<IReadOnlySet<IntPoint>> getValues,
		Action<IReadOnlySet<IntPoint>> setValues,
		Func<string> name,
		Func<string>? tooltip = null,
		Action? afterValuesUpdated = null
	)
	{
		var option = new SelectableGridOption(getValues, setValues, name, tooltip, afterValuesUpdated);
		option.AddToGMCM(api, mod);
	}
}

public static class SelectableGridOptionForHelper
{
	public static void AddSelectableGridOption(
		this GMCMI18nHelper helper,
		string keyPrefix,
		Func<IReadOnlySet<IntPoint>> getValues,
		Action<IReadOnlySet<IntPoint>> setValues,
		Action? afterValuesUpdated = null,
		object? tokens = null
	)
	{
		helper.Api.AddSelectableGridOption(
			mod: helper.Mod,
			name: () => helper.Translations.Get($"{keyPrefix}.name", tokens),
			tooltip: helper.GetOptionalTranslatedStringDelegate($"{keyPrefix}.tooltip", tokens),
			getValues: getValues,
			setValues: setValues,
			afterValuesUpdated: afterValuesUpdated
		);
	}
}