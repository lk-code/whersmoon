namespace WheresMoon.App.Rendering;

/// <summary>Moves the drawn scene gently from its current state to a new one.</summary>
internal sealed class SceneAnimator(GraphicsView view, SceneDrawable drawable)
{
	private const string AnimationName = "SceneTransition";
	private const uint DurationMilliseconds = 800;

	private SceneState _target = drawable.State;
	private bool _hasState;

	/// <summary>The state being moved to. Stays valid while the animation runs.</summary>
	public SceneState Target => _target;

	public void MoveTo(SceneState state)
	{
		_target = state;

		// The first frame after launch must show the real sky at once, not fade into it.
		if (!_hasState)
		{
			_hasState = true;
			drawable.State = state;
			view.Invalidate();
			return;
		}

		SceneState from = drawable.State;

		view.AbortAnimation(AnimationName);
		view.Animate(
			AnimationName,
			t =>
			{
				drawable.State = SceneState.Lerp(from, state, t);
				view.Invalidate();
			},
			length: DurationMilliseconds,
			easing: Easing.SinInOut);
	}
}
