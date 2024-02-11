using System;
using fennecs;
using Godot;

namespace examples.godot;



[GlobalClass]
public partial class MultiMeshExample : MultiMeshInstance3D
{
	[Export] public int SpawnCount = 1_000;
	public int InstanceCount => Multimesh.InstanceCount;
	
	private readonly Vector3 _amplitude = new(120f, 90f, 120f);
	private const float TimeScale = 0.05f;

	private readonly World _world = new();
	private double _time;

	public void SpawnWave(int spawnCount)
	{
		for (var i = 0; i < spawnCount; i++)
		{
			_world.Spawn()
				.Add(i+Multimesh.InstanceCount)
				.Add<Transform3D>()
				.Id();
		}
		Multimesh.InstanceCount += spawnCount;
	}

	public override void _Ready()
	{
		Multimesh = new MultiMesh();
		Multimesh.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
		Multimesh.Mesh = new BoxMesh();
		Multimesh.Mesh.SurfaceSetMaterial(0, ResourceLoader.Load<Material>("res://box_material.tres"));

		Multimesh.VisibleInstanceCount = -1;

		SpawnWave(SpawnCount * 5);
	}

	public override void _Process(double delta)
	{
		var query = _world.Query<int, Transform3D>().Build();
		_time += delta * TimeScale;

		var count = (float) InstanceCount;

		//Update positions
		query.RunParallel((ref int index, ref Transform3D transform) =>
		{
			var phase1 = index / 5000f * 2f;
			var group1 = 1 + (index / 1000)%5;
			
			var phase2 = index / 3000f * 2f;
			var group2 = 1 + (index / 1000)%3;
			
			var phase3 = index / 1000f * 2f;
			var group3 = 1 + (index / 1000)%10;
			
			var value1 = phase1 * Mathf.Pi * (group1 + Mathf.Sin(_time) * 1f);
			var value2 = phase2 * Mathf.Pi * (group2 + Mathf.Sin(_time * 1f) * 3f) ;
			var value3 = phase3 * Mathf.Pi * group3;

			var scale1 = 3f;
			var scale2 = 5f - group2;
			var scale3 = 4f;

			var vector = new Vector3
			{
				X = (float)Math.Sin(value1 + _time * scale1),
				Y = (float)Math.Sin(value2 + _time * scale2),
				Z = (float)Math.Sin(value3 + _time * scale3)
			};
			transform = new Transform3D(Basis.Identity, vector * _amplitude);
		}, chunkSize: 2000);

		// Write transforms into MultiMesh, must be single threaded
		query.RunParallel((ref int index, ref Transform3D transform) => { Multimesh.SetInstanceTransform(index, transform); });

	}

	private void _on_button_pressed()
	{
		SpawnWave(SpawnCount);
	}
}


