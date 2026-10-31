using WheresMoon.Core.Scene;

namespace WheresMoon.App.Controls;

internal enum InfoRowStyle
{
	Primary,
	List,
	Secondary,
	Caption,
}

internal readonly record struct InfoRow(string Text, InfoRowStyle Style);

/// <summary>
/// The pages below the clock. They slide across the whole width, each row placed by
/// <see cref="SceneLayout"/>, and the scene draws the dots from <see cref="Position"/>.
/// </summary>
internal sealed class InfoSlider : Grid
{
	public const int PageCount = 3;

	private const string SettleAnimation = "InfoSliderSettle";
	private const uint SettleMilliseconds = 260;

	/// <summary>Speed in device independent units per second that turns the page, however short the swipe.</summary>
	private const double FlickSpeed = 350.0;

	/// <summary>Resistance when pulling past the first or last page.</summary>
	private const double EdgeResistance = 0.3;

	private const double LabelHeightFactor = 1.4;

	private readonly Grid[] _pages = new Grid[PageCount];
	private readonly Label[,] _rows = new Label[PageCount, DesignTokens.Size.TextRows];
	private readonly InfoRowStyle[,] _styles = new InfoRowStyle[PageCount, DesignTokens.Size.TextRows];
	private SceneLayout? _layout;

	public InfoSlider()
	{
		InputTransparent = true;
		SafeAreaEdges = SafeAreaEdges.None;

		for (int page = 0; page < PageCount; page++)
		{
			// Positions come from SceneLayout, which already accounts for the safe area.
			var grid = new Grid { InputTransparent = true, IsVisible = page == 0, SafeAreaEdges = SafeAreaEdges.None };

			for (int row = 0; row < DesignTokens.Size.TextRows; row++)
			{
				var label = new Label
				{
					TextColor = Colors.White,
					HorizontalTextAlignment = TextAlignment.Center,
					VerticalTextAlignment = TextAlignment.Center,
					VerticalOptions = LayoutOptions.Start,
					LineBreakMode = LineBreakMode.NoWrap,
					FontAutoScalingEnabled = false,
				};

				_rows[page, row] = label;
				grid.Add(label);
			}

			_pages[page] = grid;
			Add(grid);
		}
	}

	/// <summary>The page at rest, or the one the slider is settling on.</summary>
	public int Page { get; private set; }

	/// <summary>Position between pages while sliding: 1.5 is halfway between the second and third.</summary>
	public double Position { get; private set; }

	/// <summary>Raised when <see cref="Page"/> changes.</summary>
	public event Action? PageChanged;

	/// <summary>Raised when <see cref="Position"/> changes, so that the dots can follow.</summary>
	public event Action? PositionChanged;

	public bool IsShown(int page) => _pages[page].IsVisible;

	public void SetRows(int page, IReadOnlyList<InfoRow> rows)
	{
		for (int row = 0; row < DesignTokens.Size.TextRows; row++)
		{
			Label label = _rows[page, row];

			if (row < rows.Count)
			{
				label.Text = rows[row].Text;
				_styles[page, row] = rows[row].Style;
				ApplyStyle(label, rows[row].Style);
			}
			else
			{
				label.Text = string.Empty;
			}
		}
	}

	/// <summary>Excludes every label one by one, because excluding a whole layout has no effect on Windows.</summary>
	public void SetIsInAccessibleTree(bool isAccessible)
	{
		foreach (Label label in _rows)
		{
			AutomationProperties.SetIsInAccessibleTree(label, isAccessible);
		}
	}

	public void ApplyLayout(SceneLayout layout)
	{
		_layout = layout;

		for (int page = 0; page < PageCount; page++)
		{
			for (int row = 0; row < DesignTokens.Size.TextRows; row++)
			{
				Label label = _rows[page, row];
				double height = layout.RiseSetFontSize * LabelHeightFactor;

				label.HeightRequest = height;
				label.Margin = new Thickness(0, layout.RowCenterY(row) - height / 2.0, 0, 0);
				ApplyStyle(label, _styles[page, row]);
			}
		}

		PlacePages();
	}

	/// <summary>Starts following a finger, with all pages ready to slide in.</summary>
	public void BeginDrag()
	{
		this.AbortAnimation(SettleAnimation);

		for (int page = 0; page < PageCount; page++)
		{
			_pages[page].IsVisible = true;
		}
	}

	public void DragBy(double distance)
	{
		double width = Math.Max(1.0, _layout?.Width ?? 1.0);
		double next = Position - distance / width;
		bool isPastEdge = next < 0.0 || next > PageCount - 1;

		Position = Math.Clamp(Position - distance / width * (isPastEdge ? EdgeResistance : 1.0), -0.25, PageCount - 0.75);
		PlacePages();
		PositionChanged?.Invoke();
	}

	public void EndDrag(double velocity)
	{
		int target = Math.Abs(velocity) >= FlickSpeed
			? Page + (velocity < 0.0 ? 1 : -1)
			: (int)Math.Round(Position);

		SettleOn(Math.Clamp(target, 0, PageCount - 1));
	}

	public void ShowNext() => SettleOn((Page + 1) % PageCount);

	private void SettleOn(int target)
	{
		double from = Position;

		this.AbortAnimation(SettleAnimation);

		// The neighbours must be there before the page slides in, also when no finger brought them up.
		foreach (Grid page in _pages)
		{
			page.IsVisible = true;
		}

		if (target != Page)
		{
			Page = target;
			PageChanged?.Invoke();
		}

		this.Animate(
			SettleAnimation,
			t =>
			{
				Position = from + (target - from) * t;
				PlacePages();
				PositionChanged?.Invoke();
			},
			length: SettleMilliseconds,
			easing: Easing.CubicOut,
			finished: (_, cancelled) =>
			{
				if (!cancelled)
				{
					HideOthers();
				}
			});
	}

	/// <summary>Screen readers must not find the pages out of view.</summary>
	private void HideOthers()
	{
		for (int page = 0; page < PageCount; page++)
		{
			_pages[page].IsVisible = page == Page;
		}
	}

	private void PlacePages()
	{
		double width = _layout?.Width ?? 0.0;

		for (int page = 0; page < PageCount; page++)
		{
			_pages[page].TranslationX = (page - Position) * width;
		}
	}

	private void ApplyStyle(Label label, InfoRowStyle style)
	{
		label.FontSize = style switch
		{
			InfoRowStyle.Primary => _layout?.RiseSetFontSize ?? 0.0,
			InfoRowStyle.Caption => _layout?.CaptionFontSize ?? 0.0,
			_ => _layout?.PhaseFontSize ?? 0.0,
		};
		label.FontFamily = style is InfoRowStyle.Primary or InfoRowStyle.List ? "ManropeMedium" : "ManropeRegular";
		label.Opacity = style switch
		{
			InfoRowStyle.Secondary => DesignTokens.Opacity.PhaseText,
			InfoRowStyle.Caption => DesignTokens.Opacity.CaptionText,
			_ => 1.0,
		};
	}
}
