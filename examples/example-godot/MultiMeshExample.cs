using System;
using fennecs;
using Godot;

namespace examples.godot;

public partial class MultiMeshExample : MultiMeshInstance3D
{
	[Export]
	public int EntityCount = 40_000;
	private readonly Vector3 _amplitude = new(200f, 70f, 200f);
	private const float TimeScale = 0.05f;
	
	private readonly World _world = new();
	private double _time;
	 
	public override void _Ready()
	{
		Multimesh = new MultiMesh();
		Multimesh.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
		Multimesh.Mesh = new BoxMesh();
		Multimesh.Mesh.SurfaceSetMaterial(0, ResourceLoader.Load<Material>("res://box_material.tres"));
		Multimesh.InstanceCount = EntityCount;
		
		for (var i = 0; i < EntityCount; i++)
		{
			_world.Spawn()
				.Add(i)
				.Add<Transform3D>()
				.Id();
		}
	}
	
	public override void _Process(double delta)
	{
		var query = _world.Query<int, Transform3D>().Build();
		_time += delta * TimeScale;
		
		//Update positions
		query.RunParallel((ref int index, ref Transform3D transform) =>
		{
			var phase1 = index * 3.14 / 100f;
			var phase2 = index * 3.14f / 130f;
			var phase3 = index * 3.14f / 370f;

			var scale1 = 3f;
			var scale2 = 5f;
			var scale3 = 4f;
			
			var vector = new Vector3{
				X = (float)Math.Sin(phase1 + _time * scale1), 
				Y = (float)Math.Sin(phase2 + _time * scale2), 
				Z = (float)Math.Sin(phase3 + _time * scale3)
			};
			transform = new Transform3D(Basis.Identity, vector * _amplitude);
		}, chunkSize:2000);

		// Write transforms into MultiMesh, must be single threaded
		query.Run((ref int index, ref Transform3D transform) =>
		{
			Multimesh.SetInstanceTransform(index, transform);
		});

	}
}
