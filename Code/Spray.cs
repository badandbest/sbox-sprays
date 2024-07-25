using Sandbox;
using Sandbox.Utility;
using System;

namespace badandbest.Sprays;

/// <summary>
/// A library to allow the placement of sprays in the world
/// </summary>
public static class Spray
{
	private static GameObject LocalSpray;

	[ConVar( "spray", Help = "URL of image. Must be in quotes.", Saved = true )]
	internal static string Image { get; set; }

	[ConVar( "spraydisable", Help = "Disables player sprays. Good for streamers.", Saved = true )]
	internal static bool Disabled { get; set; }

	/// <summary>
	/// Places an image on a surface.
	/// </summary>
	public static void Place()
	{
		const float RANGE = 128;// Range in GMOD.

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
		// We only want to hit static bodies. ( maps, etc )
		if ( trace.Run() is not { Body.BodyType: PhysicsBodyType.Static } tr )
			return;

		if ( string.IsNullOrEmpty( Image ) )
			Image = "materials/decals/default.png";

		var config = new CloneConfig
		{
			Name = $"Failed to load Spray - {Steam.PersonaName}",
			Transform = new( tr.HitPosition, Rotation.LookAt( tr.Normal ) ),
			PrefabVariables = new() { { "Image", Image } }
		};

		LocalSpray?.Destroy();
		LocalSpray = GameObject.Clone( "prefabs/spray.prefab", config );

		LocalSpray.NetworkSpawn();// NetworkSpawn breaks the prefab
		LocalSpray.SetPrefabSource( "prefabs/spray.prefab" );
	}
}

[Title( "Spray Renderer" ), Icon( "imagesearch_roller" )]
internal class SprayRenderer : Renderer
{
	[Property, ImageAssetPath]
	public string Image { get; set; }

	protected override async void OnEnabled()
	{
		var decal = Components.Get<DecalRenderer>( FindMode.InChildren );

		if ( Spray.Disabled )
		{
			decal.Enabled = false;
			return;
		}

		Texture texture = null;

		try
		{
			texture = await Texture.LoadAsync( FileSystem.Mounted, Image, false );
			if ( texture is null or { Width: <= 32, Height: <= 32 } )
			{
				// Probably an error texture. Replace with a fallback image.
				texture = await Texture.LoadAsync( FileSystem.Mounted, "materials/fallback.vtex" );
			}
		}
		catch ( Exception e )
		{
			Log.Error( $"Couldn't Load Avatar {Image} {e}" );
			throw;
		}
		finally
		{
			var material = Material.Load( "materials/spray.vmat" ).CreateCopy();
			material.Set( "g_tColor", texture );

			decal.Material = material;
		}
	}
}
