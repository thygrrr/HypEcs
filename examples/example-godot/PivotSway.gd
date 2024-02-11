extends Node3D

@onready var tween : Tween

func coroutine():
	while true:
		tween = create_tween()
		tween.set_ease(Tween.EASE_IN_OUT)
		tween.set_trans(Tween.TRANS_QUART)
		tween.parallel().tween_property(self, "global_rotation_degrees", 
		Vector3(
			randf_range(-180, 180),
			randf_range(-180, 180),
			randf_range(-180, 180))
		, 7)
		tween.set_ease(Tween.EASE_IN_OUT)
		tween.set_trans(Tween.TRANS_BACK)
		tween.parallel().tween_property(self, "global_scale", 
		Vector3(
			randf_range(0.2, 1.2),
			randf_range(0.2, 1.2),
			randf_range(0.2, 1.2))
		, 7)
		await tween.finished
		
		
# Called when the node enters the scene tree for the first time.
func _ready():
	coroutine()
	


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	pass
