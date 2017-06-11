using System.Threading.Tasks;
using Urho;
using Urho.Gui;
using Urho.Actions;
using Urho.Shapes;
namespace URHO2D.Template
{
    public class StartMenu : Component
	{
		TaskCompletionSource<bool> menuTaskSource;
		//Node bigAircraft;
		//Node rotor;
		Text textBlock;
		//Node menuLight;
		bool finished = true;

		public StartMenu()
		{
			ReceiveSceneUpdates = true;
		}

		public async Task ShowStartMenu(bool gameOver)
		{
			var cache = Application.ResourceCache;
            var itcache = new GB2ShapeCache().sharedGB2ShapeCache();

			textBlock = new Text();
			textBlock.HorizontalAlignment = HorizontalAlignment.Center;
			textBlock.VerticalAlignment = VerticalAlignment.Bottom;
			textBlock.Value = gameOver ? "GAME OVER" : "TAP TO START";
			textBlock.SetFont(cache.GetFont(Assets.Fonts.Font), Application.Graphics.Width / 15);
			Application.UI.Root.AddChild(textBlock);

			menuTaskSource = new TaskCompletionSource<bool>();
			finished = false;
			await menuTaskSource.Task;
		}

        protected override void OnUpdate(float timeStep)
		{
			if (finished)
				return;
			if (Application.Input.NumTouches > 0 && Application.Input.NumTouches < 2)
			{
				finished = true;
				Application.UI.Root.RemoveChild(textBlock, 0);
				menuTaskSource.TrySetResult(true);
			}
		}
	}
}
