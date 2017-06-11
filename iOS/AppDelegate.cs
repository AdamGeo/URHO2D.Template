using Foundation;
using UIKit;
using Urho;
using System.Threading.Tasks;

namespace URHO2D.Template.iOS
{
	[Register("AppDelegate")]
	public class AppDelegate : UIApplicationDelegate
	{
		public override bool FinishedLaunching(UIApplication app, NSDictionary options)
		{
			LaunchGame();
			return true;
		}

		static async void LaunchGame()
		{
			await Task.Yield();
            Urho.Application.CreateInstance(typeof(GameClass), new ApplicationOptions("Data")).Run();
            UIApplication.SharedApplication.SetStatusBarHidden(true, UIStatusBarAnimation.Slide);
		}
	}
}
