@tool
extends Sprite3D

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	pass # Replace with function body.

func math_function(pos: Vector3) -> float:
	
	return cos(pos.x) + sin(pos.y) + cos(pos.z);

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:
	modulate.v = math_function(position)
	pass
