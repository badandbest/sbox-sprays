using Sandbox;
using System.Linq;

namespace badandbest.Sprays;

public static class Spray
{
	[ConVar( "spray", Help = "The image you want to spray.", Saved = true )]
	static string SprayUrl { get; set; }
	
	/// <summary>
	/// Places an image on a surface.
	/// </summary>
	public static void Place()
	{
		const float RANGE = 128; // Range in GMOD.

		var ray = Game.ActiveScene.Camera.Transform.World.ForwardRay;
		var trace = Game.SceneTrace.Ray( ray, RANGE );

		Place( trace );
	}

	/// <summary>
	/// Places an image on a surface.
	/// </summary>
	/// <param name="trace">The trace to use.</param>
	public static void Place( SceneTrace trace )
	{
		var tr = trace.Run();
		if ( !tr.Hit )
		{
			return;
		}

		var sprayRenderer = Game.ActiveScene.GetAllComponents<SprayRenderer>().SingleOrDefault( x => x.Network.IsOwner );

		if ( !sprayRenderer.IsValid() )
		{
			var sprayObj = SceneUtility.GetPrefabScene( ResourceLibrary.Get<PrefabFile>( "spray.prefab" ) ).Clone();
			sprayObj.NetworkSpawn();
			sprayRenderer = sprayObj.Components.Get<SprayRenderer>();
		}

		Sound.Play( "SprayCan.Paint", tr.HitPosition );

		sprayRenderer.Transform.World = new Transform( tr.HitPosition, Rotation.LookAt( tr.Normal ) );
		sprayRenderer.Image = SprayUrl;
	}
}

[Title( "Spray Renderer" ), Icon( "imagesearch_roller" )]
internal class SprayRenderer : Renderer
{
	[Property, ImageAssetPath, Sync, Facepunch.Change( nameof( OnImageChanged ) )]
	public string Image { get; set; }

	public async void OnImageChanged( string oldValue, string newValue )
	{
		var decal = Components.GetInChildren<DecalRenderer>().Material;

		// Clear texture while next image is downloading.
		decal.Set( "g_tColor", Texture.Transparent );
		decal.Set( "g_tColor", await Texture.LoadAsync( null, newValue ) );
	}
}
