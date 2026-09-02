@tool
extends CollisionShape3D

var _parentMeshBuilder: MeshBuilder;

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	_parentMeshBuilder = (get_parent().get_parent() as MeshBuilder)
	return

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(_delta: float) -> void:
	if _parentMeshBuilder.CollisionMeshDirty: 
		shape = _parentMeshBuilder.CollisionMesh
		_parentMeshBuilder.CollisionMeshDirty = false
	
	return

# func _validate_property(property: Dictionary) -> void:
# 	if(property.name == "shape"):
# 		property.usage = PROPERTY_USAGE_NONE
# 	
# 	return

func _notification(what: int) -> void:
	match what:
		NOTIFICATION_EDITOR_PRE_SAVE:
			shape = null
		NOTIFICATION_EDITOR_POST_SAVE:
			shape = _parentMeshBuilder.CollisionMesh

	return
