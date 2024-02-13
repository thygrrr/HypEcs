using System;
using System.Runtime.InteropServices;
using fennecs;
using Godot;
using Vector3 = System.Numerics.Vector3;

namespace examples.godot.BasicCubes;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct Matrix4X3
{
	public float M00;
	public float M01;
	public float M02;
	public float M03;

	public float M10;
	public float M11;
	public float M12;
	public float M13;

	public float M20;
	public float M21;
	public float M22;
	public float M23;

	public Matrix4X3(float m00, float m01, float m02, float m03, float m10, float m11, float m12, float m13, float m20, float m21, float m22, float m23)
	{
		M00 = m00;
		M01 = m01;
		M02 = m02;
		M03 = m03;
		M10 = m10;
		M11 = m11;
		M12 = m12;
		M13 = m13;
		M20 = m20;
		M21 = m21;
		M22 = m22;
		M23 = m23;
	}

	public Matrix4X3()
	{
		M00 = 1;
		M01 = 0;
		M02 = 0;
		M03 = 0;
		M10 = 0;
		M11 = 1;
		M12 = 0;
		M13 = 0;
		M20 = 0;
		M21 = 0;
		M22 = 1;
		M23 = 0;
	}

	public Matrix4X3(Vector3 origin)
	{
		M00 = 1;
		M01 = 0;
		M02 = 0;
		M03 = origin.X;
		M10 = 0;
		M11 = 1;
		M12 = 0;
		M13 = origin.Y;
		M20 = 0;
		M21 = 0;
		M22 = 1;
		M23 = origin.Z;
	}

	public override string ToString()
	{
		return $"Matrix4X3({M00}, {M01}, {M02}, {M03}, {M10}, {M11}, {M12}, {M13}, {M20}, {M21}, {M22}, {M23})";
	}
}


[GlobalClass]
public partial class MultiMeshExample : Node
{
	[Export] public int SpawnCount = 10_000;
	[Export] public MultiMeshInstance3D MeshInstance;
	public int InstanceCount => MeshInstance.Multimesh.InstanceCount;

	private readonly Vector3 _amplitude = new(120f, 90f, 120f);
	private const float TimeScale = 0.003f;

	private readonly World _world = new();
	private double _time;

	public void SpawnWave(int spawnCount)
	{
		for (var i = 0; i < spawnCount; i++)
		{
			_world.Spawn()
				.Add(i+ MeshInstance.Multimesh.InstanceCount)
				.Add<Matrix4X3>()
				.Id();
		}

		MeshInstance.Multimesh.InstanceCount += spawnCount;
	}

	public override void _Ready()
	{
		MeshInstance.Multimesh = new MultiMesh();
		MeshInstance.Multimesh.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
		MeshInstance.Multimesh.Mesh = new BoxMesh();
		MeshInstance.Multimesh.Mesh.SurfaceSetMaterial(0, ResourceLoader.Load<Material>("res://BasicCubes/box_material.tres"));

		MeshInstance.Multimesh.VisibleInstanceCount = -1;

		SpawnWave(SpawnCount * 5);
	}

	public override void _Process(double delta)
	{
		var query = _world.Query<int, Matrix4X3>().Build();
		_time += delta * TimeScale;

		var count = (float) InstanceCount;

		//Update positions
		query.RunParallel((ref int index, ref Matrix4X3 transform) =>
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
				Z = (float)Math.Sin(value3 + _time * scale3),
			};
			
			transform = new Matrix4X3(vector * _amplitude);
		}, chunkSize: 4096);

		// Write transforms into Multimesh
		query.Run((_, transforms) =>
		{
			unsafe
			{
				var floatSpan = MemoryMarshal.Cast<Matrix4X3, float>(transforms)[..(transforms.Length * sizeof(Matrix4X3) / sizeof(float))];
				RenderingServer.MultimeshSetBuffer(MeshInstance.Multimesh.GetRid(), floatSpan.ToArray());
			}
		});
	}

	private void _on_button_pressed()
	{
		SpawnWave(SpawnCount);
	}
}


