@tool
extends Sprite3D

@export_range(0.0, 1.0)
var ground_level: float = 0.5
#minimum value of the vector field, in this case -2.8
var minimum_value: float = -2.82843
var maximum_value: float = 2.82843

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	pass # Replace with function body.

func math_function(pos: Vector3) -> float:
	
	return cos(pos.x) - 2 * sin(pos.y) + cos(pos.z);

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:
	var sample: float = (math_function(global_position) + maximum_value) / (2 * maximum_value)
	
	if(sample < ground_level):
		visible = false
		return
	else:
		visible = true
	
	modulate.v = sample
	
	
	pass
