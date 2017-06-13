using Foundation;
using UIKit;
using Urho;
using System.Threading.Tasks;
using System.Diagnostics;

namespace URHO2D.Template.iOS
{
	[Register("AppDelegate")]
	public class AppDelegate : UIApplicationDelegate
	{
		public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
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

		public override void DidEnterBackground(UIApplication application)
		{
			//Debug.WriteLine("Entered background. " + application.BackgroundTimeRemaining.ToString());
		}
	}
}
