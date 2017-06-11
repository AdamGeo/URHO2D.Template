using System;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using Org.Libsdl.App;
using Urho.Droid;

namespace URHO2D.Template.Droid
{
    [Activity(Label = "SamplyGame", MainLauncher = true,
        Icon = "@drawable/icon", Theme = "@android:style/Theme.NoTitleBar.Fullscreen",
              ConfigurationChanges = ConfigChanges.KeyboardHidden | ConfigChanges.Orientation, ScreenOrientation = ScreenOrientation.Landscape)]
    public class MainActivity : Activity
    {
        //SDLSurface surface;
        //UrhoSurface surface;
        Urho.Application app;

        protected override async void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            var mLayout = new LinearLayout(this);
            var surface = UrhoSurface.CreateSurface(this); //.SDLSurface;// (this, , true);
            mLayout.AddView(surface);
            SetContentView(mLayout);
            app = await surface.Show<GameClass>(new Urho.ApplicationOptions("Data"));
        }

        protected override void OnResume()
        {
            UrhoSurface.OnResume();
            base.OnResume();
        }

        protected override void OnPause()
        {
            UrhoSurface.OnPause();
            base.OnPause();
        }

        public override void OnLowMemory()
        {
            UrhoSurface.OnLowMemory();
            base.OnLowMemory();
        }

        protected override void OnDestroy()
        {
            UrhoSurface.OnDestroy();
            base.OnDestroy();
        }

        public override bool DispatchKeyEvent(KeyEvent e)
        {
            if (e.KeyCode == Android.Views.Keycode.Back)
            {
                this.Finish();
                return false;
            }

            if (!UrhoSurface.DispatchKeyEvent(e))
                return false;
            return base.DispatchKeyEvent(e);
        }

        public override void OnWindowFocusChanged(bool hasFocus)
        {
            UrhoSurface.OnWindowFocusChanged(hasFocus);
            base.OnWindowFocusChanged(hasFocus);
        }
    }
}




//using System;
//using System.Threading;
//using System.Threading.Tasks;
//using Android.App;
//using Android.OS;
//using Android.Content.PM;
//using Android.Views;
//using Android.Widget;
//using Org.Libsdl.App;
//using Urho;
//using Urho.Droid;

//namespace URHO2D.Template.Droid
//{
//    //[Activity(Label = "URHO2D.Template", MainLauncher = true, Icon = "@mipmap/icon")]
//	//[Activity(Label = "URHO2D.Template", MainLauncher = true,
//		//Icon = "@drawable/icon", Theme = "@android:style/Theme.NoTitleBar.Fullscreen")]


//	[Activity(Label = "URHO2D.Template", MainLauncher = true,
//		Icon = "@drawable/icon", Theme = "@android:style/Theme.NoTitleBar.Fullscreen",
//		ConfigurationChanges = ConfigChanges.KeyboardHidden | ConfigChanges.Orientation,
//		ScreenOrientation = ScreenOrientation.Portrait)]    
//    public class MainActivity : Activity
//    {
//		SDLSurface surface;
//		Urho.Application app;

//		protected override async void OnCreate(Bundle bundle)
//		{
//			base.OnCreate(bundle);

//			// Set our view from the "main" layout resource
//			//SetContentView(Resource.Layout.Main);
//			//placeholder = FindViewById<LinearLayout>(Resource.Id.UrhoSurfacePlaceHolder);

//			base.OnCreate(bundle);
//			var mLayout = new AbsoluteLayout(this);
//			surface = UrhoSurface.CreateSurface(this);// (this, , true);
//			mLayout.AddView(surface);
//			SetContentView(mLayout);
//			app = await surface.Show(Type.GetType(Intent.GetStringExtra("Type")), new ApplicationOptions("Data"));

//		}

//		protected override void OnResume()
//		{
//			UrhoSurface.OnResume();
//			base.OnResume();
//		}

//		protected override void OnPause()
//		{
//			UrhoSurface.OnPause();
//			base.OnPause();
//		}

//		public override void OnLowMemory()
//		{
//			UrhoSurface.OnLowMemory();
//			base.OnLowMemory();
//		}

//		protected override void OnDestroy()
//		{
//			UrhoSurface.OnDestroy();
//			base.OnDestroy();
//		}

//		public override bool DispatchKeyEvent(KeyEvent e)
//		{
//			if (e.KeyCode == Android.Views.Keycode.Back)
//			{
//				this.Finish();
//				return false;
//			}

//			if (!UrhoSurface.DispatchKeyEvent(e))
//				return false;
//			return base.DispatchKeyEvent(e);
//		}

//		public override void OnWindowFocusChanged(bool hasFocus)
//		{
//			UrhoSurface.OnWindowFocusChanged(hasFocus);
//			base.OnWindowFocusChanged(hasFocus);
//		}
//    }
//}

